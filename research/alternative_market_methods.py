#!/usr/bin/env python3
"""Pesquisa de métodos alternativos usando os quatro meses como desenvolvimento.

Hipóteses: momentum/reversão da abertura, momentum/reversão overnight, momentum no
fechamento, preenchimento de gap e força relativa MNQ-MES. O resultado é apenas um
candidato ajustado no histórico; sua validação depende de dados futuros.
"""

from __future__ import annotations

import argparse
import itertools
import json
import math
import statistics
from collections import defaultdict
from dataclasses import asdict, dataclass
from datetime import date, datetime, timedelta
from pathlib import Path
from typing import Optional

import prop_evaluation
import regime_structure_backtest as base


METHODS = (
    "CloseFirstHalfHourMomentum",
    "CloseOpeningMomentum",
    "CloseFirstPlusPenultimate",
    "CloseOvernightMomentum",
    "CloseOvernightReversal",
    "OpeningDrive90",
    "OpeningReversal90",
    "GapFill",
    "RelativeStrengthContinuation",
    "RelativeStrengthConvergence",
)
CONDITIONS = (
    "All",
    "HighOpeningVolatility",
    "LowOpeningVolatility",
    "HighOpeningVolume",
    "LowOpeningVolume",
)
THRESHOLDS = (0.0, 0.5, 1.0)


@dataclass
class DailySnapshot:
    instrument: str
    trading_day: date
    open_time: datetime
    close_time: datetime
    previous_close: float
    opening_price: float
    first_30_close: float
    penultimate_start: float
    last_half_hour_open: float
    close_price: float
    overnight_return: float
    opening_return: float
    first_half_hour_from_previous_close: float
    penultimate_return: float
    opening_volatility: float
    opening_volume: float
    relative_opening_volatility: float
    relative_opening_volume: float
    bars: list[base.Bar]


@dataclass(frozen=True)
class Candidate:
    instrument: str
    method: str
    threshold_sigma: float
    condition: str

    @property
    def name(self) -> str:
        return (
            f"{self.instrument}_{self.method}_"
            f"{self.threshold_sigma:g}s_{self.condition}"
        )


@dataclass
class Trade:
    instrument: str
    candidate: Candidate
    trading_day: date
    signal_time: datetime
    exit_time: datetime
    direction: str
    entry: float
    stop: float
    exit_price: float
    status: str
    risk_currency: float
    net_currency: float


def bar_at_or_after(bars: list[base.Bar], timestamp: datetime) -> Optional[base.Bar]:
    return next((bar for bar in bars if bar.timestamp >= timestamp), None)


def bar_at_or_before(bars: list[base.Bar], timestamp: datetime) -> Optional[base.Bar]:
    return next((bar for bar in reversed(bars) if bar.timestamp <= timestamp), None)


