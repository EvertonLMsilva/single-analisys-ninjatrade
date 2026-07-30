#!/usr/bin/env python3
"""Busca controlada de gatilhos estruturais com validação cronológica.

Reutiliza o classificador de regime congelado em regime_structure_backtest.py.
Os gatilhos representam mecanismos diferentes; somente o alvo varia entre 1R, 1,5R
e 2R. Não acessa o NinjaTrader e não envia ordens.
"""

from __future__ import annotations

import argparse
import itertools
import json
import statistics
from dataclasses import asdict, dataclass
from datetime import date, datetime, timedelta
from pathlib import Path
from typing import Callable, Optional

import prop_evaluation
import regime_structure_backtest as base


@dataclass(frozen=True)
class Candidate:
    family: str
    target_r: float

    @property
    def name(self) -> str:
        return f"{self.family}_{self.target_r:g}R"


@dataclass
class RawEvent:
    instrument: str
    trading_day: date
    regime: str
    candidate: Candidate
    direction: str
    signal_time: datetime
    entry: float
    stop: float
    risk_currency: float
    mfe_r_60m: float
    mae_r_60m: float
    accepted_by_risk: bool


@dataclass
class Trade:
    instrument: str
    trading_day: date
    regime: str
    candidate: Candidate
    direction: str
    signal_time: datetime
    exit_time: datetime
    entry: float
    stop: float
    target: float
    risk_currency: float
    status: str
    gross_currency: float
    net_currency: float


Signal = tuple[str, int, float, float]


def candidates() -> list[Candidate]:
    families = (
        "BreakoutAcceptance",
        "BreakoutRetestConfirm",
        "VwapReclaim",
        "VwapReclaimHold",
        "OpeningRangeReclaim",
        "MomentumContinuation",
        "MomentumPullbackConfirm",
        "BalanceSweep",
        "BalanceVwapRecovery",
        "CrossMarketBreakout",
        "CrossMarketVwapReclaim",
        "CrossMarketMomentum",
    )
    return [
        Candidate(family, target_r)
        for family, target_r in itertools.product(families, (1.0, 1.5, 2.0))
    ]


def close_location(bar: base.Bar) -> float:
    spread = bar.high - bar.low
    return (bar.close - bar.low) / spread if spread else 0.5


def trend_direction(context: base.DayContext) -> Optional[str]:
    if context.regime == "TrendUp":
        return "Long"
    if context.regime == "TrendDown":
        return "Short"
    return None


def cross_market_direction(context: base.DayContext) -> Optional[str]:
    if not context.cross_market_aligned:
        return None
    if context.direction > 0:
        return "Long"
    if context.direction < 0:
        return "Short"
    return None


def breakout_acceptance(
    bars: list[base.Bar],
    context: base.DayContext,
    vwaps: dict[datetime, float],
    spec: base.InstrumentSpec,
) -> Optional[Signal]:
    direction = trend_direction(context)
    if direction is None:
        return None
    accepted = 0
    for index, bar in enumerate(bars):
        if bar.timestamp > context.decision_time + timedelta(hours=2):
            break
        aligned = (
            bar.close > context.opening_range_high and bar.close > vwaps[bar.timestamp]
            if direction == "Long"
            else bar.close < context.opening_range_low and bar.close < vwaps[bar.timestamp]
        )
        accepted = accepted + 1 if aligned else 0
        if accepted < 2:
            continue
        recent = bars[max(0, index - 2): index + 1]
        entry = bar.close
        stop = (
            min(item.low for item in recent) - spec.tick_size
            if direction == "Long"
            else max(item.high for item in recent) + spec.tick_size
        )
        return direction, index, entry, stop
    return None


def vwap_reclaim(
    bars: list[base.Bar],
    context: base.DayContext,
    vwaps: dict[datetime, float],
    spec: base.InstrumentSpec,
) -> Optional[Signal]:
    direction = trend_direction(context)
    if direction is None:
        return None
    armed = False
    extreme: Optional[float] = None
    for index, bar in enumerate(bars):
        if bar.timestamp > context.decision_time + timedelta(hours=3):
            break
        vwap = vwaps[bar.timestamp]
        if direction == "Long":
            if bar.close <= vwap:
                armed = True
                extreme = bar.low if extreme is None else min(extreme, bar.low)
            elif armed and bar.close > vwap and bar.close > bar.open:
                return direction, index, bar.close, min(extreme, bar.low) - spec.tick_size
        else:
            if bar.close >= vwap:
                armed = True
                extreme = bar.high if extreme is None else max(extreme, bar.high)
            elif armed and bar.close < vwap and bar.close < bar.open:
                return direction, index, bar.close, max(extreme, bar.high) + spec.tick_size
    return None


