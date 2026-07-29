#!/usr/bin/env python3
"""Simula a avaliação Take Profit Trader 25k em janelas de 20 pregões.

O script reutiliza as mesmas entradas hipotéticas do backtest offline. Ele não acessa
o NinjaTrader, não consulta contas e não envia ordens.
"""

from __future__ import annotations

import argparse
import json
import random
import statistics
from dataclasses import asdict, dataclass
from datetime import date
from pathlib import Path
from typing import Iterable, Sequence

import offline_backtest as backtest


DEFAULT_TARGET = 1_500.0
DEFAULT_DRAWDOWN = 1_500.0
DEFAULT_WINDOW_SESSIONS = 20
DEFAULT_MINIMUM_ACTIVE_DAYS = 5
DEFAULT_MAXIMUM_CONSISTENCY = 0.50
DEFAULT_MAXIMUM_MICROS = 30
DEFAULT_BOOTSTRAP_RUNS = 10_000
DEFAULT_BOOTSTRAP_BLOCK = 5
DEFAULT_RANDOM_SEED = 20_260_729


@dataclass(frozen=True)
class EvaluationRules:
    target_currency: float
    maximum_trailing_drawdown_currency: float
    maximum_sessions: int
    minimum_active_days: int
    maximum_best_day_share: float


@dataclass(frozen=True)
class Attempt:
    status: str
    sessions_used: int
    active_days: int
    ending_net_currency: float
    maximum_drawdown_currency: float
    highest_profit_day_currency: float
    consistency_share: float | None
    trailing_floor_currency: float


def qualified_candidate() -> backtest.Candidate:
    return backtest.Candidate(
        family="Pullback",
        direction="Short",
        minimum_score=5,
        require_vwap_trend=False,
        maximum_vwap_distance_atr=2.0,
        minimum_relative_volume=1.0,
        time_window="All",
        target_r=1.5,
        valid_bars=12,
    )


def trades_by_session(
    sessions: Sequence[date], trades: Iterable[backtest.Trade]
) -> list[list[float]]:
    grouped = {session: [] for session in sessions}
    for trade in sorted(trades, key=lambda item: item.signal_time):
        if trade.trading_day in grouped:
            grouped[trade.trading_day].append(trade.net_currency)
    return [grouped[session] for session in sessions]


def simulate_attempt(
    daily_trade_results: Sequence[Sequence[float]],
    contracts: int,
    rules: EvaluationRules,
) -> Attempt:
    if contracts < 1:
        raise ValueError("A quantidade de contratos deve ser positiva.")
    if len(daily_trade_results) > rules.maximum_sessions:
        raise ValueError("A tentativa contém mais pregões que o limite configurado.")

    equity = 0.0
    running_peak = 0.0
    maximum_drawdown = 0.0
    highest_eod_equity = 0.0
    trailing_floor = -rules.maximum_trailing_drawdown_currency
    active_days = 0
    highest_profit_day = 0.0

    for session_number, trade_results in enumerate(daily_trade_results, start=1):
        day_start_equity = equity
        if trade_results:
            active_days += 1

        for one_contract_net in trade_results:
            equity += one_contract_net * contracts
            running_peak = max(running_peak, equity)
            maximum_drawdown = max(maximum_drawdown, running_peak - equity)
            if equity <= trailing_floor:
                return build_attempt(
                    "Drawdown",
                    session_number,
                    active_days,
                    equity,
                    maximum_drawdown,
                    highest_profit_day,
                    trailing_floor,
                )

        daily_net = equity - day_start_equity
        highest_profit_day = max(highest_profit_day, daily_net)
        highest_eod_equity = max(highest_eod_equity, equity)
        trailing_floor = min(
            0.0,
            highest_eod_equity - rules.maximum_trailing_drawdown_currency,
        )
        consistency_share = (
            highest_profit_day / equity if equity > 0.0 else None
        )
        if (
            equity >= rules.target_currency
            and active_days >= rules.minimum_active_days
            and consistency_share is not None
            and consistency_share < rules.maximum_best_day_share
        ):
            return build_attempt(
                "Passed",
                session_number,
                active_days,
                equity,
                maximum_drawdown,
                highest_profit_day,
                trailing_floor,
            )

    return build_attempt(
        "Timeout",
        len(daily_trade_results),
        active_days,
        equity,
        maximum_drawdown,
        highest_profit_day,
        trailing_floor,
    )