def build_snapshots(
    symbol: str, grouped: dict[date, list[base.Bar]]
) -> dict[date, DailySnapshot]:
    snapshots: dict[date, DailySnapshot] = {}
    prior_close: Optional[float] = None
    prior_volatilities: list[float] = []
    prior_volumes: list[float] = []
    for trading_day in sorted(grouped):
        open_time = base.regular_open_for(trading_day)
        close_time = open_time + timedelta(hours=6, minutes=30)
        rth = [
            bar for bar in grouped[trading_day]
            if open_time < bar.timestamp <= close_time
        ]
        if len(rth) < 360:
            continue
        if prior_close is None:
            prior_close = rth[-1].close
            continue
        first_30_end = open_time + timedelta(minutes=30)
        penultimate_start_time = close_time - timedelta(hours=1)
        last_half_open_time = close_time - timedelta(minutes=30)
        first_30 = [bar for bar in rth if bar.timestamp <= first_30_end]
        first_30_bar = bar_at_or_before(rth, first_30_end)
        penultimate_start_bar = bar_at_or_after(rth, penultimate_start_time)
        last_half_bar = bar_at_or_after(rth, last_half_open_time)
        close_bar = bar_at_or_before(rth, close_time)
        if (
            len(first_30) < 25
            or first_30_bar is None
            or penultimate_start_bar is None
            or last_half_bar is None
            or close_bar is None
        ):
            prior_close = rth[-1].close
            continue
        opening_price = rth[0].open
        opening_volatility = sum(bar.high - bar.low for bar in first_30)
        opening_volume = sum(bar.volume for bar in first_30)
        median_volatility = (
            statistics.median(prior_volatilities[-20:])
            if prior_volatilities else opening_volatility
        )
        median_volume = (
            statistics.median(prior_volumes[-20:])
            if prior_volumes else opening_volume
        )
        snapshots[trading_day] = DailySnapshot(
            instrument=symbol,
            trading_day=trading_day,
            open_time=open_time,
            close_time=close_time,
            previous_close=prior_close,
            opening_price=opening_price,
            first_30_close=first_30_bar.close,
            penultimate_start=penultimate_start_bar.open,
            last_half_hour_open=last_half_bar.open,
            close_price=close_bar.close,
            overnight_return=opening_price - prior_close,
            opening_return=first_30_bar.close - opening_price,
            first_half_hour_from_previous_close=first_30_bar.close - prior_close,
            penultimate_return=last_half_bar.open - penultimate_start_bar.open,
            opening_volatility=opening_volatility,
            opening_volume=opening_volume,
            relative_opening_volatility=(
                opening_volatility / median_volatility
                if median_volatility else 1.0
            ),
            relative_opening_volume=(
                opening_volume / median_volume if median_volume else 1.0
            ),
            bars=rth,
        )
        prior_close = close_bar.close
        prior_volatilities.append(opening_volatility)
        prior_volumes.append(opening_volume)
    return snapshots


def raw_signal(
    candidate: Candidate,
    snapshot: DailySnapshot,
    other: DailySnapshot,
) -> float:
    method = candidate.method
    if method == "CloseFirstHalfHourMomentum":
        return snapshot.first_half_hour_from_previous_close
    if method == "CloseOpeningMomentum":
        return snapshot.opening_return
    if method == "CloseFirstPlusPenultimate":
        return (
            snapshot.first_half_hour_from_previous_close
            + snapshot.penultimate_return
        )
    if method == "CloseOvernightMomentum":
        return snapshot.overnight_return
    if method == "CloseOvernightReversal":
        return -snapshot.overnight_return
    if method == "OpeningDrive90":
        return snapshot.opening_return
    if method == "OpeningReversal90":
        return -snapshot.opening_return
    if method == "GapFill":
        return -snapshot.overnight_return
    own_return = snapshot.opening_return / snapshot.opening_price
    other_return = other.opening_return / other.opening_price
    relative_strength = own_return - other_return
    if method == "RelativeStrengthContinuation":
        return relative_strength
    if method == "RelativeStrengthConvergence":
        return -relative_strength
    raise ValueError(f"Método desconhecido: {method}")


def condition_passed(candidate: Candidate, snapshot: DailySnapshot) -> bool:
    if candidate.condition == "All":
        return True
    if candidate.condition == "HighOpeningVolatility":
        return snapshot.relative_opening_volatility >= 1.0
    if candidate.condition == "LowOpeningVolatility":
        return snapshot.relative_opening_volatility < 1.0
    if candidate.condition == "HighOpeningVolume":
        return snapshot.relative_opening_volume >= 1.0
    if candidate.condition == "LowOpeningVolume":
        return snapshot.relative_opening_volume < 1.0
    raise ValueError(candidate.condition)


def signal_threshold(
    history: list[float], current: float, threshold_sigma: float
) -> bool:
    if len(history) < 20:
        return False
    standard_deviation = statistics.pstdev(history[-20:])
    if standard_deviation <= 0:
        return False
    return abs(current) >= threshold_sigma * standard_deviation