def opening_range_reclaim(
    bars: list[base.Bar],
    context: base.DayContext,
    vwaps: dict[datetime, float],
    spec: base.InstrumentSpec,
) -> Optional[Signal]:
    direction = trend_direction(context)
    if direction is None:
        return None
    extreme: Optional[float] = None
    for index, bar in enumerate(bars):
        if bar.timestamp > context.decision_time + timedelta(hours=3):
            break
        if direction == "Long":
            if bar.low < context.opening_range_low:
                extreme = bar.low if extreme is None else min(extreme, bar.low)
            if (
                extreme is not None
                and bar.close > context.opening_range_low
                and bar.close > bar.open
                and bar.close > vwaps[bar.timestamp]
            ):
                return direction, index, bar.close, extreme - spec.tick_size
        else:
            if bar.high > context.opening_range_high:
                extreme = bar.high if extreme is None else max(extreme, bar.high)
            if (
                extreme is not None
                and bar.close < context.opening_range_high
                and bar.close < bar.open
                and bar.close < vwaps[bar.timestamp]
            ):
                return direction, index, bar.close, extreme + spec.tick_size
    return None


def momentum_continuation(
    bars: list[base.Bar],
    context: base.DayContext,
    vwaps: dict[datetime, float],
    spec: base.InstrumentSpec,
) -> Optional[Signal]:
    direction = trend_direction(context)
    if direction is None:
        return None
    volumes: list[float] = []
    for index, bar in enumerate(bars):
        if bar.timestamp > context.decision_time + timedelta(hours=2):
            break
        if index < 5:
            volumes.append(bar.volume)
            continue
        recent = bars[index - 5:index]
        median_volume = statistics.median(volumes[-20:]) if volumes else bar.volume
        strong_volume = bar.volume >= 1.20 * median_volume
        location = close_location(bar)
        if (
            direction == "Long"
            and bar.close > max(item.high for item in recent)
            and bar.close > vwaps[bar.timestamp]
            and location >= 0.75
            and strong_volume
        ):
            return direction, index, bar.close, min(item.low for item in bars[index - 2:index + 1]) - spec.tick_size
        if (
            direction == "Short"
            and bar.close < min(item.low for item in recent)
            and bar.close < vwaps[bar.timestamp]
            and location <= 0.25
            and strong_volume
        ):
            return direction, index, bar.close, max(item.high for item in bars[index - 2:index + 1]) + spec.tick_size
        volumes.append(bar.volume)
    return None


def balance_sweep(
    bars: list[base.Bar],
    context: base.DayContext,
    vwaps: dict[datetime, float],
    spec: base.InstrumentSpec,
) -> Optional[Signal]:
    if context.regime != "Balance":
        return None
    sweep_direction: Optional[str] = None
    extreme: Optional[float] = None
    inside_closes = 0
    for index, bar in enumerate(bars):
        if bar.timestamp > context.decision_time + timedelta(hours=3):
            break
        if bar.high > context.opening_range_high:
            sweep_direction = "Short"
            extreme = bar.high if extreme is None else max(extreme, bar.high)
            inside_closes = 0
        elif bar.low < context.opening_range_low:
            sweep_direction = "Long"
            extreme = bar.low if extreme is None else min(extreme, bar.low)
            inside_closes = 0
        if sweep_direction == "Short" and bar.close < context.opening_range_high:
            inside_closes += 1
            if inside_closes >= 2 and bar.close < bar.open:
                return "Short", index, bar.close, max(extreme, bar.high) + spec.tick_size
        elif sweep_direction == "Long" and bar.close > context.opening_range_low:
            inside_closes += 1
            if inside_closes >= 2 and bar.close > bar.open:
                return "Long", index, bar.close, min(extreme, bar.low) - spec.tick_size
    return None


