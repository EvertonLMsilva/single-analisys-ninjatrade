# ADR 0010 — Implementar QualifiedPullback em modo visual

## Status

Aceita em 29/07/2026.

## Contexto

O candidato vendido de MNQ qualificado no backtest de 149 dias permaneceu aprovado
depois que a expiração do simulador foi alinhada ao comportamento de 0R do
NinjaTrader. O teste final não mudou: 15 operações, +USD 216,25 e profit factor
1,877.

## Decisão

- criar `QualifiedPullback` como único setup visível no MNQ;
- congelar score mínimo 5/6, distância máxima de 2 ATR, volume relativo mínimo 1,
  alvo 1,5R, validade de 12 candles e risco entre USD 5 e USD 50;
- manter o gatilho-base de pullback vendido com EMA 9/21 e tolerância de 0,1 ATR;
- tornar `EvidencePullback` uma referência silenciosa;
- iniciar `qualified-149d-2026-07-v5` em 30/07/2026;
- exigir 20 resultados decididos em pelo menos dez sessões;
- elevar a versão para `1.1.0-beta.1`;
- não adicionar execução de ordens ou acesso à conta.

## Consequências

- o gráfico passa a mostrar apenas o candidato qualificado;
- alvo e validade desse setup são constantes internas e não dependem dos campos gerais;
- sinais abaixo de USD 5 ou acima de USD 50 são registrados como rejeitados;
- MES e compras não apresentam recomendação visual;
- a estratégia continua sendo uma hipótese em validação prospectiva.
