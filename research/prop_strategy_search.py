#!/usr/bin/env python3
"""Pesquisa estratégias pela chance de aprovação da avaliação em 20 pregões.

Seleção usa somente os primeiros 60% das sessões, validação usa os 20% seguintes e
o teste final permanece fechado até existir candidato aprovado nas duas etapas.
Nenhuma função acessa o NinjaTrader, uma conta ou envia ordens.
"""

from __future__ import annotations

import argparse
import itertools
import json
import math
from dataclasses import asdict, dataclass
from datetime import date
from pathlib import Path
from typing import Iterable, Sequence

import offline_backtest as backtest
import prop_evaluation as prop


FAMILIES = (
    "Pullback",
    "VwapReclaim",
    "SessionBreakout",
    "VwapRejection",
    "OpeningRangeBreakout",
    "VwapSnapback",
)
MINIMUM_COMPONENT_TRADES = 15
MINIMUM_COMPONENT_PROFIT_FACTOR = 1.05
COMPONENTS_PER_GROUP = 1
EXTRA_COMPONENTS = 8
MAXIMUM_PORTFOLIO_COMPONENTS = 3
VALIDATION_BOOTSTRAP_RUNS = 10_000
VALIDATION_BOOTSTRAP_BLOCK = 5
RANDOM_SEED = 20_260_729
MAXIMUM_P90_DRAWDOWN_CURRENCY = 1_000.0
FINAL_SAMPLE_ACCESSED_DURING_METHOD_DEVELOPMENT = True


@dataclass
class ComponentResult:
    identifier: str
    instrument: str
    candidate: backtest.Candidate
    trades: list[backtest.Trade]
    train_metrics: dict
    selected_contracts: int
    train_evaluation: dict
    validation_evaluation: dict
    rank: float


@dataclass
class PortfolioResult:
    identifier: str
    component_ids: tuple[str, ...]
    trades: list[backtest.Trade]
    selected_contracts: int
    train_metrics: dict
    train_evaluation: dict
    validation_evaluation: dict
    validation_bootstrap: dict | None
    rank: float
    passed_selection: bool
    passed_validation: bool


def regular_open_minutes(day: date) -> int:
    return 10 * 60 + 30 if backtest.is_us_daylight_saving(day) else 11 * 60 + 30


def research_trigger_indices(
    bars: list[backtest.Bar], family: str, direction: str
) -> list[int]:
    if family in ("Pullback", "VwapReclaim", "SessionBreakout"):
        return backtest.trigger_indices(bars, family, direction)

    is_long = direction == "Long"
    indices: list[int] = []
    opening_ranges: dict[date, tuple[float, float]] = {}
    if family == "OpeningRangeBreakout":
        opening_bars: dict[date, list[backtest.Bar]] = {}
        for bar in bars:
            minutes = bar.timestamp.hour * 60 + bar.timestamp.minute
            opening = regular_open_minutes(bar.timestamp.date())
            if opening <= minutes < opening + 30:
                opening_bars.setdefault(bar.trading_day, []).append(bar)
        opening_ranges = {
            session: (
                max(bar.high for bar in session_bars),
                min(bar.low for bar in session_bars),
            )
            for session, session_bars in opening_bars.items()
            if len(session_bars) >= 4
        }

    for index in range(24, len(bars)):
        bar = bars[index]
        previous = bars[index - 1]
        if bar.trading_day != previous.trading_day:
            continue

        if family == "VwapRejection":
            tolerance = bar.atr * 0.10
            triggered = (
                bar.low <= bar.vwap + tolerance
                and bar.close > bar.vwap
                and bar.close > bar.open
                and bar.close_location >= 0.65
                and bar.ema_fast > bar.ema_slow
                if is_long
                else bar.high >= bar.vwap - tolerance
                and bar.close < bar.vwap
                and bar.close < bar.open
                and bar.close_location <= 0.35
                and bar.ema_fast < bar.ema_slow
            )
        elif family == "OpeningRangeBreakout":
            opening_range = opening_ranges.get(bar.trading_day)
            if opening_range is None:
                continue
            opening_high, opening_low = opening_range
            minutes = bar.timestamp.hour * 60 + bar.timestamp.minute
            opening = regular_open_minutes(bar.timestamp.date())
            if not opening + 30 <= minutes < opening + 180:
                continue
            triggered = (
                previous.close <= opening_high
                and bar.close > opening_high
                and bar.close > bar.open
                and bar.close_location >= 0.65
                and bar.ema_fast > bar.ema_slow
                if is_long
                else previous.close >= opening_low
                and bar.close < opening_low
                and bar.close < bar.open
                and bar.close_location <= 0.35
                and bar.ema_fast < bar.ema_slow
            )
        elif family == "VwapSnapback":
            triggered = (
                previous.close < previous.vwap
                and previous.vwap_distance_atr >= 1.25
                and bar.close > bar.open
                and bar.close > previous.close
                and bar.close_location >= 0.65
                and bar.vwap_distance_atr < previous.vwap_distance_atr
                if is_long
                else previous.close > previous.vwap
                and previous.vwap_distance_atr >= 1.25
                and bar.close < bar.open
                and bar.close < previous.close
                and bar.close_location <= 0.35
                and bar.vwap_distance_atr < previous.vwap_distance_atr
            )
        else:
            raise ValueError(f"Família desconhecida: {family}")

        if triggered:
            indices.append(index)
    return indices