def build_attempt(
    status: str,
    sessions_used: int,
    active_days: int,
    equity: float,
    maximum_drawdown: float,
    highest_profit_day: float,
    trailing_floor: float,
) -> Attempt:
    consistency = highest_profit_day / equity if equity > 0.0 else None
    return Attempt(
        status=status,
        sessions_used=sessions_used,
        active_days=active_days,
        ending_net_currency=round(equity, 2),
        maximum_drawdown_currency=round(maximum_drawdown, 2),
        highest_profit_day_currency=round(highest_profit_day, 2),
        consistency_share=round(consistency, 6) if consistency is not None else None,
        trailing_floor_currency=round(trailing_floor, 2),
    )


def rolling_attempts(
    daily_trade_results: Sequence[Sequence[float]],
    contracts: int,
    rules: EvaluationRules,
) -> list[Attempt]:
    if len(daily_trade_results) < rules.maximum_sessions:
        return []
    return [
        simulate_attempt(
            daily_trade_results[start : start + rules.maximum_sessions],
            contracts,
            rules,
        )
        for start in range(len(daily_trade_results) - rules.maximum_sessions + 1)
    ]


def bootstrap_attempts(
    daily_trade_results: Sequence[Sequence[float]],
    contracts: int,
    rules: EvaluationRules,
    runs: int,
    block_size: int,
    random_seed: int,
) -> list[Attempt]:
    if not daily_trade_results:
        return []
    if block_size < 1 or block_size > len(daily_trade_results):
        raise ValueError("O tamanho do bloco bootstrap é inválido.")

    rng = random.Random(random_seed + contracts)
    maximum_start = len(daily_trade_results) - block_size
    attempts: list[Attempt] = []
    for _ in range(runs):
        sample: list[Sequence[float]] = []
        while len(sample) < rules.maximum_sessions:
            start = rng.randint(0, maximum_start)
            sample.extend(daily_trade_results[start : start + block_size])
        attempts.append(
            simulate_attempt(sample[: rules.maximum_sessions], contracts, rules)
        )
    return attempts


def percentile(values: Sequence[float], probability: float) -> float:
    if not values:
        return 0.0
    ordered = sorted(values)
    position = (len(ordered) - 1) * probability
    lower = int(position)
    upper = min(lower + 1, len(ordered) - 1)
    fraction = position - lower
    return ordered[lower] + ((ordered[upper] - ordered[lower]) * fraction)


def summarize_attempts(attempts: Sequence[Attempt]) -> dict:
    if not attempts:
        return {
            "attempts": 0,
            "passed": 0,
            "drawdown_failures": 0,
            "timeouts": 0,
            "pass_rate_percent": 0.0,
            "drawdown_failure_rate_percent": 0.0,
        }

    passed = [attempt for attempt in attempts if attempt.status == "Passed"]
    nets = [attempt.ending_net_currency for attempt in attempts]
    drawdowns = [attempt.maximum_drawdown_currency for attempt in attempts]
    return {
        "attempts": len(attempts),
        "passed": len(passed),
        "drawdown_failures": sum(
            attempt.status == "Drawdown" for attempt in attempts
        ),
        "timeouts": sum(attempt.status == "Timeout" for attempt in attempts),
        "pass_rate_percent": round(len(passed) / len(attempts) * 100.0, 2),
        "drawdown_failure_rate_percent": round(
            sum(attempt.status == "Drawdown" for attempt in attempts)
            / len(attempts)
            * 100.0,
            2,
        ),
        "median_sessions_to_pass": (
            round(statistics.median(attempt.sessions_used for attempt in passed), 2)
            if passed
            else None
        ),
        "median_ending_net_currency": round(statistics.median(nets), 2),
        "p10_ending_net_currency": round(percentile(nets, 0.10), 2),
        "p90_ending_net_currency": round(percentile(nets, 0.90), 2),
        "minimum_ending_net_currency": round(min(nets), 2),
        "maximum_ending_net_currency": round(max(nets), 2),
        "median_maximum_drawdown_currency": round(
            statistics.median(drawdowns), 2
        ),
        "p90_maximum_drawdown_currency": round(percentile(drawdowns, 0.90), 2),
    }


