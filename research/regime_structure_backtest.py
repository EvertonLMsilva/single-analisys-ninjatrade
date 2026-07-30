#!/usr/bin/env python3
"""Pesquisa reproduzível de regime, estrutura e gatilho em barras de 1 minuto.

Não acessa o NinjaTrader, não envia ordens e não escolhe parâmetros pelo resultado.
O regime é classificado após os primeiros 30 minutos do pregão regular usando apenas
informações disponíveis naquele instante. MNQ e MES podem confirmar um ao outro.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import math
import statistics
from collections import defaultdict
from dataclasses import asdict, dataclass
from datetime import date, datetime, time, timedelta
from pathlib import Path
from typing import Iterable, Optional

import prop_evaluation


TIME_FORMAT = "%Y-%m-%d %H:%M:%S"
EXPECTED_HEADER = ["Time", "Open", "High", "Low", "Close", "Volume"]
ROUND_TURN_COST = 5.0
MAXIMUM_RISK_CURRENCY = 75.0
MINIMUM_RISK_CURRENCY = 10.0


@dataclass(frozen=True)
class InstrumentSpec:
    symbol: str
    tick_size: float
    point_value: float


@dataclass
class Bar:
    timestamp: datetime
    open: float
    high: float
    low: float
    close: float
    volume: float
    trading_day: date


@dataclass
class DayContext:
    instrument: str
    trading_day: date
    open_time: datetime
    decision_time: datetime
    session_end: datetime
    opening_price: float
    decision_close: float
    opening_range_high: float
    opening_range_low: float
    opening_range: float
    first_30_return: float
    directional_efficiency: float
    vwap: float
    vwap_slope: float
    acceptance: float
    relative_opening_volume: float
    normalized_opening_range: float
    normalized_opening_move: float
    direction: int
    regime: str
    cross_market_aligned: bool


@dataclass
class Trade:
    instrument: str
    trading_day: date
    playbook: str
    direction: str
    regime: str
    signal_time: datetime
    exit_time: datetime
    entry: float
    stop: float
    target: float
    risk_currency: float
    status: str
    gross_currency: float
    net_currency: float


SPECS = {
    "MNQ": InstrumentSpec("MNQ", 0.25, 2.0),
    "MES": InstrumentSpec("MES", 0.25, 1.25),
}


def is_us_daylight_saving(day: date) -> bool:
    march_first = date(day.year, 3, 1)
    first_sunday_march = march_first + timedelta(days=(6 - march_first.weekday()) % 7)
    second_sunday_march = first_sunday_march + timedelta(days=7)
    november_first = date(day.year, 11, 1)
    first_sunday_november = november_first + timedelta(
        days=(6 - november_first.weekday()) % 7
    )
    return second_sunday_march <= day < first_sunday_november


def trading_day_for(timestamp: datetime) -> date:
    rollover = time(19, 0) if is_us_daylight_saving(timestamp.date()) else time(20, 0)
    return timestamp.date() + timedelta(days=1) if timestamp.time() >= rollover else timestamp.date()


def regular_open_for(day: date) -> datetime:
    # Os CSVs do exportador estão no horário local de Brasília.
    hour = 10 if is_us_daylight_saving(day) else 11
    return datetime.combine(day, time(hour, 30))


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def infer_symbol(path: Path) -> str:
    name = path.name.upper()
    for symbol in SPECS:
        if f"_{symbol}_" in name:
            return symbol
    raise ValueError(f"{path}: não foi possível inferir MNQ ou MES pelo nome")


def load_bars(path: Path) -> tuple[str, list[Bar], dict[str, object]]:
    symbol = infer_symbol(path)
    bars: list[Bar] = []
    interval_counts: dict[int, int] = defaultdict(int)
    missing_minutes = 0
    previous: Optional[datetime] = None
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        if reader.fieldnames != EXPECTED_HEADER:
            raise ValueError(f"{path}: cabeçalho inválido: {reader.fieldnames}")
        for line_number, row in enumerate(reader, start=2):
            timestamp = datetime.strptime(row["Time"], TIME_FORMAT)
            open_price, high, low, close, volume = (
                float(row[name]) for name in EXPECTED_HEADER[1:]
            )
            if previous is not None:
                if timestamp <= previous:
                    raise ValueError(f"{path}: timestamp fora de ordem na linha {line_number}")
                delta = int((timestamp - previous).total_seconds() / 60)
                interval_counts[delta] += 1
                if 1 < delta < 60:
                    missing_minutes += delta - 1
            if high < max(open_price, low, close) or low > min(open_price, high, close):
                raise ValueError(f"{path}: OHLC inválido na linha {line_number}")
            if volume <= 0:
                raise ValueError(f"{path}: volume inválido na linha {line_number}")
            bars.append(
                Bar(
                    timestamp,
                    open_price,
                    high,
                    low,
                    close,
                    volume,
                    trading_day_for(timestamp),
                )
            )
            previous = timestamp
    if not bars:
        raise ValueError(f"{path}: arquivo vazio")
    one_minute_share = interval_counts.get(1, 0) / max(1, sum(interval_counts.values()))
    if one_minute_share < 0.95:
        raise ValueError(
            f"{path}: apenas {one_minute_share:.1%} dos intervalos são de 1 minuto"
        )
    audit = {
        "path": path.name,
        "sha256": file_sha256(path),
        "rows": len(bars),
        "first_timestamp": bars[0].timestamp.isoformat(sep=" "),
        "last_timestamp": bars[-1].timestamp.isoformat(sep=" "),
        "one_minute_interval_share": round(one_minute_share, 6),
        "missing_intrahour_minutes": missing_minutes,
    }
    return symbol, bars, audit


def group_by_day(bars: Iterable[Bar]) -> dict[date, list[Bar]]:
    grouped: dict[date, list[Bar]] = defaultdict(list)
    for bar in bars:
        grouped[bar.trading_day].append(bar)
    return dict(grouped)


def percentile_rank(history: list[float], value: float) -> float:
    if not history:
        return 0.5
    return sum(item <= value for item in history) / len(history)


def calculate_contexts(
    symbol: str, days: dict[date, list[Bar]]
) -> dict[date, DayContext]:
    contexts: dict[date, DayContext] = {}
    prior_ranges: list[float] = []
    prior_opening_volumes: list[float] = []

    for trading_day in sorted(days):
        open_time = regular_open_for(trading_day)
        decision_time = open_time + timedelta(minutes=30)
        session_end = open_time + timedelta(hours=6, minutes=30)
        rth = [
            bar for bar in days[trading_day]
            if open_time < bar.timestamp <= session_end
        ]
        opening = [bar for bar in rth if bar.timestamp <= decision_time]
        if len(opening) < 25:
            continue

        opening_range_high = max(bar.high for bar in opening)
        opening_range_low = min(bar.low for bar in opening)
        opening_range = opening_range_high - opening_range_low
        if opening_range <= 0:
            continue
        path_length = sum(
            abs(opening[index].close - opening[index - 1].close)
            for index in range(1, len(opening))
        )
        first_return = opening[-1].close - opening[0].open
        efficiency = abs(first_return) / path_length if path_length > 0 else 0.0
        cumulative_volume = sum(bar.volume for bar in opening)
        vwap = sum(
            ((bar.high + bar.low + bar.close) / 3.0) * bar.volume for bar in opening
        ) / cumulative_volume
        early = opening[: max(5, len(opening) - 10)]
        early_volume = sum(bar.volume for bar in early)
        early_vwap = sum(
            ((bar.high + bar.low + bar.close) / 3.0) * bar.volume for bar in early
        ) / early_volume
        direction = 1 if first_return > 0 else -1 if first_return < 0 else 0
        final_five = opening[-5:]
        acceptance = sum(
            (bar.close > vwap if direction > 0 else bar.close < vwap)
            for bar in final_five
        ) / len(final_five) if direction else 0.0
        median_range = statistics.median(prior_ranges[-20:]) if prior_ranges else opening_range
        median_volume = (
            statistics.median(prior_opening_volumes[-20:])
            if prior_opening_volumes else cumulative_volume
        )
        normalized_range = opening_range / median_range if median_range else 1.0
        normalized_move = abs(first_return) / median_range if median_range else 0.0
        relative_volume = cumulative_volume / median_volume if median_volume else 1.0
        vwap_slope = (vwap - early_vwap) / opening_range

        trend = (
            direction != 0
            and normalized_move >= 0.30
            and efficiency >= 0.28
            and acceptance >= 0.80
            and vwap_slope * direction > 0.025
            and normalized_range >= 0.75
        )
        balance = (
            normalized_move <= 0.22
            and efficiency <= 0.30
            and normalized_range <= 1.20
        )
        regime = (
            "TrendUp" if trend and direction > 0
            else "TrendDown" if trend
            else "Balance" if balance
            else "Transition"
        )
        contexts[trading_day] = DayContext(
            instrument=symbol,
            trading_day=trading_day,
            open_time=open_time,
            decision_time=decision_time,
            session_end=session_end,
            opening_price=opening[0].open,
            decision_close=opening[-1].close,
            opening_range_high=opening_range_high,
            opening_range_low=opening_range_low,
            opening_range=opening_range,
            first_30_return=first_return,
            directional_efficiency=efficiency,
            vwap=vwap,
            vwap_slope=vwap_slope,
            acceptance=acceptance,
            relative_opening_volume=relative_volume,
            normalized_opening_range=normalized_range,
            normalized_opening_move=normalized_move,
            direction=direction,
            regime=regime,
            cross_market_aligned=False,
        )
        prior_ranges.append(opening_range)
        prior_opening_volumes.append(cumulative_volume)
    return contexts


def apply_cross_market_confirmation(
    left: dict[date, DayContext], right: dict[date, DayContext]
) -> None:
    for trading_day in set(left) & set(right):
        left_context = left[trading_day]
        right_context = right[trading_day]
        aligned = (
            left_context.direction != 0
            and left_context.direction == right_context.direction
            and right_context.acceptance >= 0.60
        )
        left_context.cross_market_aligned = aligned
        right_context.cross_market_aligned = aligned
        if left_context.regime.startswith("Trend") and not aligned:
            left_context.regime = "Transition"
        if right_context.regime.startswith("Trend") and not aligned:
            right_context.regime = "Transition"


def rth_bars_after_decision(day_bars: list[Bar], context: DayContext) -> list[Bar]:
    return [
        bar for bar in day_bars
        if context.decision_time < bar.timestamp <= context.session_end
    ]


def rolling_vwaps(day_bars: list[Bar], context: DayContext) -> dict[datetime, float]:
    result: dict[datetime, float] = {}
    price_volume = 0.0
    volume = 0.0
    for bar in day_bars:
        if not (context.open_time < bar.timestamp <= context.session_end):
            continue
        typical = (bar.high + bar.low + bar.close) / 3.0
        price_volume += typical * bar.volume
        volume += bar.volume
        result[bar.timestamp] = price_volume / volume
    return result


def find_signal(
    symbol: str, day_bars: list[Bar], context: DayContext
) -> Optional[tuple[str, str, int, float, float, float]]:
    spec = SPECS[symbol]
    bars = rth_bars_after_decision(day_bars, context)
    vwaps = rolling_vwaps(day_bars, context)
    tolerance = context.opening_range * 0.12

    if context.regime in ("TrendUp", "TrendDown"):
        is_long = context.regime == "TrendUp"
        broken = False
        for index, bar in enumerate(bars):
            if bar.timestamp > context.decision_time + timedelta(hours=2, minutes=30):
                break
            vwap = vwaps[bar.timestamp]
            if is_long:
                broken = broken or bar.close > context.opening_range_high + spec.tick_size
                trigger = (
                    broken
                    and bar.low <= context.opening_range_high + tolerance
                    and bar.close > context.opening_range_high
                    and bar.close > vwap
                    and bar.close > bar.open
                )
                if trigger:
                    entry = bar.close
                    stop = min(bar.low, context.opening_range_high - tolerance)
                    return "TrendRetest", "Long", index, entry, stop, entry + 1.5 * (entry - stop)
            else:
                broken = broken or bar.close < context.opening_range_low - spec.tick_size
                trigger = (
                    broken
                    and bar.high >= context.opening_range_low - tolerance
                    and bar.close < context.opening_range_low
                    and bar.close < vwap
                    and bar.close < bar.open
                )
                if trigger:
                    entry = bar.close
                    stop = max(bar.high, context.opening_range_low + tolerance)
                    return "TrendRetest", "Short", index, entry, stop, entry - 1.5 * (stop - entry)

    if context.regime == "Balance":
        for index, bar in enumerate(bars):
            if bar.timestamp > context.decision_time + timedelta(hours=3):
                break
            vwap = vwaps[bar.timestamp]
            if bar.high > context.opening_range_high and bar.close < context.opening_range_high:
                entry = bar.close
                stop = bar.high + spec.tick_size
                risk = stop - entry
                reward = entry - vwap
                if reward >= 1.2 * risk:
                    return "BalanceRejection", "Short", index, entry, stop, vwap
            if bar.low < context.opening_range_low and bar.close > context.opening_range_low:
                entry = bar.close
                stop = bar.low - spec.tick_size
                risk = entry - stop
                reward = vwap - entry
                if reward >= 1.2 * risk:
                    return "BalanceRejection", "Long", index, entry, stop, vwap
    return None


def evaluate_signal(
    symbol: str,
    context: DayContext,
    bars: list[Bar],
    signal: tuple[str, str, int, float, float, float],
) -> Optional[Trade]:
    playbook, direction, signal_index, entry, stop, target = signal
    spec = SPECS[symbol]
    risk_points = abs(entry - stop)
    risk_currency = risk_points * spec.point_value
    if not (MINIMUM_RISK_CURRENCY <= risk_currency <= MAXIMUM_RISK_CURRENCY):
        return None

    status = "TimeExit"
    exit_price = bars[-1].close
    exit_time = bars[-1].timestamp
    for bar in bars[signal_index + 1:]:
        stop_hit = bar.low <= stop if direction == "Long" else bar.high >= stop
        target_hit = bar.high >= target if direction == "Long" else bar.low <= target
        if stop_hit:
            status = "AmbiguousLoss" if target_hit else "Stop"
            exit_price = stop
            exit_time = bar.timestamp
            break
        if target_hit:
            status = "Target"
            exit_price = target
            exit_time = bar.timestamp
            break
    gross = (
        (exit_price - entry) if direction == "Long" else (entry - exit_price)
    ) * spec.point_value
    return Trade(
        instrument=symbol,
        trading_day=context.trading_day,
        playbook=playbook,
        direction=direction,
        regime=context.regime,
        signal_time=bars[signal_index].timestamp,
        exit_time=exit_time,
        entry=entry,
        stop=stop,
        target=target,
        risk_currency=round(risk_currency, 2),
        status=status,
        gross_currency=round(gross, 2),
        net_currency=round(gross - ROUND_TURN_COST, 2),
    )


def generate_trades(
    symbol: str,
    days: dict[date, list[Bar]],
    contexts: dict[date, DayContext],
) -> list[Trade]:
    trades: list[Trade] = []
    for trading_day, context in sorted(contexts.items()):
        future = rth_bars_after_decision(days[trading_day], context)
        if not future or context.regime == "Transition":
            continue
        signal = find_signal(symbol, days[trading_day], context)
        if signal is None:
            continue
        trade = evaluate_signal(symbol, context, future, signal)
        if trade is not None:
            trades.append(trade)
    return trades


def maximum_drawdown(values: Iterable[float]) -> float:
    equity = peak = drawdown = 0.0
    for value in values:
        equity += value
        peak = max(peak, equity)
        drawdown = max(drawdown, peak - equity)
    return drawdown


def summarize(trades: list[Trade]) -> dict[str, object]:
    decided = [trade for trade in trades if trade.status != "TimeExit"]
    net_values = [trade.net_currency for trade in trades]
    wins = [value for value in net_values if value > 0]
    losses = [value for value in net_values if value <= 0]
    gross_profit = sum(wins)
    gross_loss = abs(sum(losses))
    return {
        "trades": len(trades),
        "active_sessions": len({trade.trading_day for trade in trades}),
        "targets": sum(trade.status == "Target" for trade in trades),
        "stops": sum(trade.status in ("Stop", "AmbiguousLoss") for trade in trades),
        "time_exits": sum(trade.status == "TimeExit" for trade in trades),
        "win_rate": round(len(wins) / len(trades), 6) if trades else None,
        "net_currency": round(sum(net_values), 2),
        "average_trade_currency": round(statistics.mean(net_values), 2) if trades else None,
        "profit_factor": round(gross_profit / gross_loss, 4) if gross_loss else None,
        "maximum_drawdown_currency": round(maximum_drawdown(net_values), 2),
        "decided_trades": len(decided),
    }


def chronological_blocks(trades: list[Trade], sessions: list[date]) -> list[dict[str, object]]:
    if not sessions:
        return []
    blocks: list[dict[str, object]] = []
    for block_number in range(3):
        start = math.floor(len(sessions) * block_number / 3)
        end = math.floor(len(sessions) * (block_number + 1) / 3)
        selected = set(sessions[start:end])
        block_trades = [trade for trade in trades if trade.trading_day in selected]
        blocks.append(
            {
                "block": block_number + 1,
                "from": sessions[start].isoformat() if start < len(sessions) else None,
                "to": sessions[end - 1].isoformat() if end > start else None,
                **summarize(block_trades),
            }
        )
    return blocks


def regime_counts(contexts: dict[date, DayContext]) -> dict[str, int]:
    counts: dict[str, int] = defaultdict(int)
    for context in contexts.values():
        counts[context.regime] += 1
    return dict(sorted(counts.items()))


def economic_gate(trades: list[Trade], sessions: list[date]) -> dict[str, object]:
    rules = prop_evaluation.EvaluationRules(
        target_currency=1_500.0,
        maximum_trailing_drawdown_currency=1_500.0,
        maximum_sessions=20,
        minimum_active_days=5,
        maximum_best_day_share=0.50,
    )
    grouped = prop_evaluation.trades_by_session(sessions, trades)
    scenarios: list[dict[str, object]] = []
    for contracts in range(1, 31):
        attempts = prop_evaluation.rolling_attempts(grouped, contracts, rules)
        summary = prop_evaluation.summarize_attempts(attempts)
        scenarios.append(
            {
                "micro_contracts": contracts,
                "maximum_configured_risk_per_trade_currency": (
                    MAXIMUM_RISK_CURRENCY * contracts
                ),
                **summary,
            }
        )
    qualified = [
        item for item in scenarios
        if item["pass_rate_percent"] >= 60.0
        and item["drawdown_failure_rate_percent"] <= 15.0
        and item["p90_maximum_drawdown_currency"] <= 1_000.0
    ]
    best = max(
        scenarios,
        key=lambda item: (
            item["pass_rate_percent"],
            -item["drawdown_failure_rate_percent"],
            -item["micro_contracts"],
        ),
    )
    return {
        "rules": asdict(rules),
        "acceptance": {
            "minimum_pass_rate_percent": 60.0,
            "maximum_drawdown_failure_rate_percent": 15.0,
            "maximum_p90_drawdown_currency": 1_000.0,
        },
        "best_scenario": best,
        "qualified_micro_contracts": [
            item["micro_contracts"] for item in qualified
        ],
        "passed": bool(qualified),
        "scenarios": scenarios,
    }


def run_self_tests() -> None:
    assert trading_day_for(datetime(2026, 7, 1, 19, 1)) == date(2026, 7, 2)
    assert trading_day_for(datetime(2026, 3, 2, 19, 1)) == date(2026, 3, 2)
    assert regular_open_for(date(2026, 7, 1)).time() == time(10, 30)
    assert regular_open_for(date(2026, 3, 2)).time() == time(11, 30)
    assert maximum_drawdown([100, -40, -90, 50]) == 130
    assert round(percentile_rank([1, 2, 3, 4], 3), 2) == 0.75


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--mnq", type=Path, required=True)
    parser.add_argument("--mes", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()

    if args.self_test:
        run_self_tests()

    loaded = [load_bars(args.mnq), load_bars(args.mes)]
    if {item[0] for item in loaded} != {"MNQ", "MES"}:
        raise ValueError("Os arquivos precisam representar MNQ e MES.")
    grouped = {symbol: group_by_day(bars) for symbol, bars, _ in loaded}
    contexts = {
        symbol: calculate_contexts(symbol, grouped[symbol]) for symbol in grouped
    }
    apply_cross_market_confirmation(contexts["MNQ"], contexts["MES"])

    shared_sessions = sorted(set(contexts["MNQ"]) & set(contexts["MES"]))
    all_trades: list[Trade] = []
    instrument_results: dict[str, object] = {}
    for symbol in ("MNQ", "MES"):
        trades = generate_trades(symbol, grouped[symbol], contexts[symbol])
        all_trades.extend(trades)
        instrument_results[symbol] = {
            "regime_counts": regime_counts(contexts[symbol]),
            "overall": summarize(trades),
            "chronological_thirds": chronological_blocks(trades, shared_sessions),
            "by_playbook": {
                playbook: summarize([trade for trade in trades if trade.playbook == playbook])
                for playbook in ("TrendRetest", "BalanceRejection")
            },
            "by_direction": {
                direction: summarize([trade for trade in trades if trade.direction == direction])
                for direction in ("Long", "Short")
            },
            "economic_gate_20_sessions": economic_gate(trades, shared_sessions),
        }

    result = {
        "method": {
            "name": "RegimeStructureFlowV1",
            "bar_period": "1 minute",
            "decision_time": "30 minutes after RTH open",
            "regimes": ["TrendUp", "TrendDown", "Balance", "Transition"],
            "playbooks": ["TrendRetest", "BalanceRejection"],
            "cross_market_confirmation": "MNQ and MES direction/acceptance",
            "risk_currency_per_contract": {
                "minimum": MINIMUM_RISK_CURRENCY,
                "maximum": MAXIMUM_RISK_CURRENCY,
            },
            "round_turn_cost_currency": ROUND_TURN_COST,
            "order_flow": "not available in OHLCV; reserved for later tick/volumetric validation",
        },
        "data_audit": {symbol: audit for symbol, _, audit in loaded},
        "shared_sessions": len(shared_sessions),
        "from_session": shared_sessions[0].isoformat(),
        "to_session": shared_sessions[-1].isoformat(),
        "instruments": instrument_results,
        "combined_research_only": summarize(sorted(all_trades, key=lambda trade: trade.signal_time)),
        "trades": [
            {
                **asdict(trade),
                "trading_day": trade.trading_day.isoformat(),
                "signal_time": trade.signal_time.isoformat(sep=" "),
                "exit_time": trade.exit_time.isoformat(sep=" "),
            }
            for trade in sorted(all_trades, key=lambda trade: trade.signal_time)
        ],
        "limitations": [
            "O bloco histórico final já foi consultado em pesquisas anteriores e não é teste intocado.",
            "OHLCV de 1 minuto não contém bid/ask delta, imbalance ou absorção.",
            "MNQ e MES são correlacionados; o resultado combinado não autoriza operar ambos.",
            "Uma estratégia só pode avançar após estabilidade nos três blocos e dados futuros.",
        ],
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(json.dumps({
        "output": str(args.output),
        "shared_sessions": len(shared_sessions),
        "MNQ": instrument_results["MNQ"]["overall"],
        "MES": instrument_results["MES"]["overall"],
    }, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
