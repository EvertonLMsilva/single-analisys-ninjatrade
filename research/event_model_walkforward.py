#!/usr/bin/env python3
"""Modelo logístico walk-forward para eventos intradiários de 1 minuto.

O modelo usa somente recursos disponíveis no instante do evento, amostra a cada cinco
minutos e permite no máximo uma operação por ativo/dia. A seleção do limiar ocorre
dentro do bloco inicial; validação e confirmação permanecem cronologicamente à frente.
Não acessa NinjaTrader, conta ou ordens.
"""

from __future__ import annotations

import argparse
import json
import math
import statistics
from dataclasses import asdict, dataclass
from datetime import date, datetime, timedelta
from pathlib import Path
from typing import Optional

import numpy as np

import prop_evaluation
import regime_structure_backtest as base


FEATURE_NAMES = (
    "direction",
    "opening_alignment",
    "trend_up",
    "trend_down",
    "balance",
    "transition",
    "opening_move",
    "opening_range",
    "opening_efficiency",
    "opening_acceptance",
    "opening_relative_volume",
    "opening_vwap_slope",
    "vwap_distance",
    "momentum_5",
    "momentum_15",
    "candle_body",
    "close_location",
    "relative_volume",
    "opening_range_position",
    "minutes_after_decision",
    "risk_fraction",
    "cross_market_aligned",
)


@dataclass
class Event:
    instrument: str
    trading_day: date
    timestamp: datetime
    direction: str
    target_r: float
    features: list[float]
    entry: float
    stop: float
    target: float
    risk_currency: float
    status: str
    net_currency: float

    @property
    def signal_time(self) -> datetime:
        return self.timestamp


@dataclass
class LogisticModel:
    means: list[float]
    scales: list[float]
    weights: list[float]
    intercept: float


def sigmoid(values: np.ndarray) -> np.ndarray:
    clipped = np.clip(values, -35.0, 35.0)
    return 1.0 / (1.0 + np.exp(-clipped))


def fit_logistic(events: list[Event]) -> LogisticModel:
    if not events:
        raise ValueError("Não existem eventos para treinamento.")
    x = np.asarray([event.features for event in events], dtype=float)
    y = np.asarray([event.status == "Target" for event in events], dtype=float)
    means = x.mean(axis=0)
    scales = x.std(axis=0)
    scales[scales < 1e-9] = 1.0
    standardized = (x - means) / scales
    weights = np.zeros(standardized.shape[1], dtype=float)
    intercept = 0.0
    positives = max(1.0, y.sum())
    negatives = max(1.0, len(y) - y.sum())
    sample_weights = np.where(y > 0.5, len(y) / (2.0 * positives), len(y) / (2.0 * negatives))
    learning_rate = 0.04
    regularization = 0.08
    for _ in range(1_500):
        probabilities = sigmoid(standardized @ weights + intercept)
        error = (probabilities - y) * sample_weights
        gradient = standardized.T @ error / len(y) + regularization * weights
        intercept_gradient = error.mean()
        weights -= learning_rate * gradient
        intercept -= learning_rate * intercept_gradient
    return LogisticModel(
        means=means.tolist(),
        scales=scales.tolist(),
        weights=weights.tolist(),
        intercept=float(intercept),
    )


def predict(model: LogisticModel, events: list[Event]) -> list[float]:
    if not events:
        return []
    x = np.asarray([event.features for event in events], dtype=float)
    standardized = (
        x - np.asarray(model.means)
    ) / np.asarray(model.scales)
    return sigmoid(
        standardized @ np.asarray(model.weights) + model.intercept
    ).tolist()


def true_range(bar: base.Bar, previous_close: float) -> float:
    return max(
        bar.high - bar.low,
        abs(bar.high - previous_close),
        abs(bar.low - previous_close),
    )