def candidate_variants(
    family: str, direction: str
) -> Iterable[backtest.Candidate]:
    time_windows = ("Opening",) if family == "OpeningRangeBreakout" else ("All", "RTH")
    for values in itertools.product(
        (4, 5),
        (False, True),
        (1.25, 2.0),
        (0.8, 1.0),
        time_windows,
        (1.0, 1.5, 2.0),
        (6, 12),
    ):
        yield backtest.Candidate(
            family=family,
            direction=direction,
            minimum_score=values[0],
            require_vwap_trend=values[1],
            maximum_vwap_distance_atr=values[2],
            minimum_relative_volume=values[3],
            time_window=values[4],
            target_r=values[5],
            valid_bars=values[6],
        )


def candidate_identifier(
    instrument: str, candidate: backtest.Candidate
) -> str:
    return (
        f"{instrument}-{candidate.family}-{candidate.direction}"
        f"-s{candidate.minimum_score}"
        f"-vt{int(candidate.require_vwap_trend)}"
        f"-d{candidate.maximum_vwap_distance_atr:g}"
        f"-v{candidate.minimum_relative_volume:g}"
        f"-{candidate.time_window}"
        f"-r{candidate.target_r:g}"
        f"-b{candidate.valid_bars}"
    )


def sessions_subset(
    ordered_sessions: Sequence[date], selected: set[date]
) -> list[date]:
    return [session for session in ordered_sessions if session in selected]


def evaluate_contracts(
    daily_results: Sequence[Sequence[float]],
    rules: prop.EvaluationRules,
) -> tuple[int, dict, float]:
    best_contracts = 1
    best_summary: dict | None = None
    best_rank = -math.inf
    for contracts in range(1, prop.DEFAULT_MAXIMUM_MICROS + 1):
        summary = prop.summarize_attempts(
            prop.rolling_attempts(daily_results, contracts, rules)
        )
        rank = (
            summary["pass_rate_percent"]
            - (summary["drawdown_failure_rate_percent"] * 2.0)
            - (
                20.0
                * summary.get("p90_maximum_drawdown_currency", math.inf)
                / rules.maximum_trailing_drawdown_currency
            )
            + min(
                summary.get("median_ending_net_currency", 0.0)
                / rules.target_currency
                * 10.0,
                10.0,
            )
            - (contracts * 0.001)
        )
        if rank > best_rank:
            best_contracts = contracts
            best_summary = summary
            best_rank = rank
    assert best_summary is not None
    return best_contracts, best_summary, round(best_rank, 6)


def evaluate_fixed_contracts(
    daily_results: Sequence[Sequence[float]],
    contracts: int,
    rules: prop.EvaluationRules,
) -> dict:
    return prop.summarize_attempts(
        prop.rolling_attempts(daily_results, contracts, rules)
    )


def passed_gate(summary: dict) -> bool:
    return (
        summary["pass_rate_percent"] >= 60.0
        and summary["drawdown_failure_rate_percent"] <= 15.0
        and summary.get("p90_maximum_drawdown_currency", math.inf)
        <= MAXIMUM_P90_DRAWDOWN_CURRENCY
    )


def component_group(component: ComponentResult) -> tuple[str, str, str]:
    return (
        component.instrument,
        component.candidate.family,
        component.candidate.direction,
    )