def run_evaluation(
    mnq_path: Path,
    mes_path: Path,
    rules: EvaluationRules,
    maximum_micros: int,
    bootstrap_runs: int,
    bootstrap_block: int,
    random_seed: int,
) -> dict:
    mnq_bars = backtest.load_bars(mnq_path)
    mes_bars = backtest.load_bars(mes_path)
    backtest.calculate_features(mnq_bars)
    backtest.calculate_features(mes_bars)
    mnq_excluded, _, alignment = backtest.invalid_sessions_from_alignment(
        mnq_bars, mes_bars
    )
    sessions = sorted(
        {bar.trading_day for bar in mnq_bars} - mnq_excluded
    )
    candidate = qualified_candidate()
    trades = backtest.simulate_candidate(
        "MNQ",
        mnq_bars,
        candidate,
        backtest.trigger_indices(mnq_bars, candidate.family, candidate.direction),
        point_value=2.0,
        tick_size=0.25,
        excluded_sessions=mnq_excluded,
        round_turn_cost=backtest.ROUND_TURN_COST,
    )
    baseline_metrics = backtest.metrics(trades)
    if baseline_metrics["trades"] != 86 or baseline_metrics["net_currency"] != 442.5:
        raise RuntimeError(
            "A estratégia qualificada não reconciliou com o backtest congelado: "
            f"{baseline_metrics}"
        )

    daily_results = trades_by_session(sessions, trades)
    contract_scenarios = []
    for contracts in range(1, maximum_micros + 1):
        rolling = rolling_attempts(daily_results, contracts, rules)
        bootstrap = bootstrap_attempts(
            daily_results,
            contracts,
            rules,
            bootstrap_runs,
            bootstrap_block,
            random_seed,
        )
        contract_scenarios.append(
            {
                "micro_contracts": contracts,
                "maximum_risk_per_trade_currency": round(
                    backtest.MAXIMUM_RISK * contracts, 2
                ),
                "average_risk_per_trade_currency": round(
                    backtest.risk_summary(trades)["average"] * contracts, 2
                ),
                "rolling_20_session_windows": summarize_attempts(rolling),
                "moving_block_bootstrap": summarize_attempts(bootstrap),
            }
        )

    required_contracts_at_historical_average = (
        rules.target_currency
        / (baseline_metrics["net_currency"] / len(sessions) * rules.maximum_sessions)
    )
    qualified_scenarios = [
        scenario
        for scenario in contract_scenarios
        if scenario["rolling_20_session_windows"]["pass_rate_percent"] >= 60.0
        and scenario["rolling_20_session_windows"][
            "drawdown_failure_rate_percent"
        ]
        <= 15.0
    ]

    return {
        "purpose": (
            "Avaliar a viabilidade econômica do QualifiedPullback para atingir "
            "USD 1.500 em no máximo 20 pregões."
        ),
        "rules": asdict(rules),
        "position_limit": {
            "instrument": "MNQ",
            "maximum_micro_contracts_tested": maximum_micros,
            "official_rule_url": (
                "https://takeprofittraderhelp.zendesk.com/hc/en-us/articles/"
                "15169066911133-Rule-2-Do-Not-Exceed-Maximum-Position-Size"
            ),
        },
        "methodology": {
            "strategy": asdict(candidate),
            "round_turn_cost_per_micro_currency": backtest.ROUND_TURN_COST,
            "rolling_windows": (
                "Todas as janelas consecutivas de 20 pregões válidos."
            ),
            "bootstrap": (
                f"{bootstrap_runs} reamostragens determinísticas em blocos móveis "
                f"de {bootstrap_block} pregões por quantidade de contratos."
            ),
            "bootstrap_random_seed": random_seed,
            "trailing_drawdown": (
                "Piso sobe pelo maior saldo de fim de dia e para em zero; "
                "violação é verificada após cada operação hipotética."
            ),
            "consistency": (
                "Melhor dia precisa representar menos de 50% do lucro líquido."
            ),
            "limitations": [
                "Candles de cinco minutos não determinam a ordem intrabar.",
                (
                    "A violação do piso é medida no resultado fechado de cada "
                    "operação; excursões não realizadas podem tornar o risco real maior."
                ),
                "Resultados de contratos são escalados linearmente.",
                "Slippage está representado somente pelo custo conservador fixo.",
                "Janelas rolantes se sobrepõem e não são amostras independentes.",
                "Bootstrap estima cenários; não prevê resultados futuros.",
            ],
        },
        "datasets": {
            "mnq": {
                "path": mnq_path.name,
                "sha256": backtest.file_sha256(mnq_path),
                "valid_sessions": len(sessions),
                "excluded_sessions": sorted(
                    session.isoformat() for session in mnq_excluded
                ),
            },
            "mes_alignment_reference": {
                "path": mes_path.name,
                "sha256": backtest.file_sha256(mes_path),
            },
            "alignment": alignment,
        },
        "qualified_candidate_baseline": {
            "metrics": baseline_metrics,
            "risk_currency": backtest.risk_summary(trades),
            "historical_average_micro_contracts_required": round(
                required_contracts_at_historical_average, 2
            ),
            "integer_micro_contracts_required": int(
                required_contracts_at_historical_average
            )
            + (
                0
                if required_contracts_at_historical_average.is_integer()
                else 1
            ),
        },
        "contract_scenarios": contract_scenarios,
        "acceptance_gate": {
            "minimum_rolling_pass_rate_percent": 60.0,
            "maximum_rolling_drawdown_failure_rate_percent": 15.0,
            "qualified_scenarios": [
                scenario["micro_contracts"] for scenario in qualified_scenarios
            ],
            "passed": bool(qualified_scenarios),
        },
        "decision": (
            "QualifiedPullback é economicamente aprovado para o objetivo de 20 pregões."
            if qualified_scenarios
            else "QualifiedPullback é economicamente reprovado para o objetivo de 20 pregões."
        ),
    }