def breakout_retest_confirm(
    bars: list[base.Bar],
    context: base.DayContext,
    vwaps: dict[datetime, float],
    spec: base.InstrumentSpec,
) -> Optional[Signal]:
    direction = trend_direction(context)
    if direction is None:
        return None
    accepted = 0
    retest_index: Optional[int] = None
    tolerance = context.opening_range * 0.10
    for index, bar in enumerate(bars):
        if bar.timestamp > context.decision_time + timedelta(hours=3):
            break
        if retest_index is None:
            outside = (
                bar.close > context.opening_range_high
                if direction == "Long"
                else bar.close < context.opening_range_low
            )
            accepted = accepted + 1 if outside else 0
            if accepted >= 2:
                retest_index = -1
            continue
        if retest_index == -1:
            if direction == "Long":
                held = (
                    bar.low <= context.opening_range_high + tolerance
                    and bar.close >= context.opening_range_high
                    and bar.close > bar.open
                )
            else:
                held = (
                    bar.high >= context.opening_range_low - tolerance
                    and bar.close <= context.opening_range_low
                    and bar.close < bar.open
                )
            if held:
                retest_index = index
            continue
        confirmation = bars[retest_index]
        if index > retest_index + 3:
            retest_index = -1
            continue
        if direction == "Long" and bar.close > confirmation.high and bar.close > vwaps[bar.timestamp]:
            stop = min(item.low for item in bars[retest_index:index + 1]) - spec.tick_size
            return direction, index, bar.close, stop
        if direction == "Short" and bar.close < confirmation.low and bar.close < vwaps[bar.timestamp]:
            stop = max(item.high for item in bars[retest_index:index + 1]) + spec.tick_size
            return direction, index, bar.close, stop
    return None


def vwap_reclaim_hold(
    bars: list[base.Bar],
    context: base.DayContext,
    vwaps: dict[datetime, float],
    spec: base.InstrumentSpec,
) -> Optional[Signal]:
    direction = trend_direction(context)
    if direction is None:
        return None
    armed = False
    correct_side = 0
    for index, bar in enumerate(bars):
        if bar.timestamp > context.decision_time + timedelta(hours=3):
            break
        vwap = vwaps[bar.timestamp]
        adverse = bar.close <= vwap if direction == "Long" else bar.close >= vwap
        if adverse:
            armed = True
            correct_side = 0
            continue
        if not armed:
            continue
        correct_side += 1
        if correct_side < 3 or index < 3:
            continue
        recent = bars[index - 2:index + 1]
        if direction == "Long" and bar.close > max(item.high for item in bars[index - 2:index]):
            return direction, index, bar.close, min(item.low for item in recent) - spec.tick_size
        if direction == "Short" and bar.close < min(item.low for item in bars[index - 2:index]):
            return direction, index, bar.close, max(item.high for item in recent) + spec.tick_size
    return None


def momentum_pullback_confirm(
    bars: list[base.Bar],
    context: base.DayContext,
    vwaps: dict[datetime, float],
    spec: base.InstrumentSpec,
) -> Optional[Signal]:
    direction = trend_direction(context)
    if direction is None:
        return None
    impulse_index: Optional[int] = None
    impulse_level: Optional[float] = None
    for index, bar in enumerate(bars):
        if bar.timestamp > context.decision_time + timedelta(hours=3):
            break
        if impulse_index is None:
            if index < 5:
                continue
            previous = bars[index - 5:index]
            if (
                direction == "Long"
                and bar.close > max(item.high for item in previous)
                and close_location(bar) >= 0.75
            ):
                impulse_index = index
                impulse_level = max(item.high for item in previous)
            elif (
                direction == "Short"
                and bar.close < min(item.low for item in previous)
                and close_location(bar) <= 0.25
            ):
                impulse_index = index
                impulse_level = min(item.low for item in previous)
            continue
        if index > impulse_index + 15:
            impulse_index = None
            impulse_level = None
            continue
        if direction == "Long":
            pullback = bar.low <= impulse_level and bar.close >= impulse_level
            if pullback and bar.close > bar.open and bar.close > vwaps[bar.timestamp]:
                recent = bars[max(impulse_index, index - 3):index + 1]
                return direction, index, bar.close, min(item.low for item in recent) - spec.tick_size
        else:
            pullback = bar.high >= impulse_level and bar.close <= impulse_level
            if pullback and bar.close < bar.open and bar.close < vwaps[bar.timestamp]:
                recent = bars[max(impulse_index, index - 3):index + 1]
                return direction, index, bar.close, max(item.high for item in recent) + spec.tick_size
    return None