def trade_fingerprint(
    trades: Iterable[backtest.Trade], sessions: set[date]
) -> tuple[tuple, ...]:
    return tuple(
        (
            trade.instrument,
            trade.signal_time.isoformat(),
            trade.exit_time.isoformat(),
            trade.status,
            round(trade.entry, 8),
            round(trade.stop, 8),
            round(trade.target, 8),
            round(trade.net_currency, 8),
        )
        for trade in sorted(
            backtest.subset(list(trades), sessions),
            key=lambda item: (item.signal_time, item.instrument),
        )
    )


def component_simplicity_key(component: ComponentResult) -> tuple:
    candidate = component.candidate
    return (
        candidate.valid_bars,
        -candidate.minimum_score,
        -int(candidate.require_vwap_trend),
        candidate.maximum_vwap_distance_atr,
        -candidate.minimum_relative_volume,
        candidate.time_window,
        candidate.target_r,
        component.identifier,
    )


def deduplicate_components(
    components: Sequence[ComponentResult], train_sessions: set[date]
) -> list[ComponentResult]:
    unique: dict[tuple[tuple, ...], ComponentResult] = {}
    for component in components:
        fingerprint = trade_fingerprint(component.trades, train_sessions)
        current = unique.get(fingerprint)
        if current is None or component_simplicity_key(
            component
        ) < component_simplicity_key(current):
            unique[fingerprint] = component
    return list(unique.values())


def select_diverse_components(
    components: Sequence[ComponentResult],
) -> list[ComponentResult]:
    selected: list[ComponentResult] = []
    by_group: dict[tuple[str, str, str], list[ComponentResult]] = {}
    for component in components:
        by_group.setdefault(component_group(component), []).append(component)
    for group in sorted(by_group):
        selected.extend(
            sorted(by_group[group], key=lambda item: item.rank, reverse=True)[
                :COMPONENTS_PER_GROUP
            ]
        )
    selected_ids = {component.identifier for component in selected}
    remaining = sorted(components, key=lambda item: item.rank, reverse=True)
    for component in remaining:
        if len(selected) >= len(by_group) * COMPONENTS_PER_GROUP + EXTRA_COMPONENTS:
            break
        if component.identifier not in selected_ids:
            selected.append(component)
            selected_ids.add(component.identifier)
    return sorted(selected, key=lambda item: item.identifier)


def combine_trades(components: Sequence[ComponentResult]) -> list[backtest.Trade]:
    ordered_components = sorted(components, key=lambda item: item.identifier)
    indexed_trades = [
        (trade.signal_time, component_index, trade)
        for component_index, component in enumerate(ordered_components)
        for trade in component.trades
    ]
    indexed_trades.sort(key=lambda item: (item[0], item[1], item[2].exit_time))
    combined: list[backtest.Trade] = []
    blocked_until = None
    for _, _, trade in indexed_trades:
        if blocked_until is not None and trade.signal_time <= blocked_until:
            continue
        combined.append(trade)
        blocked_until = trade.exit_time
    return combined


def serialize_component(component: ComponentResult) -> dict:
    return {
        "identifier": component.identifier,
        "instrument": component.instrument,
        "candidate": asdict(component.candidate),
        "train_metrics": component.train_metrics,
        "selected_micro_contracts": component.selected_contracts,
        "train_evaluation": component.train_evaluation,
        "validation_evaluation": component.validation_evaluation,
        "rank": component.rank,
    }


def serialize_portfolio(portfolio: PortfolioResult) -> dict:
    return {
        "identifier": portfolio.identifier,
        "component_ids": portfolio.component_ids,
        "selected_micro_contracts": portfolio.selected_contracts,
        "train_metrics": portfolio.train_metrics,
        "train_evaluation": portfolio.train_evaluation,
        "validation_evaluation": portfolio.validation_evaluation,
        "validation_bootstrap": portfolio.validation_bootstrap,
        "rank": portfolio.rank,
        "passed_selection": portfolio.passed_selection,
        "passed_validation": portfolio.passed_validation,
    }


def deduplicate_portfolios(
    portfolios: Sequence[PortfolioResult], train_sessions: set[date]
) -> list[PortfolioResult]:
    unique: dict[tuple[tuple, ...], PortfolioResult] = {}
    for portfolio in portfolios:
        fingerprint = trade_fingerprint(portfolio.trades, train_sessions)
        current = unique.get(fingerprint)
        selection_key = (
            -portfolio.rank,
            len(portfolio.component_ids),
            portfolio.selected_contracts,
            portfolio.identifier,
        )
        current_key = (
            -current.rank,
            len(current.component_ids),
            current.selected_contracts,
            current.identifier,
        ) if current is not None else None
        if current is None or selection_key < current_key:
            unique[fingerprint] = portfolio
    return list(unique.values())