def run_self_tests() -> None:
    rules = EvaluationRules(1_500, 1_500, 20, 5, 0.50)
    steady_winner = [[160.0] for _ in range(10)] + [[] for _ in range(10)]
    passed = simulate_attempt(steady_winner, 1, rules)
    assert passed.status == "Passed"
    assert passed.sessions_used == 10
    assert passed.active_days == 10
    assert passed.ending_net_currency == 1_600.0
    assert passed.consistency_share == 0.10

    concentrated = [[800.0], [175.0], [175.0], [175.0], [175.0]] + [
        [] for _ in range(15)
    ]
    timeout = simulate_attempt(concentrated, 1, rules)
    assert timeout.status == "Timeout"
    assert timeout.ending_net_currency == 1_500.0
    assert timeout.consistency_share > 0.50

    drawdown = simulate_attempt([[-1_500.0]] + [[] for _ in range(19)], 1, rules)
    assert drawdown.status == "Drawdown"
    assert drawdown.sessions_used == 1

    trailing = [[1_000.0], [-1_500.0]] + [[] for _ in range(18)]
    trailed_out = simulate_attempt(trailing, 1, rules)
    assert trailed_out.status == "Drawdown"
    assert trailed_out.trailing_floor_currency == -500.0

    attempts = rolling_attempts([[100.0] for _ in range(21)], 1, rules)
    assert len(attempts) == 2


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mnq", type=Path, required=True)
    parser.add_argument("--mes", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--target", type=float, default=DEFAULT_TARGET)
    parser.add_argument("--drawdown", type=float, default=DEFAULT_DRAWDOWN)
    parser.add_argument(
        "--sessions", type=int, default=DEFAULT_WINDOW_SESSIONS
    )
    parser.add_argument(
        "--minimum-active-days",
        type=int,
        default=DEFAULT_MINIMUM_ACTIVE_DAYS,
    )
    parser.add_argument(
        "--maximum-micros", type=int, default=DEFAULT_MAXIMUM_MICROS
    )
    parser.add_argument(
        "--bootstrap-runs", type=int, default=DEFAULT_BOOTSTRAP_RUNS
    )
    parser.add_argument(
        "--bootstrap-block", type=int, default=DEFAULT_BOOTSTRAP_BLOCK
    )
    parser.add_argument(
        "--random-seed", type=int, default=DEFAULT_RANDOM_SEED
    )
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()

    if args.self_test:
        run_self_tests()
    rules = EvaluationRules(
        args.target,
        args.drawdown,
        args.sessions,
        args.minimum_active_days,
        DEFAULT_MAXIMUM_CONSISTENCY,
    )
    result = run_evaluation(
        args.mnq,
        args.mes,
        rules,
        args.maximum_micros,
        args.bootstrap_runs,
        args.bootstrap_block,
        args.random_seed,
    )
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, indent=2, ensure_ascii=False),
        encoding="utf-8",
    )
    print(json.dumps(result, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