def balance_vwap_recovery(
    bars: list[base.Bar],
    context: base.DayContext,
    vwaps: dict[datetime, float],
    spec: base.InstrumentSpec,
) -> Optional[Signal]:
    if context.regime != "Balance":
        return None
    swept: Optional[str] = None
    for index, bar in enumerate(bars):
        if bar.timestamp > context.decision_time + timedelta(hours=3):
            break
        if bar.high > context.opening_range_high:
            swept = "Short"
        elif bar.low < context.opening_range_low:
            swept = "Long"
        if swept == "Short" and bar.close < vwaps[bar.timestamp] and bar.close < bar.open:
            recent = bars[max(0, index - 4):index + 1]
            return "Short", index, bar.close, max(item.high for item in recent) + spec.tick_size
        if swept == "Long" and bar.close > vwaps[bar.timestamp] and bar.close > bar.open:
            recent = bars[max(0, index - 4):index + 1]
            return "Long", index, bar.close, min(item.low for item in recent) - spec.tick_size
    return None


def cross_market_breakout(
    bars: list[base.Bar],
    context: base.DayContext,
    vwaps: dict[datetime, float],
    spec: base.InstrumentSpec,
) -> Optional[Signal]:
    direction = cross_market_direction(context)
    if direction is None:
        return None
    accepted = 0
    for index, bar in enumerate(bars):
        if bar.timestamp > context.decision_time + timedelta(hours=2):
            break
        aligned = (
            bar.close > context.opening_range_high and bar.close > vwaps[bar.timestamp]
            if direction == "Long"
            else bar.close < context.opening_range_low and bar.close < vwaps[bar.timestamp]
        )
        accepted = accepted + 1 if aligned else 0
        if accepted < 2:
            continue
        recent = bars[max(0, index - 2):index + 1]
        stop = (
            min(item.low for item in recent) - spec.tick_size
            if direction == "Long"
            else max(item.high for item in recent) + spec.tick_size
        )
        return direction, index, bar.close, stop
    return None


def cross_market_vwap_reclaim(
    bars: list[base.Bar],
    context: base.DayContext,
    vwaps: dict[datetime, float],
    spec: base.InstrumentSpec,
) -> Optional[Signal]:
    direction = cross_market_direction(context)
    if direction is None:
        return None
    armed = False
    correct_side = 0
    for index, bar in enumerate(bars):
        if bar.timestamp > context.decision_time + timedelta(hours=3):
            break
        vwap = vwaps[bar.timestamp]
        adverse = bar.close <= vwap if direction == "Long" else bar.close >= vwap
        if adverse:
            armed = True
            correct_side = 0
            continue
        if not armed:
            continue
        correct_side += 1
        if correct_side < 2 or index < 2:
            continue
        recent = bars[index - 2:index + 1]
        stop = (
            min(item.low for item in recent) - spec.tick_size
            if direction == "Long"
            else max(item.high for item in recent) + spec.tick_size
        )
        return direction, index, bar.close, stop
    return None


def cross_market_momentum(
    bars: list[base.Bar],
    context: base.DayContext,
    vwaps: dict[datetime, float],
    spec: base.InstrumentSpec,
) -> Optional[Signal]:
    direction = cross_market_direction(context)
    if direction is None:
        return None
    volumes: list[float] = []
    for index, bar in enumerate(bars):
        if bar.timestamp > context.decision_time + timedelta(hours=2):
            break
        if index < 5:
            volumes.append(bar.volume)
            continue
        recent = bars[index - 5:index]
        median_volume = statistics.median(volumes[-20:])
        if direction == "Long":
            trigger = (
                bar.close > max(item.high for item in recent)
                and bar.close > vwaps[bar.timestamp]
                and close_location(bar) >= 0.70
                and bar.volume >= 1.10 * median_volume
            )
        else:
            trigger = (
                bar.close < min(item.low for item in recent)
                and bar.close < vwaps[bar.timestamp]
                and close_location(bar) <= 0.30
                and bar.volume >= 1.10 * median_volume
            )
        if trigger:
            stop = (
                min(item.low for item in bars[index - 2:index + 1]) - spec.tick_size
                if direction == "Long"
                else max(item.high for item in bars[index - 2:index + 1]) + spec.tick_size
            )
            return direction, index, bar.close, stop
        volumes.append(bar.volume)
    return None