def evaluate_outcome(
    bars: list[base.Bar],
    index: int,
    direction: str,
    entry: float,
    stop: float,
    target_r: float,
    spec: base.InstrumentSpec,
) -> tuple[float, str, float]:
    risk_points = entry - stop if direction == "Long" else stop - entry
    target = (
        entry + target_r * risk_points
        if direction == "Long"
        else entry - target_r * risk_points
    )
    status = "TimeExit"
    exit_price = bars[min(len(bars) - 1, index + 60)].close
    for bar in bars[index + 1:min(len(bars), index + 61)]:
        stop_hit = bar.low <= stop if direction == "Long" else bar.high >= stop
        target_hit = bar.high >= target if direction == "Long" else bar.low <= target
        if stop_hit:
            status = "AmbiguousLoss" if target_hit else "Stop"
            exit_price = stop
            break
        if target_hit:
            status = "Target"
            exit_price = target
            break
    gross = (
        exit_price - entry if direction == "Long" else entry - exit_price
    ) * spec.point_value
    return target, status, round(gross - base.ROUND_TURN_COST, 2)


def build_events(
    symbol: str,
    grouped: dict[date, list[base.Bar]],
    contexts: dict[date, base.DayContext],
    target_r: float,
) -> list[Event]:
    spec = base.SPECS[symbol]
    events: list[Event] = []
    for trading_day, context in sorted(contexts.items()):
        rth = [
            bar for bar in grouped[trading_day]
            if context.open_time < bar.timestamp <= context.session_end
        ]
        if len(rth) < 120:
            continue
        price_volume = volume = 0.0
        vwaps: list[float] = []
        atrs: list[float] = []
        ranges: list[float] = []
        volumes: list[float] = []
        previous_close = rth[0].open
        atr = rth[0].high - rth[0].low
        for bar in rth:
            typical = (bar.high + bar.low + bar.close) / 3.0
            price_volume += typical * bar.volume
            volume += bar.volume
            vwaps.append(price_volume / volume)
            tr = true_range(bar, previous_close)
            atr = tr if not atrs else ((atr * 13.0) + tr) / 14.0
            atrs.append(atr)
            ranges.append(bar.high - bar.low)
            volumes.append(bar.volume)
            previous_close = bar.close

        for index, bar in enumerate(rth):
            if not (context.decision_time < bar.timestamp <= context.decision_time + timedelta(hours=3)):
                continue
            minutes_after = int((bar.timestamp - context.decision_time).total_seconds() / 60)
            if minutes_after % 5 != 0 or index < 20 or index + 2 >= len(rth):
                continue
            median_volume = statistics.median(volumes[max(0, index - 20):index])
            for direction in ("Long", "Short"):
                sign = 1.0 if direction == "Long" else -1.0
                recent = rth[index - 2:index + 1]
                entry = bar.close
                stop = (
                    min(item.low for item in recent) - spec.tick_size
                    if direction == "Long"
                    else max(item.high for item in recent) + spec.tick_size
                )
                risk_points = entry - stop if direction == "Long" else stop - entry
                risk_currency = risk_points * spec.point_value
                if not (
                    risk_points > 0
                    and base.MINIMUM_RISK_CURRENCY
                    <= risk_currency
                    <= base.MAXIMUM_RISK_CURRENCY
                ):
                    continue
                candle_range = max(spec.tick_size, bar.high - bar.low)
                directional_location = (
                    (bar.close - bar.low) / candle_range
                    if direction == "Long"
                    else (bar.high - bar.close) / candle_range
                )
                regime = context.regime
                features = [
                    sign,
                    sign * context.direction,
                    float(regime == "TrendUp"),
                    float(regime == "TrendDown"),
                    float(regime == "Balance"),
                    float(regime == "Transition"),
                    sign * context.first_30_return / context.opening_range,
                    context.normalized_opening_range,
                    context.directional_efficiency,
                    context.acceptance,
                    context.relative_opening_volume,
                    sign * context.vwap_slope,
                    sign * (bar.close - vwaps[index]) / max(atrs[index], spec.tick_size),
                    sign * (bar.close - rth[index - 5].close) / max(atrs[index], spec.tick_size),
                    sign * (bar.close - rth[index - 15].close) / max(atrs[index], spec.tick_size),
                    sign * (bar.close - bar.open) / max(atrs[index], spec.tick_size),
                    directional_location,
                    bar.volume / median_volume if median_volume else 1.0,
                    sign * (
                        bar.close
                        - (context.opening_range_high + context.opening_range_low) / 2.0
                    ) / context.opening_range,
                    minutes_after / 180.0,
                    risk_currency / base.MAXIMUM_RISK_CURRENCY,
                    float(context.cross_market_aligned),
                ]
                target, status, net = evaluate_outcome(
                    rth, index, direction, entry, stop, target_r, spec
                )
                events.append(
                    Event(
                        instrument=symbol,
                        trading_day=trading_day,
                        timestamp=bar.timestamp,
                        direction=direction,
                        target_r=target_r,
                        features=features,
                        entry=entry,
                        stop=stop,
                        target=target,
                        risk_currency=round(risk_currency, 2),
                        status=status,
                        net_currency=net,
                    )
                )
    return events