def validation_champion_score(
    portfolio: PortfolioResult, rules: prop.EvaluationRules
) -> float:
    assert portfolio.validation_bootstrap is not None
    bootstrap = portfolio.validation_bootstrap
    return round(
        bootstrap["pass_rate_percent"]
        - (2.0 * bootstrap["drawdown_failure_rate_percent"])
        - (
            20.0
            * bootstrap["p90_maximum_drawdown_currency"]
            / rules.maximum_trailing_drawdown_currency
        )
        - (0.1 * portfolio.selected_contracts)
        - (0.5 * (len(portfolio.component_ids) - 1)),
        6,
    )


def run_search(
    mnq_path: Path,
    mes_path: Path,
    rules: prop.EvaluationRules,
) -> dict:
    bars_by_instrument = {
        "MNQ": backtest.load_bars(mnq_path),
        "MES": backtest.load_bars(mes_path),
    }
    for bars in bars_by_instrument.values():
        backtest.calculate_features(bars)
    mnq_excluded, mes_excluded, alignment = backtest.invalid_sessions_from_alignment(
        bars_by_instrument["MNQ"], bars_by_instrument["MES"]
    )
    excluded_by_instrument = {
        "MNQ": mnq_excluded,
        "MES": mes_excluded,
    }
    shared_sessions = sorted(
        (
            {bar.trading_day for bar in bars_by_instrument["MNQ"]}
            - mnq_excluded
        )
        & (
            {bar.trading_day for bar in bars_by_instrument["MES"]}
            - mes_excluded
        )
    )
    train_set, validation_set, test_set = backtest.split_sessions(shared_sessions)
    train_sessions = sessions_subset(shared_sessions, train_set)
    validation_sessions = sessions_subset(shared_sessions, validation_set)
    test_sessions = sessions_subset(shared_sessions, test_set)

    triggers: dict[tuple[str, str, str], list[int]] = {}
    for instrument, bars in bars_by_instrument.items():
        for family in FAMILIES:
            for direction in ("Long", "Short"):
                triggers[(instrument, family, direction)] = research_trigger_indices(
                    bars, family, direction
                )

    evaluated_components = 0
    profitable_components: list[ComponentResult] = []
    for instrument, bars in bars_by_instrument.items():
        point_value = 2.0 if instrument == "MNQ" else 5.0
        for family in FAMILIES:
            for direction in ("Long", "Short"):
                family_triggers = triggers[(instrument, family, direction)]
                for candidate in candidate_variants(family, direction):
                    evaluated_components += 1
                    trades = backtest.simulate_candidate(
                        instrument,
                        bars,
                        candidate,
                        family_triggers,
                        point_value=point_value,
                        tick_size=0.25,
                        excluded_sessions=excluded_by_instrument[instrument],
                        round_turn_cost=backtest.ROUND_TURN_COST,
                    )
                    train_trades = backtest.subset(trades, train_set)
                    train_metrics = backtest.metrics(train_trades)
                    if (
                        train_metrics["trades"] < MINIMUM_COMPONENT_TRADES
                        or train_metrics["net_currency"] <= 0.0
                        or train_metrics["profit_factor"]
                        < MINIMUM_COMPONENT_PROFIT_FACTOR
                    ):
                        continue
                    train_daily = prop.trades_by_session(
                        train_sessions, train_trades
                    )
                    contracts, train_evaluation, rank = evaluate_contracts(
                        train_daily, rules
                    )
                    validation_daily = prop.trades_by_session(
                        validation_sessions,
                        backtest.subset(trades, validation_set),
                    )
                    profitable_components.append(
                        ComponentResult(
                            identifier=candidate_identifier(
                                instrument, candidate
                            ),
                            instrument=instrument,
                            candidate=candidate,
                            trades=trades,
                            train_metrics=train_metrics,
                            selected_contracts=contracts,
                            train_evaluation=train_evaluation,
                            validation_evaluation=evaluate_fixed_contracts(
                                validation_daily, contracts, rules
                            ),
                            rank=rank,
                        )
                    )

    raw_profitable_components = len(profitable_components)
    profitable_components = deduplicate_components(
        profitable_components, train_set
    )
    research_components = select_diverse_components(profitable_components)
    portfolios: list[PortfolioResult] = []
    for size in range(2, MAXIMUM_PORTFOLIO_COMPONENTS + 1):
        for combination in itertools.combinations(research_components, size):
            groups = [component_group(component) for component in combination]
            if len(set(groups)) != len(groups):
                continue
            trades = combine_trades(combination)
            train_trades = backtest.subset(trades, train_set)
            train_metrics = backtest.metrics(train_trades)
            if (
                train_metrics["trades"] < MINIMUM_COMPONENT_TRADES
                or train_metrics["net_currency"] <= 0.0
                or train_metrics["profit_factor"]
                < MINIMUM_COMPONENT_PROFIT_FACTOR
            ):
                continue
            train_daily = prop.trades_by_session(train_sessions, train_trades)
            contracts, train_evaluation, rank = evaluate_contracts(
                train_daily, rules
            )
            passed_selection = passed_gate(train_evaluation)
            validation_daily = prop.trades_by_session(
                validation_sessions,
                backtest.subset(trades, validation_set),
            )
            validation_evaluation = evaluate_fixed_contracts(
                validation_daily, contracts, rules
            )
            validation_bootstrap = None
            passed_validation = False
            if passed_selection and passed_gate(validation_evaluation):
                validation_bootstrap = prop.summarize_attempts(
                    prop.bootstrap_attempts(
                        validation_daily,
                        contracts,
                        rules,
                        VALIDATION_BOOTSTRAP_RUNS,
                        VALIDATION_BOOTSTRAP_BLOCK,
                        RANDOM_SEED,
                    )
                )
                passed_validation = passed_gate(validation_bootstrap)
            identifiers = tuple(
                sorted(component.identifier for component in combination)
            )
            portfolios.append(
                PortfolioResult(
                    identifier=" + ".join(identifiers),
                    component_ids=identifiers,
                    trades=trades,
                    selected_contracts=contracts,
                    train_metrics=train_metrics,
                    train_evaluation=train_evaluation,
                    validation_evaluation=validation_evaluation,
                    validation_bootstrap=validation_bootstrap,
                    rank=rank,
                    passed_selection=passed_selection,
                    passed_validation=passed_validation,
                )
            )

    raw_portfolios = len(portfolios)
    portfolios = deduplicate_portfolios(portfolios, train_set)
    ranked_components = sorted(
        profitable_components, key=lambda item: item.rank, reverse=True
    )
    ranked_portfolios = sorted(portfolios, key=lambda item: item.rank, reverse=True)
    validated = [
        portfolio for portfolio in ranked_portfolios if portfolio.passed_validation
    ]
    selection_survivors = [
        portfolio for portfolio in ranked_portfolios if portfolio.passed_selection
    ]
    validation_near_misses = sorted(
        selection_survivors,
        key=lambda portfolio: (
            -portfolio.validation_evaluation["pass_rate_percent"],
            portfolio.validation_evaluation[
                "drawdown_failure_rate_percent"
            ],
            portfolio.validation_evaluation.get(
                "p90_maximum_drawdown_currency", math.inf
            ),
            portfolio.identifier,
        ),
    )

    validation_champion: PortfolioResult | None = None
    if validated:
        validation_champion = sorted(
            validated,
            key=lambda portfolio: (
                -validation_champion_score(portfolio, rules),
                portfolio.identifier,
            ),
        )[0]

    final_test: dict
    if not validated:
        final_test = {
            "status": "LockedNoValidatedCandidate",
            "opened_in_canonical_run": False,
            "sample_integrity": (
                "ContaminatedByPreliminaryMethodDevelopment"
                if FINAL_SAMPLE_ACCESSED_DURING_METHOD_DEVELOPMENT
                else "Untouched"
            ),
            "message": (
                "Nenhum portfólio passou em seleção e validação; o teste final "
                "não participa do resultado canônico. Durante o desenvolvimento "
                "preliminar do método, porém, esse trecho chegou a ser consultado "
                "e não deve ser tratado como amostra inédita no futuro."
            ),
        }
    else:
        assert validation_champion is not None
        test_daily = prop.trades_by_session(
            test_sessions,
            backtest.subset(validation_champion.trades, test_set),
        )
        test_evaluation = evaluate_fixed_contracts(
            test_daily, validation_champion.selected_contracts, rules
        )
        final_test = {
            "status": "OpenedForPreselectedValidationChampion",
            "opened_in_canonical_run": True,
            "sample_integrity": (
                "ContaminatedByPreliminaryMethodDevelopment"
                if FINAL_SAMPLE_ACCESSED_DURING_METHOD_DEVELOPMENT
                else "Untouched"
            ),
            "selection_rule": (
                "Maior score de validação: aprovação - 2x falha por drawdown "
                "- drawdown P90 normalizado - contratos - complexidade."
            ),
            "champion_identifier": validation_champion.identifier,
            "champion_validation_score": validation_champion_score(
                validation_champion, rules
            ),
            "selected_micro_contracts": validation_champion.selected_contracts,
            "evaluation": test_evaluation,
            "passed": passed_gate(test_evaluation),
        }

    return {
        "purpose": (
            "Pesquisar componentes e portfólios que atinjam USD 1.500 em no "
            "máximo 20 pregões sem selecionar parâmetros pelo teste final."
        ),
        "rules": asdict(rules),
        "methodology": {
            "families": list(FAMILIES),
            "selection": "Primeiros 60% das sessões compartilhadas.",
            "validation": "20% seguintes; contrato permanece congelado.",
            "test": (
                "20% finais abertos somente para um campeão pré-selecionado "
                "entre os portfólios aprovados nas duas etapas anteriores."
            ),
            "component_deduplication": (
                "Configurações com operações idênticas na seleção são reduzidas "
                "a uma regra canônica sem consultar validação ou teste."
            ),
            "portfolio_deduplication": (
                "Portfólios com operações idênticas na seleção são reduzidos "
                "pelo ranking da seleção antes da escolha na validação."
            ),
            "one_position_at_a_time": True,
            "same_timestamp_priority": (
                "Identificador do componente em ordem alfabética."
            ),
            "round_turn_cost_per_micro_currency": backtest.ROUND_TURN_COST,
            "component_minimum_trades": MINIMUM_COMPONENT_TRADES,
            "component_minimum_profit_factor": MINIMUM_COMPONENT_PROFIT_FACTOR,
            "acceptance_gate": {
                "minimum_pass_rate_percent": 60.0,
                "maximum_drawdown_failure_rate_percent": 15.0,
                "maximum_p90_drawdown_currency": (
                    MAXIMUM_P90_DRAWDOWN_CURRENCY
                ),
            },
            "limitations": [
                "Pesquisa com múltiplas variantes aumenta o risco de sobreajuste.",
                "Candles de cinco minutos não determinam a ordem intrabar.",
                "Excursões não realizadas podem tornar o drawdown real maior.",
                "Custos são conservadores, mas slippage real pode ser maior.",
                "Portfólios descartam sinais durante outra operação hipotética.",
                "Amostras de validação e teste possuem apenas 21 sessões cada.",
            ],
        },
        "datasets": {
            "mnq": {
                "path": mnq_path.name,
                "sha256": backtest.file_sha256(mnq_path),
            },
            "mes": {
                "path": mes_path.name,
                "sha256": backtest.file_sha256(mes_path),
            },
            "alignment": alignment,
            "shared_valid_sessions": len(shared_sessions),
            "train_range": [
                train_sessions[0].isoformat(),
                train_sessions[-1].isoformat(),
            ],
            "validation_range": [
                validation_sessions[0].isoformat(),
                validation_sessions[-1].isoformat(),
            ],
            "test_range": [
                test_sessions[0].isoformat(),
                test_sessions[-1].isoformat(),
            ],
            "train_sessions": len(train_sessions),
            "validation_sessions": len(validation_sessions),
            "test_sessions": len(test_sessions),
        },
        "search": {
            "evaluated_components": evaluated_components,
            "raw_profitable_train_components": raw_profitable_components,
            "unique_profitable_train_components": len(profitable_components),
            "research_components": len(research_components),
            "raw_evaluated_portfolios": raw_portfolios,
            "unique_evaluated_portfolios": len(portfolios),
            "selection_survivors": sum(
                portfolio.passed_selection for portfolio in portfolios
            ),
            "validation_survivors": len(validated),
            "validation_failure_reasons": {
                "pass_rate_below_60_percent": sum(
                    portfolio.validation_evaluation["pass_rate_percent"]
                    < 60.0
                    for portfolio in selection_survivors
                ),
                "drawdown_failure_rate_above_15_percent": sum(
                    portfolio.validation_evaluation[
                        "drawdown_failure_rate_percent"
                    ]
                    > 15.0
                    for portfolio in selection_survivors
                ),
                "p90_drawdown_above_1000_currency": sum(
                    portfolio.validation_evaluation.get(
                        "p90_maximum_drawdown_currency", math.inf
                    )
                    > MAXIMUM_P90_DRAWDOWN_CURRENCY
                    for portfolio in selection_survivors
                ),
            },
        },
        "top_components": [
            serialize_component(component)
            for component in ranked_components[:20]
        ],
        "research_component_ids": [
            component.identifier for component in research_components
        ],
        "top_portfolios": [
            serialize_portfolio(portfolio)
            for portfolio in ranked_portfolios[:20]
        ],
        "selection_survivors": [
            serialize_portfolio(portfolio)
            for portfolio in selection_survivors
        ],
        "validation_near_misses": [
            serialize_portfolio(portfolio)
            for portfolio in validation_near_misses[:10]
        ],
        "validation_survivors": [
            {
                **serialize_portfolio(portfolio),
                "validation_champion_score": validation_champion_score(
                    portfolio, rules
                ),
            }
            for portfolio in validated
        ],
        "validation_champion": (
            {
                **serialize_portfolio(validation_champion),
                "validation_champion_score": validation_champion_score(
                    validation_champion, rules
                ),
            }
            if validation_champion is not None
            else None
        ),
        "final_test": final_test,
        "decision": (
            (
                "O campeão pré-selecionado passou no teste final e pode avançar "
                "somente para validação prospectiva sem ordens."
            )
            if final_test.get("passed")
            else (
                "O campeão pré-selecionado falhou no teste final; não alterar "
                "o indicador."
            )
            if final_test["opened_in_canonical_run"]
            else (
                "Nenhum candidato passou pelo portão econômico antes do teste "
                "final; não alterar o indicador."
            )
        ),
    }