FINDERS: dict[
    str,
    Callable[
        [list[base.Bar], base.DayContext, dict[datetime, float], base.InstrumentSpec],
        Optional[Signal],
    ],
] = {
    "BreakoutAcceptance": breakout_acceptance,
    "BreakoutRetestConfirm": breakout_retest_confirm,
    "VwapReclaim": vwap_reclaim,
    "VwapReclaimHold": vwap_reclaim_hold,
    "OpeningRangeReclaim": opening_range_reclaim,
    "MomentumContinuation": momentum_continuation,
    "MomentumPullbackConfirm": momentum_pullback_confirm,
    "BalanceSweep": balance_sweep,
    "BalanceVwapRecovery": balance_vwap_recovery,
    "CrossMarketBreakout": cross_market_breakout,
    "CrossMarketVwapReclaim": cross_market_vwap_reclaim,
    "CrossMarketMomentum": cross_market_momentum,
}


def excursion(
    bars: list[base.Bar],
    signal_index: int,
    direction: str,
    entry: float,
    risk_points: float,
) -> tuple[float, float]:
    horizon_end = bars[signal_index].timestamp + timedelta(minutes=60)
    horizon = [
        bar for bar in bars[signal_index + 1:]
        if bar.timestamp <= horizon_end
    ]
    if not horizon or risk_points <= 0:
        return 0.0, 0.0
    if direction == "Long":
        mfe = max(bar.high - entry for bar in horizon)
        mae = max(entry - bar.low for bar in horizon)
    else:
        mfe = max(entry - bar.low for bar in horizon)
        mae = max(bar.high - entry for bar in horizon)
    return max(0.0, mfe / risk_points), max(0.0, mae / risk_points)


def evaluate(
    symbol: str,
    context: base.DayContext,
    bars: list[base.Bar],
    candidate: Candidate,
    signal: Signal,
) -> tuple[RawEvent, Optional[Trade]]:
    direction, signal_index, entry, stop = signal
    spec = base.SPECS[symbol]
    risk_points = entry - stop if direction == "Long" else stop - entry
    risk_currency = risk_points * spec.point_value
    accepted = (
        risk_points > 0
        and base.MINIMUM_RISK_CURRENCY <= risk_currency <= base.MAXIMUM_RISK_CURRENCY
    )
    mfe_r, mae_r = excursion(bars, signal_index, direction, entry, risk_points)
    raw_event = RawEvent(
        instrument=symbol,
        trading_day=context.trading_day,
        regime=context.regime,
        candidate=candidate,
        direction=direction,
        signal_time=bars[signal_index].timestamp,
        entry=entry,
        stop=stop,
        risk_currency=round(risk_currency, 2),
        mfe_r_60m=round(mfe_r, 4),
        mae_r_60m=round(mae_r, 4),
        accepted_by_risk=accepted,
    )
    if not accepted:
        return raw_event, None

    target = (
        entry + candidate.target_r * risk_points
        if direction == "Long"
        else entry - candidate.target_r * risk_points
    )
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
        exit_price - entry if direction == "Long" else entry - exit_price
    ) * spec.point_value
    return raw_event, Trade(
        instrument=symbol,
        trading_day=context.trading_day,
        regime=context.regime,
        candidate=candidate,
        direction=direction,
        signal_time=bars[signal_index].timestamp,
        exit_time=exit_time,
        entry=entry,
        stop=stop,
        target=target,
        risk_currency=round(risk_currency, 2),
        status=status,
        gross_currency=round(gross, 2),
        net_currency=round(gross - base.ROUND_TURN_COST, 2),
    )


def simulate_candidate(
    symbol: str,
    grouped: dict[date, list[base.Bar]],
    contexts: dict[date, base.DayContext],
    candidate: Candidate,
) -> tuple[list[RawEvent], list[Trade]]:
    raw_events: list[RawEvent] = []
    trades: list[Trade] = []
    finder = FINDERS[candidate.family]
    for trading_day, context in sorted(contexts.items()):
        bars = base.rth_bars_after_decision(grouped[trading_day], context)
        if not bars:
            continue
        signal = finder(
            bars,
            context,
            base.rolling_vwaps(grouped[trading_day], context),
            base.SPECS[symbol],
        )
        if signal is None:
            continue
        raw_event, trade = evaluate(symbol, context, bars, candidate, signal)
        raw_events.append(raw_event)
        if trade is not None:
            trades.append(trade)
    return raw_events, trades