def select_one_per_day(
    events: list[Event],
    probabilities: list[float],
    threshold: float,
) -> list[Event]:
    best: dict[date, tuple[float, Event]] = {}
    for event, probability in zip(events, probabilities):
        if probability < threshold:
            continue
        current = best.get(event.trading_day)
        if current is None or probability > current[0]:
            best[event.trading_day] = (probability, event)
    return [
        pair[1] for pair in sorted(best.values(), key=lambda item: item[1].timestamp)
    ]


def metrics(events: list[Event]) -> dict[str, object]:
    values = [event.net_currency for event in events]
    wins = [value for value in values if value > 0]
    losses = [value for value in values if value <= 0]
    gross_profit = sum(wins)
    gross_loss = abs(sum(losses))
    return {
        "trades": len(events),
        "active_sessions": len({event.trading_day for event in events}),
        "targets": sum(event.status == "Target" for event in events),
        "stops": sum(event.status in ("Stop", "AmbiguousLoss") for event in events),
        "time_exits": sum(event.status == "TimeExit" for event in events),
        "win_rate": round(len(wins) / len(events), 6) if events else None,
        "net_currency": round(sum(values), 2),
        "average_trade_currency": round(statistics.mean(values), 2) if values else None,
        "profit_factor": round(gross_profit / gross_loss, 4) if gross_loss else None,
        "maximum_drawdown_currency": round(base.maximum_drawdown(values), 2),
    }


def subset(events: list[Event], sessions: list[date]) -> list[Event]:
    selected = set(sessions)
    return [event for event in events if event.trading_day in selected]


def choose_threshold(
    model: LogisticModel,
    tuning_events: list[Event],
) -> tuple[float, dict[str, object]]:
    probabilities = predict(model, tuning_events)
    choices = []
    for threshold in (0.50, 0.55, 0.60, 0.65, 0.70, 0.75):
        selected = select_one_per_day(tuning_events, probabilities, threshold)
        result = metrics(selected)
        choices.append((threshold, result))
    viable = [
        item for item in choices
        if item[1]["trades"] >= 5
        and item[1]["net_currency"] > 0
        and item[1]["profit_factor"] is not None
        and item[1]["profit_factor"] >= 1.10
    ]
    if not viable:
        best = max(
            choices,
            key=lambda item: (
                item[1]["net_currency"],
                item[1]["trades"],
                -item[0],
            ),
        )
        return best[0], {**best[1], "passed": False}
    best = max(
        viable,
        key=lambda item: (
            item[1]["average_trade_currency"],
            item[1]["net_currency"],
            -item[1]["maximum_drawdown_currency"],
        ),
    )
    return best[0], {**best[1], "passed": True}


def block_passed(result: dict[str, object], minimum_trades: int = 5) -> bool:
    return (
        result["trades"] >= minimum_trades
        and result["net_currency"] > 0
        and result["profit_factor"] is not None
        and result["profit_factor"] >= 1.10
    )