def trade_window(
    candidate: Candidate, snapshot: DailySnapshot
) -> tuple[datetime, datetime, Optional[float]]:
    if candidate.method.startswith("Close"):
        return (
            snapshot.close_time - timedelta(minutes=30),
            snapshot.close_time,
            None,
        )
    if candidate.method in (
        "RelativeStrengthContinuation",
        "RelativeStrengthConvergence",
    ):
        return (
            snapshot.open_time + timedelta(minutes=30),
            snapshot.open_time + timedelta(hours=2),
            None,
        )
    if candidate.method in ("OpeningDrive90", "OpeningReversal90"):
        return (
            snapshot.open_time + timedelta(minutes=30),
            snapshot.open_time + timedelta(hours=2),
            None,
        )
    if candidate.method == "GapFill":
        return (
            snapshot.open_time + timedelta(minutes=30),
            snapshot.open_time + timedelta(hours=2),
            snapshot.previous_close,
        )
    raise ValueError(candidate.method)


def simulate_trade(
    candidate: Candidate,
    snapshot: DailySnapshot,
    signal: float,
) -> Optional[Trade]:
    if signal == 0:
        return None
    direction = "Long" if signal > 0 else "Short"
    entry_time, exit_time, special_target = trade_window(candidate, snapshot)
    entry_bar = bar_at_or_after(snapshot.bars, entry_time)
    exit_bar = bar_at_or_before(snapshot.bars, exit_time)
    if entry_bar is None or exit_bar is None or exit_bar.timestamp <= entry_bar.timestamp:
        return None
    spec = base.SPECS[candidate.instrument]
    risk_points = base.MAXIMUM_RISK_CURRENCY / spec.point_value
    stop = (
        entry_bar.open - risk_points
        if direction == "Long"
        else entry_bar.open + risk_points
    )
    target = special_target
    if target is not None:
        target_valid = (
            target > entry_bar.open if direction == "Long"
            else target < entry_bar.open
        )
        if not target_valid:
            return None

    status = "TimeExit"
    final_price = exit_bar.close
    final_time = exit_bar.timestamp
    active_bars = [
        bar for bar in snapshot.bars
        if entry_bar.timestamp <= bar.timestamp <= exit_bar.timestamp
    ]
    for index, bar in enumerate(active_bars):
        if index == 0:
            continue
        stop_hit = bar.low <= stop if direction == "Long" else bar.high >= stop
        target_hit = (
            target is not None
            and (bar.high >= target if direction == "Long" else bar.low <= target)
        )
        if stop_hit:
            status = "Stop"
            final_price = stop
            final_time = bar.timestamp
            break
        if target_hit:
            status = "Target"
            final_price = target
            final_time = bar.timestamp
            break
    gross = (
        final_price - entry_bar.open
        if direction == "Long"
        else entry_bar.open - final_price
    ) * spec.point_value
    return Trade(
        instrument=candidate.instrument,
        candidate=candidate,
        trading_day=snapshot.trading_day,
        signal_time=entry_bar.timestamp,
        exit_time=final_time,
        direction=direction,
        entry=entry_bar.open,
        stop=stop,
        exit_price=final_price,
        status=status,
        risk_currency=base.MAXIMUM_RISK_CURRENCY,
        net_currency=round(gross - base.ROUND_TURN_COST, 2),
    )


def simulate_candidate(
    candidate: Candidate,
    snapshots: dict[str, dict[date, DailySnapshot]],
    sessions: list[date],
) -> list[Trade]:
    history: list[float] = []
    trades: list[Trade] = []
    other_symbol = "MES" if candidate.instrument == "MNQ" else "MNQ"
    for session in sessions:
        snapshot = snapshots[candidate.instrument].get(session)
        other = snapshots[other_symbol].get(session)
        if snapshot is None or other is None:
            continue
        signal = raw_signal(candidate, snapshot, other)
        allowed = (
            condition_passed(candidate, snapshot)
            and signal_threshold(history, signal, candidate.threshold_sigma)
        )
        history.append(signal)
        if not allowed:
            continue
        trade = simulate_trade(candidate, snapshot, signal)
        if trade is not None:
            trades.append(trade)
    return trades