def metrics(trades: list[Trade]) -> dict[str, object]:
    values = [trade.net_currency for trade in trades]
    wins = [value for value in values if value > 0]
    losses = [value for value in values if value <= 0]
    gross_profit = sum(wins)
    gross_loss = abs(sum(losses))
    return {
        "trades": len(trades),
        "active_sessions": len({trade.trading_day for trade in trades}),
        "targets": sum(trade.status == "Target" for trade in trades),
        "stops": sum(trade.status in ("Stop", "AmbiguousLoss") for trade in trades),
        "time_exits": sum(trade.status == "TimeExit" for trade in trades),
        "win_rate": round(len(wins) / len(trades), 6) if trades else None,
        "net_currency": round(sum(values), 2),
        "average_trade_currency": round(statistics.mean(values), 2) if values else None,
        "profit_factor": round(gross_profit / gross_loss, 4) if gross_loss else None,
        "maximum_drawdown_currency": round(base.maximum_drawdown(values), 2),
    }


def event_metrics(events: list[RawEvent]) -> dict[str, object]:
    return {
        "events": len(events),
        "accepted_by_risk": sum(event.accepted_by_risk for event in events),
        "risk_rejection_rate": round(
            sum(not event.accepted_by_risk for event in events) / len(events), 6
        ) if events else None,
        "median_risk_currency": round(
            statistics.median(event.risk_currency for event in events), 2
        ) if events else None,
        "median_mfe_r_60m": round(
            statistics.median(event.mfe_r_60m for event in events), 4
        ) if events else None,
        "median_mae_r_60m": round(
            statistics.median(event.mae_r_60m for event in events), 4
        ) if events else None,
        "mfe_at_least_1r_rate": round(
            sum(event.mfe_r_60m >= 1.0 for event in events) / len(events), 6
        ) if events else None,
        "mae_before_1r_proxy_rate": round(
            sum(event.mae_r_60m >= 1.0 and event.mfe_r_60m < 1.0 for event in events)
            / len(events),
            6,
        ) if events else None,
    }


def split_sessions(sessions: list[date]) -> dict[str, list[date]]:
    first = len(sessions) // 2
    second = first + (len(sessions) - first) // 2
    return {
        "selection": sessions[:first],
        "validation": sessions[first:second],
        "confirmation": sessions[second:],
    }


def subset(trades: list[Trade], sessions: list[date]) -> list[Trade]:
    selected = set(sessions)
    return [trade for trade in trades if trade.trading_day in selected]


def passes_block(
    block_metrics: dict[str, object], minimum_trades: int
) -> bool:
    profit_factor = block_metrics["profit_factor"]
    return (
        block_metrics["trades"] >= minimum_trades
        and block_metrics["net_currency"] > 0
        and profit_factor is not None
        and profit_factor >= 1.10
    )


def viable_component(block_metrics: dict[str, object]) -> bool:
    profit_factor = block_metrics["profit_factor"]
    return (
        block_metrics["trades"] >= 3
        and block_metrics["net_currency"] > 0
        and profit_factor is not None
        and profit_factor >= 1.10
    )


def economic_gate(trades: list[Trade], sessions: list[date]) -> dict[str, object]:
    rules = prop_evaluation.EvaluationRules(1_500.0, 1_500.0, 20, 5, 0.50)
    daily = prop_evaluation.trades_by_session(sessions, trades)
    scenarios = []
    for contracts in range(1, 31):
        summary = prop_evaluation.summarize_attempts(
            prop_evaluation.rolling_attempts(daily, contracts, rules)
        )
        scenarios.append({"micro_contracts": contracts, **summary})
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
            -item["p90_maximum_drawdown_currency"],
        ),
    )
    return {
        "passed": bool(qualified),
        "qualified_micro_contracts": [
            item["micro_contracts"] for item in qualified
        ],
        "best_scenario": best,
        "scenarios": scenarios,
    }


