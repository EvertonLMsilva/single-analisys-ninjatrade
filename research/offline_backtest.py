#!/usr/bin/env python3
"""Backtest offline reproduzível para candles OHLCV do Trade Assistant.

O script nunca acessa o NinjaTrader ou uma conta. Entradas são presumidas no fechamento
do candle do gatilho e avaliadas somente a partir do candle seguinte.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import itertools
import json
import math
import statistics
from collections import deque
from dataclasses import asdict, dataclass
from datetime import date, datetime, time, timedelta
from pathlib import Path
from typing import Callable, Iterable, Optional


TIME_FORMAT = "%Y-%m-%d %H:%M:%S"
ROUND_TURN_COST = 5.0
SENSITIVITY_COST = 3.0
HIGH_COST = 7.0
MAXIMUM_RISK = 50.0
MINIMUM_RISK = 5.0


@dataclass
class Bar:
    timestamp: datetime
    open: float
    high: float
    low: float
    close: float
    volume: float
    trading_day: date
    ema_fast: float = 0.0
    ema_slow: float = 0.0
    atr: float = 0.0
    vwap: float = 0.0
    previous_vwap: float = 0.0
    vwap_slope_atr: float = 0.0
    vwap_distance_atr: float = 0.0
    fast_slope_atr: float = 0.0
    slow_slope_atr: float = 0.0
    relative_volume: float = 0.0
    candle_body_atr: float = 0.0
    close_location: float = 0.5
    previous_session_high_12: Optional[float] = None
    previous_session_low_12: Optional[float] = None
    long_score: int = 0
    short_score: int = 0


@dataclass(frozen=True)
class Candidate:
    family: str
    direction: str
    minimum_score: int
    require_vwap_trend: bool
    maximum_vwap_distance_atr: float
    minimum_relative_volume: float
    time_window: str
    target_r: float
    valid_bars: int


@dataclass
class Trade:
    instrument: str
    candidate: Candidate
    signal_time: datetime
    exit_time: datetime
    trading_day: date
    entry: float
    stop: float
    target: float
    risk_currency: float
    status: str
    gross_currency: float
    net_currency: float
    exit_index: int


def trading_day_for(timestamp: datetime) -> date:
    rollover = time(19, 0) if is_us_daylight_saving(timestamp.date()) else time(20, 0)
    return timestamp.date() + timedelta(days=1) if timestamp.time() >= rollover else timestamp.date()


def is_us_daylight_saving(day: date) -> bool:
    march_first = date(day.year, 3, 1)
    first_sunday_march = march_first + timedelta(days=(6 - march_first.weekday()) % 7)
    second_sunday_march = first_sunday_march + timedelta(days=7)
    november_first = date(day.year, 11, 1)
    first_sunday_november = november_first + timedelta(
        days=(6 - november_first.weekday()) % 7
    )
    return second_sunday_march <= day < first_sunday_november


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for block in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def load_bars(path: Path) -> list[Bar]:
    bars: list[Bar] = []
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        expected = ["Time", "Open", "High", "Low", "Close", "Volume"]
        if reader.fieldnames != expected:
            raise ValueError(f"{path}: cabeçalho inválido: {reader.fieldnames}")
        previous: Optional[datetime] = None
        for line_number, row in enumerate(reader, start=2):
            timestamp = datetime.strptime(row["Time"], TIME_FORMAT)
            values = [float(row[name]) for name in ("Open", "High", "Low", "Close", "Volume")]
            open_price, high, low, close, volume = values
            if previous is not None and timestamp <= previous:
                raise ValueError(f"{path}: timestamp fora de ordem na linha {line_number}")
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
    return bars


def calculate_features(bars: list[Bar]) -> None:
    alpha_fast = 2.0 / 10.0
    alpha_slow = 2.0 / 22.0
    ema_fast = bars[0].close
    ema_slow = bars[0].close
    atr = bars[0].high - bars[0].low
    previous_close = bars[0].close
    volume_window: deque[float] = deque()
    volume_sum = 0.0
    session_day: Optional[date] = None
    price_volume_sum = 0.0
    session_volume = 0.0
    session_highs: deque[float] = deque(maxlen=12)
    session_lows: deque[float] = deque(maxlen=12)

    for index, bar in enumerate(bars):
        ema_fast = bar.close if index == 0 else alpha_fast * bar.close + (1 - alpha_fast) * ema_fast
        ema_slow = bar.close if index == 0 else alpha_slow * bar.close + (1 - alpha_slow) * ema_slow
        true_range = max(
            bar.high - bar.low,
            abs(bar.high - previous_close),
            abs(bar.low - previous_close),
        )
        atr = true_range if index == 0 else ((atr * 13.0) + true_range) / 14.0

        if bar.trading_day != session_day:
            session_day = bar.trading_day
            price_volume_sum = 0.0
            session_volume = 0.0
            session_highs.clear()
            session_lows.clear()

        bar.previous_session_high_12 = max(session_highs) if len(session_highs) == 12 else None
        bar.previous_session_low_12 = min(session_lows) if len(session_lows) == 12 else None
        previous_vwap = bars[index - 1].vwap if index > 0 and bars[index - 1].trading_day == bar.trading_day else 0.0
        typical_price = (bar.high + bar.low + bar.close) / 3.0
        price_volume_sum += typical_price * bar.volume
        session_volume += bar.volume
        vwap = price_volume_sum / session_volume
        if previous_vwap == 0:
            previous_vwap = vwap

        volume_window.append(bar.volume)
        volume_sum += bar.volume
        if len(volume_window) > 20:
            volume_sum -= volume_window.popleft()
        average_volume = volume_sum / len(volume_window)

        bar.ema_fast = ema_fast
        bar.ema_slow = ema_slow
        bar.atr = atr
        bar.previous_vwap = previous_vwap
        bar.vwap = vwap
        bar.vwap_slope_atr = (vwap - previous_vwap) / atr if atr > 0 else 0.0
        bar.vwap_distance_atr = abs(bar.close - vwap) / atr if atr > 0 else 0.0
        bar.relative_volume = bar.volume / average_volume if average_volume > 0 else 0.0
        bar.candle_body_atr = abs(bar.close - bar.open) / atr if atr > 0 else 0.0
        candle_range = bar.high - bar.low
        bar.close_location = (bar.close - bar.low) / candle_range if candle_range > 0 else 0.5
        if index >= 3:
            bar.fast_slope_atr = (bar.ema_fast - bars[index - 3].ema_fast) / atr
            bar.slow_slope_atr = (bar.ema_slow - bars[index - 3].ema_slow) / atr
        bar.long_score = context_score(bar, "Long")
        bar.short_score = context_score(bar, "Short")

        session_highs.append(bar.high)
        session_lows.append(bar.low)
        previous_close = bar.close


def context_score(bar: Bar, direction: str) -> int:
    return sum(context_checks(bar, direction).values())


def context_checks(bar: Bar, direction: str) -> dict[str, bool]:
    is_long = direction == "Long"
    return {
        "correct_vwap_side": bar.close > bar.vwap if is_long else bar.close < bar.vwap,
        "vwap_slope_aligned": bar.vwap_slope_atr > 0 if is_long else bar.vwap_slope_atr < 0,
        "ema_slopes_aligned": (
            bar.fast_slope_atr > 0 and bar.slow_slope_atr >= 0
            if is_long
            else bar.fast_slope_atr < 0 and bar.slow_slope_atr <= 0
        ),
        "strong_candle": bar.candle_body_atr >= 0.15
        and (bar.close_location >= 0.65 if is_long else bar.close_location <= 0.35),
        "within_1_25_atr_of_vwap": bar.vwap_distance_atr <= 1.25,
        "relative_volume_at_least_0_8": bar.relative_volume >= 0.8,
    }


def trigger_indices(bars: list[Bar], family: str, direction: str) -> list[int]:
    indices: list[int] = []
    last_pullback_trigger = -1_000_000
    is_long = direction == "Long"
    for index in range(24, len(bars)):
        bar = bars[index]
        previous = bars[index - 1]
        if bar.trading_day != previous.trading_day:
            continue
        if family == "Pullback":
            tolerance = bar.atr * 0.1
            trend = (
                bar.ema_fast > bar.ema_slow
                and bar.ema_fast > previous.ema_fast
                and bar.ema_slow >= previous.ema_slow
                if is_long
                else bar.ema_fast < bar.ema_slow
                and bar.ema_fast < previous.ema_fast
                and bar.ema_slow <= previous.ema_slow
            )
            confirmation = (
                bar.low <= bar.ema_fast + tolerance
                and bar.close > bar.ema_fast
                and bar.close > bar.open
                if is_long
                else bar.high >= bar.ema_fast - tolerance
                and bar.close < bar.ema_fast
                and bar.close < bar.open
            )
            triggered = trend and confirmation
        elif family == "VwapReclaim":
            triggered = (
                previous.close <= previous.vwap
                and bar.close > bar.vwap
                and bar.ema_fast > bar.ema_slow
                and bar.close > bar.open
                and bar.close_location >= 0.60
                if is_long
                else previous.close >= previous.vwap
                and bar.close < bar.vwap
                and bar.ema_fast < bar.ema_slow
                and bar.close < bar.open
                and bar.close_location <= 0.40
            )
        elif family == "SessionBreakout":
            reference = (
                bar.previous_session_high_12 if is_long else bar.previous_session_low_12
            )
            triggered = (
                reference is not None
                and (
                    bar.close > reference
                    and bar.ema_fast > bar.ema_slow
                    and bar.close > bar.open
                    if is_long
                    else bar.close < reference
                    and bar.ema_fast < bar.ema_slow
                    and bar.close < bar.open
                )
                and bar.candle_body_atr >= 0.20
            )
        else:
            raise ValueError(f"Família desconhecida: {family}")
        if triggered and (
            family != "Pullback" or index - last_pullback_trigger >= 3
        ):
            indices.append(index)
            if family == "Pullback":
                last_pullback_trigger = index
    return indices


def in_time_window(timestamp: datetime, window: str) -> bool:
    minutes = timestamp.hour * 60 + timestamp.minute
    if window == "All":
        return True
    regular_open = 10 * 60 + 30 if is_us_daylight_saving(timestamp.date()) else 11 * 60 + 30
    regular_close = regular_open + 6 * 60 + 30
    if window == "RTH":
        return regular_open <= minutes < regular_close
    if window == "Opening":
        return regular_open <= minutes < regular_open + 3 * 60
    raise ValueError(f"Janela desconhecida: {window}")


def candidate_accepts(bar: Bar, candidate: Candidate) -> bool:
    score = bar.long_score if candidate.direction == "Long" else bar.short_score
    if score < candidate.minimum_score:
        return False
    if bar.vwap_distance_atr > candidate.maximum_vwap_distance_atr:
        return False
    if bar.relative_volume < candidate.minimum_relative_volume:
        return False
    if not in_time_window(bar.timestamp, candidate.time_window):
        return False
    if candidate.require_vwap_trend:
        if candidate.direction == "Long":
            return bar.close > bar.vwap and bar.vwap_slope_atr > 0
        return bar.close < bar.vwap and bar.vwap_slope_atr < 0
    return True


def simulate_candidate(
    instrument: str,
    bars: list[Bar],
    candidate: Candidate,
    triggers: list[int],
    point_value: float,
    tick_size: float,
    excluded_sessions: set[date],
    round_turn_cost: float,
) -> list[Trade]:
    trades: list[Trade] = []
    blocked_until = -1
    is_long = candidate.direction == "Long"
    for signal_index in triggers:
        if signal_index <= blocked_until:
            continue
        bar = bars[signal_index]
        if bar.trading_day in excluded_sessions or not candidate_accepts(bar, candidate):
            continue
        entry = bar.close
        stop = bar.low - tick_size if is_long else bar.high + tick_size
        risk_points = abs(entry - stop)
        risk_currency = risk_points * point_value
        if not MINIMUM_RISK <= risk_currency <= MAXIMUM_RISK:
            continue
        target = (
            entry + risk_points * candidate.target_r
            if is_long
            else entry - risk_points * candidate.target_r
        )
        last_index = signal_index
        outcome = "Expired"
        gross = 0.0
        for offset in range(1, candidate.valid_bars + 1):
            index = signal_index + offset
            if index >= len(bars) or bars[index].trading_day != bar.trading_day:
                break
            evaluation = bars[index]
            last_index = index
            target_hit = evaluation.high >= target if is_long else evaluation.low <= target
            stop_hit = evaluation.low <= stop if is_long else evaluation.high >= stop
            if target_hit and stop_hit:
                outcome = "AmbiguousWorstCase"
                gross = -risk_currency
                break
            if stop_hit:
                outcome = "Stop"
                gross = -risk_currency
                break
            if target_hit:
                outcome = "Target"
                gross = risk_currency * candidate.target_r
                break
        else:
            last_index = min(signal_index + candidate.valid_bars, len(bars) - 1)
        if outcome == "Expired":
            exit_close = bars[last_index].close
            gross = (
                (exit_close - entry) * point_value
                if is_long
                else (entry - exit_close) * point_value
            )
        trades.append(
            Trade(
                instrument,
                candidate,
                bar.timestamp,
                bars[last_index].timestamp,
                bar.trading_day,
                entry,
                stop,
                target,
                risk_currency,
                outcome,
                gross,
                gross - round_turn_cost,
                last_index,
            )
        )
        blocked_until = last_index
    return trades


def metrics(trades: Iterable[Trade]) -> dict:
    ordered = sorted(trades, key=lambda trade: trade.signal_time)
    equity = 0.0
    peak = 0.0
    maximum_drawdown = 0.0
    gross_profit = 0.0
    gross_loss = 0.0
    daily: dict[date, float] = {}
    statuses: dict[str, int] = {}
    for trade in ordered:
        equity += trade.net_currency
        peak = max(peak, equity)
        maximum_drawdown = max(maximum_drawdown, peak - equity)
        if trade.net_currency > 0:
            gross_profit += trade.net_currency
        elif trade.net_currency < 0:
            gross_loss += -trade.net_currency
        daily[trade.trading_day] = daily.get(trade.trading_day, 0.0) + trade.net_currency
        statuses[trade.status] = statuses.get(trade.status, 0) + 1
    count = len(ordered)
    return {
        "trades": count,
        "net_currency": round(equity, 2),
        "expectancy_currency": round(equity / count, 2) if count else 0.0,
        "profit_factor": round(gross_profit / gross_loss, 3) if gross_loss else (99.0 if gross_profit else 0.0),
        "maximum_drawdown_currency": round(maximum_drawdown, 2),
        "positive_sessions": sum(value > 0 for value in daily.values()),
        "negative_sessions": sum(value < 0 for value in daily.values()),
        "sessions_with_trades": len(daily),
        "statuses": statuses,
    }


def monthly_metrics(trades: Iterable[Trade]) -> dict[str, dict]:
    grouped: dict[str, list[Trade]] = {}
    for trade in trades:
        month = trade.trading_day.strftime("%Y-%m")
        grouped.setdefault(month, []).append(trade)
    return {month: metrics(grouped[month]) for month in sorted(grouped)}


def risk_summary(trades: Iterable[Trade]) -> dict:
    risks = [trade.risk_currency for trade in trades]
    if not risks:
        return {"minimum": 0.0, "median": 0.0, "average": 0.0, "maximum": 0.0}
    return {
        "minimum": round(min(risks), 2),
        "median": round(statistics.median(risks), 2),
        "average": round(statistics.mean(risks), 2),
        "maximum": round(max(risks), 2),
    }


def context_criterion_summary(
    trades: Iterable[Trade], bars: list[Bar], direction: str
) -> dict[str, dict]:
    bars_by_timestamp = {bar.timestamp: bar for bar in bars}
    trade_list = list(trades)
    totals: dict[str, int] = {}
    for trade in trade_list:
        for name, passed in context_checks(
            bars_by_timestamp[trade.signal_time], direction
        ).items():
            totals[name] = totals.get(name, 0) + int(passed)
    count = len(trade_list)
    return {
        name: {
            "passed": passed,
            "total": count,
            "rate_percent": round(passed / count * 100.0, 2) if count else 0.0,
        }
        for name, passed in totals.items()
    }


def split_sessions(sessions: list[date]) -> tuple[set[date], set[date], set[date]]:
    if len(sessions) < 15:
        raise ValueError("São necessárias pelo menos 15 sessões válidas.")
    train_end = max(1, int(len(sessions) * 0.60))
    validation_end = max(train_end + 1, int(len(sessions) * 0.80))
    return (
        set(sessions[:train_end]),
        set(sessions[train_end:validation_end]),
        set(sessions[validation_end:]),
    )


def candidate_grid(direction: str) -> Iterable[Candidate]:
    return (
        Candidate(*values)
        for values in itertools.product(
            ("Pullback", "VwapReclaim", "SessionBreakout"),
            (direction,),
            (0, 3, 4, 5),
            (False, True),
            (1.25, 2.0, 4.0),
            (0.0, 0.8, 1.0),
            ("All", "RTH", "Opening"),
            (1.0, 1.5),
            (3, 6, 12),
        )
    )


def subset(trades: list[Trade], sessions: set[date]) -> list[Trade]:
    return [trade for trade in trades if trade.trading_day in sessions]


def select_candidate(
    instrument: str,
    bars: list[Bar],
    direction: str,
    point_value: float,
    tick_size: float,
    excluded_sessions: set[date],
    session_splits: tuple[set[date], set[date], set[date]],
) -> tuple[Optional[Candidate], dict, int, int]:
    train_sessions, validation_sessions, test_sessions = session_splits
    triggers_by_family = {
        family: trigger_indices(bars, family, direction)
        for family in ("Pullback", "VwapReclaim", "SessionBreakout")
    }
    evaluated = 0
    survivors = 0
    ranked: list[tuple[float, Candidate, list[Trade], dict, dict]] = []
    for candidate in candidate_grid(direction):
        evaluated += 1
        trades = simulate_candidate(
            instrument,
            bars,
            candidate,
            triggers_by_family[candidate.family],
            point_value,
            tick_size,
            excluded_sessions,
            ROUND_TURN_COST,
        )
        train_metrics = metrics(subset(trades, train_sessions))
        validation_metrics = metrics(subset(trades, validation_sessions))
        if (
            train_metrics["trades"] < 15
            or validation_metrics["trades"] < 5
            or train_metrics["net_currency"] <= 0
            or validation_metrics["net_currency"] <= 0
            or train_metrics["profit_factor"] < 1.10
            or validation_metrics["profit_factor"] < 1.10
        ):
            continue
        survivors += 1
        robust_expectancy = min(
            train_metrics["expectancy_currency"],
            validation_metrics["expectancy_currency"],
        )
        rank = (
            robust_expectancy * 10.0
            + (train_metrics["expectancy_currency"] + validation_metrics["expectancy_currency"])
            - 0.02
            * max(
                train_metrics["maximum_drawdown_currency"],
                validation_metrics["maximum_drawdown_currency"],
            )
        )
        ranked.append((rank, candidate, trades, train_metrics, validation_metrics))
    if not ranked:
        return None, {"train": metrics([]), "validation": metrics([]), "test": metrics([]), "all": metrics([])}, evaluated, survivors
    ranked.sort(key=lambda item: item[0], reverse=True)
    _, selected, trades, train_metrics, validation_metrics = ranked[0]
    test_metrics = metrics(subset(trades, test_sessions))
    all_metrics = metrics(trades)
    sensitivity_trades = simulate_candidate(
        instrument,
        bars,
        selected,
        triggers_by_family[selected.family],
        point_value,
        tick_size,
        excluded_sessions,
        SENSITIVITY_COST,
    )
    high_cost_trades = simulate_candidate(
        instrument,
        bars,
        selected,
        triggers_by_family[selected.family],
        point_value,
        tick_size,
        excluded_sessions,
        HIGH_COST,
    )
    survivor_test_metrics = [
        metrics(subset(item[2], test_sessions))
        for item in ranked
    ]
    survivor_test_nets = [
        item["net_currency"] for item in survivor_test_metrics
    ]
    result = {
        "train": train_metrics,
        "validation": validation_metrics,
        "test": test_metrics,
        "all": all_metrics,
        "all_at_3_cost": metrics(sensitivity_trades),
        "all_at_7_cost": metrics(high_cost_trades),
        "monthly": monthly_metrics(trades),
        "risk_currency": risk_summary(trades),
        "context_criteria": context_criterion_summary(
            trades, bars, selected.direction
        ),
        "survivor_test_robustness": {
            "pretest_survivors": len(survivor_test_metrics),
            "positive_in_test": sum(
                item["net_currency"] > 0 for item in survivor_test_metrics
            ),
            "passed_test_gate": sum(
                item["trades"] >= 4
                and item["net_currency"] > 0
                and item["profit_factor"] > 1.0
                for item in survivor_test_metrics
            ),
            "median_test_net_currency": round(
                statistics.median(survivor_test_nets), 2
            ),
            "minimum_test_net_currency": round(min(survivor_test_nets), 2),
            "maximum_test_net_currency": round(max(survivor_test_nets), 2),
        },
        "test_passed": (
            test_metrics["trades"] >= 4
            and test_metrics["net_currency"] > 0
            and test_metrics["profit_factor"] > 1.0
        ),
    }
    return selected, result, evaluated, survivors


def invalid_sessions_from_alignment(
    mnq_bars: list[Bar], mes_bars: list[Bar], tolerance: int = 3
) -> tuple[set[date], set[date], dict]:
    mnq_times = {bar.timestamp for bar in mnq_bars}
    mes_times = {bar.timestamp for bar in mes_bars}
    missing_mnq: dict[date, int] = {}
    missing_mes: dict[date, int] = {}
    for timestamp in mes_times - mnq_times:
        day = trading_day_for(timestamp)
        missing_mnq[day] = missing_mnq.get(day, 0) + 1
    for timestamp in mnq_times - mes_times:
        day = trading_day_for(timestamp)
        missing_mes[day] = missing_mes.get(day, 0) + 1
    return (
        {day for day, count in missing_mnq.items() if count > tolerance},
        {day for day, count in missing_mes.items() if count > tolerance},
        {
            "common_timestamps": len(mnq_times & mes_times),
            "mnq_only": len(mnq_times - mes_times),
            "mes_only": len(mes_times - mnq_times),
            "mnq_excluded_sessions": sorted(day.isoformat() for day, count in missing_mnq.items() if count > tolerance),
            "mes_excluded_sessions": sorted(day.isoformat() for day, count in missing_mes.items() if count > tolerance),
        },
    )


def serialize_candidate(candidate: Optional[Candidate]) -> Optional[dict]:
    if candidate is None:
        return None
    data = asdict(candidate)
    if math.isinf(data["maximum_vwap_distance_atr"]):
        data["maximum_vwap_distance_atr"] = "Infinity"
    return data


def run_backtest(mnq_path: Path, mes_path: Path) -> dict:
    mnq_bars = load_bars(mnq_path)
    mes_bars = load_bars(mes_path)
    calculate_features(mnq_bars)
    calculate_features(mes_bars)
    mnq_excluded, mes_excluded, alignment = invalid_sessions_from_alignment(mnq_bars, mes_bars)
    instruments = [
        ("MNQ", mnq_bars, 2.0, mnq_excluded, mnq_path),
        ("MES", mes_bars, 5.0, mes_excluded, mes_path),
    ]
    output = {
        "methodology": {
            "selection_cost_per_round_turn": ROUND_TURN_COST,
            "sensitivity_cost_per_round_turn": SENSITIVITY_COST,
            "high_cost_per_round_turn": HIGH_COST,
            "maximum_risk_per_contract": MAXIMUM_RISK,
            "minimum_risk_per_contract": MINIMUM_RISK,
            "entry_assumption": "Fechamento do candle do gatilho",
            "first_evaluated_bar": "Candle seguinte",
            "same_bar_target_and_stop": "Pior caso: stop",
            "expiry": "Saída no fechamento do último candle válido",
            "split": "60% seleção, 20% validação, 20% teste final por sessão",
        },
        "alignment": alignment,
        "datasets": {},
        "frozen_baseline": {},
        "selections": [],
    }
    for instrument, bars, point_value, excluded, path in instruments:
        sessions = sorted({bar.trading_day for bar in bars} - excluded)
        splits = split_sessions(sessions)
        output["datasets"][instrument] = {
            "path": path.name,
            "sha256": file_sha256(path),
            "bars": len(bars),
            "valid_sessions": len(sessions),
            "excluded_sessions": sorted(day.isoformat() for day in excluded),
            "train_sessions": len(splits[0]),
            "validation_sessions": len(splits[1]),
            "test_sessions": len(splits[2]),
            "train_range": [min(splits[0]).isoformat(), max(splits[0]).isoformat()],
            "validation_range": [min(splits[1]).isoformat(), max(splits[1]).isoformat()],
            "test_range": [min(splits[2]).isoformat(), max(splits[2]).isoformat()],
        }
        if instrument == "MNQ":
            frozen_candidate = Candidate(
                "Pullback",
                "Short",
                4,
                True,
                math.inf,
                0.0,
                "All",
                1.0,
                3,
            )
            frozen_trades = simulate_candidate(
                instrument,
                bars,
                frozen_candidate,
                trigger_indices(bars, "Pullback", "Short"),
                point_value,
                0.25,
                excluded,
                ROUND_TURN_COST,
            )
            output["frozen_baseline"] = {
                "candidate": serialize_candidate(frozen_candidate),
                "train": metrics(subset(frozen_trades, splits[0])),
                "validation": metrics(subset(frozen_trades, splits[1])),
                "test": metrics(subset(frozen_trades, splits[2])),
                "all": metrics(frozen_trades),
            }
        for direction in ("Long", "Short"):
            candidate, result, evaluated, survivors = select_candidate(
                instrument,
                bars,
                direction,
                point_value,
                0.25,
                excluded,
                splits,
            )
            output["selections"].append(
                {
                    "instrument": instrument,
                    "direction": direction,
                    "candidate": serialize_candidate(candidate),
                    "evaluated_candidates": evaluated,
                    "selection_validation_survivors": survivors,
                    "metrics": result,
                }
            )
    return output


def run_self_tests() -> None:
    assert not is_us_daylight_saving(date(2026, 3, 7))
    assert is_us_daylight_saving(date(2026, 3, 8))
    assert is_us_daylight_saving(date(2026, 10, 31))
    assert not is_us_daylight_saving(date(2026, 11, 1))
    assert trading_day_for(datetime(2026, 3, 2, 19, 0)).isoformat() == "2026-03-02"
    assert trading_day_for(datetime(2026, 3, 2, 20, 5)).isoformat() == "2026-03-03"
    assert trading_day_for(datetime(2026, 3, 8, 19, 5)).isoformat() == "2026-03-09"
    assert trading_day_for(datetime(2026, 11, 1, 19, 5)).isoformat() == "2026-11-01"
    assert trading_day_for(datetime(2026, 11, 1, 20, 5)).isoformat() == "2026-11-02"
    assert trading_day_for(datetime(2026, 7, 29, 18, 0)).isoformat() == "2026-07-29"
    assert trading_day_for(datetime(2026, 7, 29, 19, 5)).isoformat() == "2026-07-30"
    assert in_time_window(datetime(2026, 3, 6, 11, 30), "RTH")
    assert not in_time_window(datetime(2026, 3, 6, 10, 30), "RTH")
    assert in_time_window(datetime(2026, 3, 9, 10, 30), "RTH")
    candidate = Candidate("Pullback", "Long", 0, False, 4.0, 0.0, "All", 1.0, 3)
    day = date(2026, 7, 1)
    bars = [
        Bar(datetime(2026, 7, 1, 10, 0) + timedelta(minutes=5 * index), 100, 101, 99, 100, 100, day)
        for index in range(5)
    ]
    bars[0].low = 99.0
    bars[1].high = 102.0
    bars[1].low = 98.0
    for bar in bars:
        bar.atr = 1
        bar.vwap = 100
        bar.relative_volume = 1
    trade = simulate_candidate("TEST", bars, candidate, [0], 5.0, 1.0, set(), 0.0)[0]
    assert trade.status == "AmbiguousWorstCase"
    assert trade.gross_currency == -10.0

    one_bar_candidate = Candidate("Pullback", "Long", 0, False, 4.0, 0.0, "All", 1.0, 1)
    cases = (
        (102.0, 99.0, 101.0, "Target", 10.0),
        (101.0, 98.0, 99.0, "Stop", -10.0),
        (101.0, 99.0, 101.0, "Expired", 5.0),
    )
    for high, low, close, expected_status, expected_gross in cases:
        case_bars = [
            Bar(datetime(2026, 7, 1, 10, 0), 100, 101, 99, 100, 100, day),
            Bar(datetime(2026, 7, 1, 10, 5), 100, high, low, close, 100, day),
        ]
        for case_bar in case_bars:
            case_bar.atr = 1
            case_bar.vwap = 100
            case_bar.relative_volume = 1
        case_trade = simulate_candidate(
            "TEST", case_bars, one_bar_candidate, [0], 5.0, 1.0, set(), 0.0
        )[0]
        assert case_trade.status == expected_status, case_trade
        assert case_trade.gross_currency == expected_gross, case_trade


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mnq", type=Path, required=True)
    parser.add_argument("--mes", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        run_self_tests()
    result = run_backtest(args.mnq, args.mes)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding="utf-8")
    print(json.dumps(result, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