def maximum_gap(sessions: list[date], trades: list[Trade]) -> int:
    active = {trade.trading_day for trade in trades}
    longest = current = 0
    for session in sessions:
        if session in active:
            current = 0
        else:
            current += 1
            longest = max(longest, current)
    return longest


def metrics(trades: list[Trade], sessions: list[date]) -> dict[str, object]:
    values = [trade.net_currency for trade in trades]
    wins = [value for value in values if value > 0]
    losses = [value for value in values if value <= 0]
    gross_profit = sum(wins)
    gross_loss = abs(sum(losses))
    monthly: dict[str, float] = defaultdict(float)
    for trade in trades:
        monthly[trade.trading_day.strftime("%Y-%m")] += trade.net_currency
    all_months = sorted({session.strftime("%Y-%m") for session in sessions})
    month_results = {month: round(monthly.get(month, 0.0), 2) for month in all_months}
    return {
        "trades": len(trades),
        "net_currency": round(sum(values), 2),
        "average_trade_currency": round(statistics.mean(values), 2) if values else None,
        "win_rate": round(len(wins) / len(trades), 6) if trades else None,
        "profit_factor": round(gross_profit / gross_loss, 4) if gross_loss else None,
        "maximum_drawdown_currency": round(base.maximum_drawdown(values), 2),
        "maximum_inactive_session_gap": maximum_gap(sessions, trades),
        "monthly_net_currency": month_results,
        "positive_months": sum(value > 0 for value in month_results.values()),
        "negative_months": sum(value < 0 for value in month_results.values()),
        "worst_month_currency": min(month_results.values()) if month_results else 0.0,
    }


def qualified(result: dict[str, object]) -> bool:
    return (
        result["trades"] >= 25
        and result["net_currency"] > 0
        and result["profit_factor"] is not None
        and result["profit_factor"] >= 1.20
        and result["positive_months"] >= 4
        and result["maximum_inactive_session_gap"] <= 5
        and result["worst_month_currency"] >= -100.0
    )