def choose_best(rows: list[dict[str, object]]) -> Optional[dict[str, object]]:
    eligible = [
        row for row in rows
        if row["selection_passed"] and row["validation_passed"]
    ]
    if not eligible:
        return None
    return max(
        eligible,
        key=lambda row: (
            min(
                row["selection"]["average_trade_currency"],
                row["validation"]["average_trade_currency"],
            ),
            row["validation"]["net_currency"],
            -row["validation"]["maximum_drawdown_currency"],
        ),
    )


def combine_earliest_by_day(
    candidates_trades: list[list[Trade]],
) -> list[Trade]:
    by_day: dict[date, Trade] = {}
    for trade in itertools.chain.from_iterable(candidates_trades):
        current = by_day.get(trade.trading_day)
        if current is None or trade.signal_time < current.signal_time:
            by_day[trade.trading_day] = trade
    return sorted(by_day.values(), key=lambda trade: trade.signal_time)


def run_self_tests() -> None:
    assert len(candidates()) == 36
    assert close_location(
        base.Bar(datetime(2026, 1, 1), 1, 3, 1, 2.5, 1, date(2026, 1, 1))
    ) == 0.75
    sessions = [date(2026, 1, day) for day in range(1, 10)]
    splits = split_sessions(sessions)
    assert [len(splits[name]) for name in ("selection", "validation", "confirmation")] == [4, 2, 3]