def rolling_select(
    events: list[Event],
    all_sessions: list[date],
    evaluation_sessions: list[date],
    threshold: float,
    training_window: int = 40,
    retrain_every: int = 5,
) -> tuple[list[Event], LogisticModel]:
    selected: list[Event] = []
    model: Optional[LogisticModel] = None
    for offset, session in enumerate(evaluation_sessions):
        position = all_sessions.index(session)
        training_sessions = all_sessions[max(0, position - training_window):position]
        if model is None or offset % retrain_every == 0:
            model = fit_logistic(subset(events, training_sessions))
        day_events = subset(events, [session])
        selected.extend(
            select_one_per_day(
                day_events,
                predict(model, day_events),
                threshold,
            )
        )
    if model is None:
        raise ValueError("Nenhum modelo walk-forward foi treinado.")
    return selected, model


def economic_gate(events: list[Event], sessions: list[date]) -> dict[str, object]:
    rules = prop_evaluation.EvaluationRules(1_500.0, 1_500.0, 20, 5, 0.50)
    daily = prop_evaluation.trades_by_session(sessions, events)
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
    return {
        "passed": bool(qualified),
        "qualified_micro_contracts": [
            item["micro_contracts"] for item in qualified
        ],
        "best_scenario": max(
            scenarios,
            key=lambda item: (
                item["pass_rate_percent"],
                -item["drawdown_failure_rate_percent"],
                -item["p90_maximum_drawdown_currency"],
            ),
        ),
    }


def feature_importance(model: LogisticModel) -> list[dict[str, object]]:
    rows = [
        {"feature": name, "coefficient": round(weight, 6)}
        for name, weight in zip(FEATURE_NAMES, model.weights)
    ]
    return sorted(rows, key=lambda row: abs(row["coefficient"]), reverse=True)