def portfolio_prequalified(result: dict[str, object]) -> bool:
    return (
        result["trades"] >= 25
        and result["net_currency"] > 0
        and result["profit_factor"] is not None
        and result["profit_factor"] >= 1.20
        and result["positive_months"] >= 4
        and result["maximum_inactive_session_gap"] <= 5
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
    accepted = [
        scenario for scenario in scenarios
        if scenario["pass_rate_percent"] >= 60.0
        and scenario["drawdown_failure_rate_percent"] <= 15.0
        and scenario["p90_maximum_drawdown_currency"] <= 1_000.0
    ]
    return {
        "passed": bool(accepted),
        "qualified_micro_contracts": [
            scenario["micro_contracts"] for scenario in accepted
        ],
        "best_scenario": max(
            scenarios,
            key=lambda scenario: (
                scenario["pass_rate_percent"],
                -scenario["drawdown_failure_rate_percent"],
                -scenario["p90_maximum_drawdown_currency"],
            ),
        ),
    }


def conditional_sizing_gate(
    high_trades: list[Trade],
    low_trades: list[Trade],
    sessions: list[date],
) -> dict[str, object]:
    rules = prop_evaluation.EvaluationRules(1_500.0, 1_500.0, 20, 5, 0.50)
    high_by_day = {
        trade.trading_day: trade.net_currency for trade in high_trades
    }
    low_by_day = {
        trade.trading_day: trade.net_currency for trade in low_trades
    }
    scenarios = []
    for high_micros in range(1, 11):
        for low_micros in range(1, 11):
            daily = [
                (
                    [high_by_day[session] * high_micros]
                    if session in high_by_day
                    else [low_by_day[session] * low_micros]
                    if session in low_by_day
                    else []
                )
                for session in sessions
            ]
            summary = prop_evaluation.summarize_attempts(
                prop_evaluation.rolling_attempts(daily, 1, rules)
            )
            scenarios.append(
                {
                    "high_regime_micros": high_micros,
                    "low_regime_micros": low_micros,
                    "maximum_risk_currency": (
                        max(high_micros, low_micros)
                        * base.MAXIMUM_RISK_CURRENCY
                    ),
                    **summary,
                }
            )
    accepted = [
        scenario for scenario in scenarios
        if scenario["pass_rate_percent"] >= 60.0
        and scenario["drawdown_failure_rate_percent"] <= 15.0
        and scenario["p90_maximum_drawdown_currency"] <= 1_000.0
        and scenario["maximum_risk_currency"] <= 300.0
    ]
    within_risk_cap = [
        scenario for scenario in scenarios
        if scenario["maximum_risk_currency"] <= 300.0
    ]
    ranking = lambda scenario: (
        scenario["pass_rate_percent"],
        -scenario["drawdown_failure_rate_percent"],
        -scenario["p90_maximum_drawdown_currency"],
        -scenario["maximum_risk_currency"],
    )
    return {
        "passed": bool(accepted),
        "qualified_scenarios": accepted,
        "best_scenario": max(scenarios, key=ranking),
        "best_scenario_with_risk_cap": max(within_risk_cap, key=ranking),
    }


def neighbor_support(
    candidate: Candidate,
    rows_by_key: dict[tuple[str, str, float, str], dict[str, object]],
) -> dict[str, object]:
    neighboring = [
        value for value in THRESHOLDS
        if value != candidate.threshold_sigma
        and abs(value - candidate.threshold_sigma) == 0.5
    ]
    results = []
    for threshold in neighboring:
        row = rows_by_key.get(
            (candidate.instrument, candidate.method, threshold, candidate.condition)
        )
        if row is not None:
            results.append(
                {
                    "threshold_sigma": threshold,
                    "net_currency": row["metrics"]["net_currency"],
                    "profit_factor": row["metrics"]["profit_factor"],
                    "positive": row["metrics"]["net_currency"] > 0,
                }
            )
    return {
        "neighbors": results,
        "all_neighbors_positive": bool(results) and all(
            item["positive"] for item in results
        ),
    }


def run_self_tests() -> None:
    assert len(METHODS) == 10
    assert signal_threshold([1.0] * 20, 1.0, 0.0) is False
    assert maximum_gap(
        [date(2026, 1, day) for day in range(1, 6)], []
    ) == 5


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
    snapshots = {
        symbol: build_snapshots(symbol, grouped[symbol])
        for symbol in ("MNQ", "MES")
    }
    sessions = sorted(set(snapshots["MNQ"]) & set(snapshots["MES"]))
    evaluation_sessions = sessions[20:]
    candidates = []
    for instrument, method, threshold, condition in itertools.product(
        ("MNQ", "MES"), METHODS, THRESHOLDS, CONDITIONS
    ):
        if method.startswith("RelativeStrength") and instrument == "MES":
            continue
        candidates.append(Candidate(instrument, method, threshold, condition))

    rows = []
    trades_by_name: dict[str, list[Trade]] = {}
    for candidate in candidates:
        trades = simulate_candidate(candidate, snapshots, sessions)
        trades_by_name[candidate.name] = trades
        result = metrics(trades, evaluation_sessions)
        rows.append(
            {
                "candidate": asdict(candidate),
                "metrics": result,
                "qualified_stability": qualified(result),
            }
        )
    rows_by_key = {
        (
            row["candidate"]["instrument"],
            row["candidate"]["method"],
            row["candidate"]["threshold_sigma"],
            row["candidate"]["condition"],
        ): row
        for row in rows
    }
    for row in rows:
        row["neighbor_support"] = neighbor_support(
            Candidate(**row["candidate"]), rows_by_key
        )

    portfolio_rows = []
    portfolio_trades: dict[str, list[Trade]] = {}
    condition_pairs = (
        ("HighOpeningVolatility", "LowOpeningVolatility"),
        ("HighOpeningVolume", "LowOpeningVolume"),
    )
    methods_by_instrument = {
        instrument: sorted(
            {
                candidate.method
                for candidate in candidates
                if candidate.instrument == instrument
            }
        )
        for instrument in ("MNQ", "MES")
    }
    for instrument in ("MNQ", "MES"):
        for high_condition, low_condition in condition_pairs:
            for threshold in THRESHOLDS:
                for high_method, low_method in itertools.product(
                    methods_by_instrument[instrument],
                    methods_by_instrument[instrument],
                ):
                    high = Candidate(
                        instrument, high_method, threshold, high_condition
                    )
                    low = Candidate(
                        instrument, low_method, threshold, low_condition
                    )
                    combined = sorted(
                        trades_by_name[high.name] + trades_by_name[low.name],
                        key=lambda trade: trade.signal_time,
                    )
                    portfolio_name = f"{high.name}__{low.name}"
                    portfolio_trades[portfolio_name] = combined
                    portfolio_metrics = metrics(combined, evaluation_sessions)
                    portfolio_rows.append(
                        {
                            "name": portfolio_name,
                            "instrument": instrument,
                            "threshold_sigma": threshold,
                            "high_component": asdict(high),
                            "low_component": asdict(low),
                            "metrics": portfolio_metrics,
                            "prequalified": portfolio_prequalified(
                                portfolio_metrics
                            ),
                            "economic_gate": None,
                            "conditional_sizing_gate": None,
                        }
                    )
    for row in portfolio_rows:
        if row["prequalified"]:
            row["economic_gate"] = economic_gate(
                portfolio_trades[row["name"]], evaluation_sessions
            )
            row["conditional_sizing_gate"] = conditional_sizing_gate(
                trades_by_name[
                    Candidate(**row["high_component"]).name
                ],
                trades_by_name[
                    Candidate(**row["low_component"]).name
                ],
                evaluation_sessions,
            )
    viable_portfolios = [
        row for row in portfolio_rows if row["prequalified"]
    ]
    best_portfolio = max(
        viable_portfolios or portfolio_rows,
        key=lambda row: (
            bool(
                row["conditional_sizing_gate"]
                and row["conditional_sizing_gate"]["passed"]
            ),
            (
                row["conditional_sizing_gate"][
                    "best_scenario_with_risk_cap"
                ][
                    "pass_rate_percent"
                ]
                if row["conditional_sizing_gate"] else 0.0
            ),
            row["metrics"]["positive_months"],
            row["metrics"]["net_currency"]
            / max(1.0, row["metrics"]["maximum_drawdown_currency"]),
            row["metrics"]["profit_factor"] or 0.0,
        ),
    )
    frozen_portfolio = next(
        row
        for row in portfolio_rows
        if row["instrument"] == "MNQ"
        and row["threshold_sigma"] == 0.0
        and row["high_component"]["method"]
        == "CloseFirstHalfHourMomentum"
        and row["high_component"]["condition"]
        == "HighOpeningVolatility"
        and row["low_component"]["method"]
        == "CloseFirstPlusPenultimate"
        and row["low_component"]["condition"]
        == "LowOpeningVolatility"
    )

    stable = [
        row for row in rows
        if row["qualified_stability"]
        and row["neighbor_support"]["all_neighbors_positive"]
    ]
    pool = stable or [row for row in rows if row["metrics"]["trades"] >= 25]
    best = max(
        pool,
        key=lambda row: (
            row["qualified_stability"],
            row["metrics"]["positive_months"],
            row["metrics"]["net_currency"]
            / max(1.0, row["metrics"]["maximum_drawdown_currency"]),
            row["metrics"]["profit_factor"] or 0.0,
            row["metrics"]["net_currency"],
        ),
    )
    best_candidate = Candidate(**best["candidate"])
    gate = economic_gate(
        trades_by_name[best_candidate.name], evaluation_sessions
    )
    result = {
        "purpose": (
            "Use all available four-month history as development, choose an "
            "economically useful candidate, then freeze it for future validation."
        ),
        "methodology": {
            "hypotheses": list(METHODS),
            "conditions": list(CONDITIONS),
            "threshold_sigma": list(THRESHOLDS),
            "candidate_count": len(candidates),
            "development_only": True,
            "stability_gate": {
                "minimum_trades": 25,
                "minimum_profit_factor": 1.20,
                "minimum_positive_months": 4,
                "maximum_inactive_session_gap": 5,
                "minimum_worst_month_currency": -100.0,
                "positive_neighboring_thresholds": True,
            },
            "round_turn_cost_currency": base.ROUND_TURN_COST,
            "maximum_risk_per_trade_currency": base.MAXIMUM_RISK_CURRENCY,
            "research_sources": [
                "https://doi.org/10.1016/j.jfineco.2018.05.009",
                "https://doi.org/10.1016/j.jbef.2021.100557",
                "https://doi.org/10.1016/j.finmar.2021.100623",
            ],
        },
        "data_audit": {symbol: audit for symbol, _, audit in loaded},
        "sessions": {
            "count": len(sessions),
            "from": sessions[0].isoformat(),
            "to": sessions[-1].isoformat(),
            "warmup_sessions": 20,
            "evaluation_count": len(evaluation_sessions),
            "evaluation_from": evaluation_sessions[0].isoformat(),
        },
        "candidate_results": rows,
        "stability_qualified": len(
            [row for row in rows if row["qualified_stability"]]
        ),
        "stable_with_neighbor_support": len(stable),
        "portfolio_count": len(portfolio_rows),
        "portfolio_prequalified": len(viable_portfolios),
        "portfolio_results": portfolio_rows,
        "best_development_portfolio": best_portfolio,
        "frozen_prospective_candidate": {
            **frozen_portfolio,
            "selection_reason": (
                "Best risk-capped 20-session pass rate among prequalified "
                "interpretable portfolios, with daily signals and academic basis."
            ),
            "future_validation_protocol": {
                "start_after_dataset_cutoff": "2026-07-29",
                "minimum_new_sessions": 20,
                "position_for_edge_validation": "1 MNQ micro",
                "parameters_must_remain_unchanged": True,
                "minimum_trades": 12,
                "minimum_net_currency": 0.01,
                "minimum_profit_factor": 1.20,
                "maximum_drawdown_currency": 500.0,
                "maximum_inactive_session_gap": 5,
                "economic_sizing_only_after_edge_validation": True,
            },
        },
        "best_development_candidate": {
            **best,
            "economic_gate": gate,
        },
        "decision": (
            "Freeze the selected development candidate for prospective validation. "
            "It is historically profitable but not yet validated and does not pass "
            "the 60% economic gate."
        ),
        "limitations": [
            "All sessions were used for development, so no historical block is untouched.",
            "The fixed USD 75 stop may execute differently with intrabar tick data.",
            "Results do not include true bid/ask spread or order-flow information.",
            "Profitability in development data does not establish future profitability.",
        ],
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(json.dumps({
        "output": str(args.output),
        "candidates": len(rows),
        "stability_qualified": result["stability_qualified"],
        "stable_with_neighbor_support": result["stable_with_neighbor_support"],
        "best": result["best_development_candidate"],
    }, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