def serialize_trade(trade: Trade) -> dict[str, object]:
    return {
        **asdict(trade),
        "candidate": asdict(trade.candidate),
        "trading_day": trade.trading_day.isoformat(),
        "signal_time": trade.signal_time.isoformat(sep=" "),
        "exit_time": trade.exit_time.isoformat(sep=" "),
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--mnq", type=Path, required=True)
    parser.add_argument("--mes", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        run_self_tests()

    loaded = [base.load_bars(args.mnq), base.load_bars(args.mes)]
    grouped = {symbol: base.group_by_day(bars) for symbol, bars, _ in loaded}
    contexts = {
        symbol: base.calculate_contexts(symbol, grouped[symbol])
        for symbol in ("MNQ", "MES")
    }
    base.apply_cross_market_confirmation(contexts["MNQ"], contexts["MES"])
    sessions = sorted(set(contexts["MNQ"]) & set(contexts["MES"]))
    splits = split_sessions(sessions)
    minimum_trades = {
        "selection": max(5, len(splits["selection"]) // 5),
        "validation": max(4, len(splits["validation"]) // 5),
    }

    rows: list[dict[str, object]] = []
    trades_by_key: dict[tuple[str, str], list[Trade]] = {}
    events_by_key: dict[tuple[str, str], list[RawEvent]] = {}
    for symbol in ("MNQ", "MES"):
        for candidate in candidates():
            events, trades = simulate_candidate(
                symbol, grouped[symbol], contexts[symbol], candidate
            )
            key = (symbol, candidate.name)
            trades_by_key[key] = trades
            events_by_key[key] = events
            block_metrics = {
                name: metrics(subset(trades, block_sessions))
                for name, block_sessions in splits.items()
            }
            rows.append(
                {
                    "instrument": symbol,
                    "candidate": asdict(candidate),
                    "events": event_metrics(events),
                    **block_metrics,
                    "overall": metrics(trades),
                    "selection_passed": passes_block(
                        block_metrics["selection"], minimum_trades["selection"]
                    ),
                    "validation_passed": passes_block(
                        block_metrics["validation"], minimum_trades["validation"]
                    ),
                }
            )

    best_individual = choose_best(rows)
    selected_rows = [
        row for row in rows
        if row["selection_passed"] and row["validation_passed"]
    ]

    portfolio_rows: list[dict[str, object]] = []
    component_survivors = [
        row for row in rows if viable_component(row["selection"])
    ]
    for left, right in itertools.combinations(component_survivors, 2):
        if left["instrument"] != right["instrument"]:
            continue
        if left["candidate"]["family"] == right["candidate"]["family"]:
            continue
        left_key = (
            left["instrument"],
            f"{left['candidate']['family']}_{left['candidate']['target_r']:g}R",
        )
        right_key = (
            right["instrument"],
            f"{right['candidate']['family']}_{right['candidate']['target_r']:g}R",
        )
        combined = combine_earliest_by_day(
            [trades_by_key[left_key], trades_by_key[right_key]]
        )
        block_metrics = {
            name: metrics(subset(combined, block_sessions))
            for name, block_sessions in splits.items()
        }
        portfolio_rows.append(
            {
                "instrument": left["instrument"],
                "components": [left["candidate"], right["candidate"]],
                **block_metrics,
                "overall": metrics(combined),
                "selection_passed": passes_block(
                    block_metrics["selection"], minimum_trades["selection"]
                ),
                "validation_passed": passes_block(
                    block_metrics["validation"], minimum_trades["validation"]
                ),
                "_trades": combined,
            }
        )
    best_portfolio = choose_best(portfolio_rows)

    finalists: list[tuple[str, dict[str, object], list[Trade]]] = []
    if best_individual:
        key = (
            best_individual["instrument"],
            f"{best_individual['candidate']['family']}_{best_individual['candidate']['target_r']:g}R",
        )
        finalists.append(("individual", best_individual, trades_by_key[key]))
    if best_portfolio:
        matching = next(
            row for row in portfolio_rows
            if row["instrument"] == best_portfolio["instrument"]
            and row["components"] == best_portfolio["components"]
        )
        finalists.append(("portfolio", best_portfolio, matching["_trades"]))

    finalist_results = []
    for kind, row, trades in finalists:
        gate = economic_gate(trades, sessions)
        finalist_results.append(
            {
                "kind": kind,
                "definition": {
                    key: value for key, value in row.items()
                    if key not in {"_trades"}
                },
                "economic_gate": gate,
                "confirmation_passed": passes_block(
                    row["confirmation"],
                    max(4, len(splits["confirmation"]) // 5),
                ),
                "trades": [serialize_trade(trade) for trade in trades],
            }
        )

    approved = [
        item for item in finalist_results
        if item["confirmation_passed"] and item["economic_gate"]["passed"]
    ]
    result = {
        "method": {
            "name": "RegimeTriggerWalkForwardV2",
            "candidate_families": sorted(FINDERS),
            "target_r_values": [1.0, 1.5, 2.0],
            "candidate_count_per_instrument": len(candidates()),
            "selection_rule": (
                "At least 20% active sessions (minimum 5/4), positive net and PF >= 1.10."
            ),
            "portfolio_rule": "At most two selection survivors; earliest signal per day.",
            "risk_currency_per_contract": {
                "minimum": base.MINIMUM_RISK_CURRENCY,
                "maximum": base.MAXIMUM_RISK_CURRENCY,
            },
            "round_turn_cost_currency": base.ROUND_TURN_COST,
        },
        "data_audit": {symbol: audit for symbol, _, audit in loaded},
        "sessions": {
            name: {
                "count": len(block),
                "from": block[0].isoformat(),
                "to": block[-1].isoformat(),
            }
            for name, block in splits.items()
        },
        "minimum_trades": minimum_trades,
        "candidate_results": rows,
        "selection_survivors": sum(row["selection_passed"] for row in rows),
        "viable_selection_components": len(component_survivors),
        "selection_and_validation_survivors": len(selected_rows),
        "portfolio_candidates": len(portfolio_rows),
        "portfolio_results": portfolio_rows,
        "portfolio_selection_and_validation_survivors": sum(
            row["selection_passed"] and row["validation_passed"]
            for row in portfolio_rows
        ),
        "finalists": finalist_results,
        "approved": bool(approved),
        "decision": (
            "Promote the approved finalist to prospective shadow validation."
            if approved
            else "Do not change the indicator; no finalist passed confirmation and the economic gate."
        ),
        "limitations": [
            "The historical confirmation block is not pristine because prior studies used this period.",
            "OHLCV does not contain bid/ask delta, imbalance or absorption.",
            "Only future data after 2026-07-29 can provide an untouched confirmation.",
            "A risk-scaled result does not establish future profitability.",
        ],
    }
    for row in portfolio_rows:
        row.pop("_trades", None)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(json.dumps({
        "output": str(args.output),
        "selection_survivors": result["selection_survivors"],
        "selection_and_validation_survivors": result[
            "selection_and_validation_survivors"
        ],
        "portfolio_candidates": result["portfolio_candidates"],
        "portfolio_selection_and_validation_survivors": result[
            "portfolio_selection_and_validation_survivors"
        ],
        "finalists": len(finalist_results),
        "approved": result["approved"],
    }, indent=2))


if __name__ == "__main__":
    main()