def run_self_tests() -> None:
    day = date(2026, 7, 1)
    candidate = backtest.Candidate(
        "Pullback", "Long", 4, False, 2.0, 0.8, "All", 1.5, 6
    )
    identifier = candidate_identifier("MNQ", candidate)
    assert identifier.startswith("MNQ-Pullback-Long-s4")
    assert len(list(candidate_variants("OpeningRangeBreakout", "Long"))) == 96
    assert len(list(candidate_variants("Pullback", "Long"))) == 192
    assert sessions_subset([day], {day}) == [day]
    assert passed_gate(
        {
            "pass_rate_percent": 60.0,
            "drawdown_failure_rate_percent": 15.0,
            "p90_maximum_drawdown_currency": 1_000.0,
        }
    )
    assert not passed_gate(
        {
            "pass_rate_percent": 59.99,
            "drawdown_failure_rate_percent": 0.0,
            "p90_maximum_drawdown_currency": 500.0,
        }
    )
    assert not passed_gate(
        {
            "pass_rate_percent": 80.0,
            "drawdown_failure_rate_percent": 0.0,
            "p90_maximum_drawdown_currency": 1_000.01,
        }
    )
    assert round(
        validation_champion_score(
            PortfolioResult(
                "test",
                ("a", "b"),
                [],
                5,
                {},
                {},
                {},
                {
                    "pass_rate_percent": 80.0,
                    "drawdown_failure_rate_percent": 5.0,
                    "p90_maximum_drawdown_currency": 750.0,
                },
                0.0,
                True,
                True,
            ),
            prop.EvaluationRules(1_500, 1_500, 20, 5, 0.50),
        ),
        2,
    ) == 59.0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--mnq", type=Path, required=True)
    parser.add_argument("--mes", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--self-test", action="store_true")
    args = parser.parse_args()
    if args.self_test:
        run_self_tests()
    rules = prop.EvaluationRules(
        target_currency=prop.DEFAULT_TARGET,
        maximum_trailing_drawdown_currency=prop.DEFAULT_DRAWDOWN,
        maximum_sessions=prop.DEFAULT_WINDOW_SESSIONS,
        minimum_active_days=prop.DEFAULT_MINIMUM_ACTIVE_DAYS,
        maximum_best_day_share=prop.DEFAULT_MAXIMUM_CONSISTENCY,
    )
    result = run_search(args.mnq, args.mes, rules)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, indent=2, ensure_ascii=False),
        encoding="utf-8",
    )
    print(json.dumps(result, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