def run_self_tests() -> None:
    values = np.asarray([-100.0, 0.0, 100.0])
    probabilities = sigmoid(values)
    assert probabilities[0] < 1e-10
    assert probabilities[2] > 1 - 1e-10
    assert len(FEATURE_NAMES) == 22


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
    selection_end = len(sessions) // 2
    validation_end = selection_end + (len(sessions) - selection_end) // 2
    selection = sessions[:selection_end]
    internal_train = selection[: len(selection) * 2 // 3]
    internal_tune = selection[len(selection) * 2 // 3:]
    validation = sessions[selection_end:validation_end]
    confirmation = sessions[validation_end:]

    model_results = []
    for symbol in ("MNQ", "MES"):
        for target_r in (1.0, 1.5, 2.0):
            events = build_events(symbol, grouped[symbol], contexts[symbol], target_r)
            train_events = subset(events, internal_train)
            tune_events = subset(events, internal_tune)
            initial_model = fit_logistic(train_events)
            threshold, tuning_result = choose_threshold(initial_model, tune_events)

            for adaptation in ("Frozen", "Rolling40"):
                if adaptation == "Frozen":
                    validation_model = fit_logistic(subset(events, selection))
                    validation_events = subset(events, validation)
                    validation_selected = select_one_per_day(
                        validation_events,
                        predict(validation_model, validation_events),
                        threshold,
                    )
                else:
                    validation_selected, validation_model = rolling_select(
                        events,
                        sessions,
                        validation,
                        threshold,
                    )
                validation_result = metrics(validation_selected)
                validation_passed = (
                    tuning_result["passed"] and block_passed(validation_result)
                )

                confirmation_selected: list[Event] = []
                final_model = validation_model
                if validation_passed and adaptation == "Frozen":
                    final_model = fit_logistic(
                        subset(events, selection + validation)
                    )
                    confirmation_events = subset(events, confirmation)
                    confirmation_selected = select_one_per_day(
                        confirmation_events,
                        predict(final_model, confirmation_events),
                        threshold,
                    )
                elif validation_passed:
                    confirmation_selected, final_model = rolling_select(
                        events,
                        sessions,
                        confirmation,
                        threshold,
                    )
                confirmation_result = metrics(confirmation_selected)
                confirmation_passed = (
                    validation_passed and block_passed(confirmation_result)
                )
                out_of_sample = validation_selected + confirmation_selected
                model_results.append(
                    {
                        "instrument": symbol,
                        "target_r": target_r,
                        "adaptation": adaptation,
                        "event_count": len(events),
                        "threshold": threshold,
                        "internal_tuning": tuning_result,
                        "validation": validation_result,
                        "validation_passed": validation_passed,
                        "confirmation": confirmation_result,
                        "confirmation_passed": confirmation_passed,
                        "out_of_sample": metrics(out_of_sample),
                        "economic_gate": (
                            economic_gate(
                                out_of_sample, validation + confirmation
                            )
                            if validation_passed else None
                        ),
                        "top_coefficients": feature_importance(final_model)[:10],
                    }
                )

    approved = [
        row for row in model_results
        if row["confirmation_passed"]
        and row["economic_gate"] is not None
        and row["economic_gate"]["passed"]
    ]
    best_observed = max(
        model_results,
        key=lambda row: (
            row["confirmation_passed"],
            row["validation_passed"],
            row["out_of_sample"]["net_currency"],
            row["out_of_sample"]["trades"],
        ),
    )
    result = {
        "method": {
            "name": "IntradayEventLogisticWalkForwardV1",
            "features": list(FEATURE_NAMES),
            "sampling": "Every five minutes from 30 to 210 minutes after RTH open.",
            "directions": ["Long", "Short"],
            "maximum_trades_per_instrument_day": 1,
            "targets_r": [1.0, 1.5, 2.0],
            "thresholds_considered_in_internal_tuning": [
                0.50, 0.55, 0.60, 0.65, 0.70, 0.75
            ],
            "adaptations": {
                "Frozen": "Refit after validation only when the validation gate passes.",
                "Rolling40": (
                    "Retrain every five sessions using only the previous 40 sessions."
                ),
            },
            "risk_currency_per_contract": {
                "minimum": base.MINIMUM_RISK_CURRENCY,
                "maximum": base.MAXIMUM_RISK_CURRENCY,
            },
            "round_turn_cost_currency": base.ROUND_TURN_COST,
        },
        "data_audit": {symbol: audit for symbol, _, audit in loaded},
        "sessions": {
            "internal_train": [internal_train[0].isoformat(), internal_train[-1].isoformat(), len(internal_train)],
            "internal_tune": [internal_tune[0].isoformat(), internal_tune[-1].isoformat(), len(internal_tune)],
            "validation": [validation[0].isoformat(), validation[-1].isoformat(), len(validation)],
            "confirmation": [confirmation[0].isoformat(), confirmation[-1].isoformat(), len(confirmation)],
        },
        "models": model_results,
        "best_observed": best_observed,
        "approved": bool(approved),
        "decision": (
            "Promote approved model to prospective shadow validation."
            if approved
            else "Do not change the indicator; no model passed the complete gate."
        ),
        "limitations": [
            "The confirmation block was already viewed in prior studies and is not pristine.",
            "Logistic relationships are linear and cannot represent every market interaction.",
            "OHLCV bars do not provide order-flow delta, imbalance or absorption.",
            "Only data after 2026-07-29 can provide untouched prospective evidence.",
        ],
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    print(json.dumps({
        "output": str(args.output),
        "models": len(model_results),
        "validation_survivors": sum(row["validation_passed"] for row in model_results),
        "confirmation_survivors": sum(row["confirmation_passed"] for row in model_results),
        "approved": result["approved"],
        "best_observed": {
            "instrument": best_observed["instrument"],
            "target_r": best_observed["target_r"],
            "adaptation": best_observed["adaptation"],
            "validation": best_observed["validation"],
            "confirmation": best_observed["confirmation"],
        },
    }, indent=2))


if __name__ == "__main__":
    main()
