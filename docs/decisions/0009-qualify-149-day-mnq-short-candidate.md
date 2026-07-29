# ADR 0009 — Qualificar candidato vendido de MNQ com 149 dias

## Status

Aceita em 29/07/2026 para pesquisa visual. Execução não autorizada.

## Contexto

A nova exportação aumentou a amostra de aproximadamente 43 para 108 sessões e permitiu
repetir a seleção cronológica com um teste final maior e completamente separado da
escolha dos parâmetros.

## Decisão

- qualificar MNQ vendido, pullback, score mínimo 5/6, distância máxima de 2 ATR,
  volume relativo mínimo 1, alvo de 1,5R e validade de 12 candles;
- preparar esse setup como próximo candidato visual, sem ordens;
- aposentar o `EvidencePullback` atual como candidato visível quando a substituição
  for implementada;
- não habilitar compras ou MES;
- exigir pelo menos 20 resultados prospectivos em dez sessões antes de nova decisão;
- manter risco máximo de USD 50 e custo de avaliação de USD 5.

## Motivo

O candidato terminou positivo na seleção, validação e teste final. No teste foram 15
operações, +USD 216,25, profit factor 1,877 e drawdown de USD 124. Todos os 26
candidatos que sobreviveram antes do teste também passaram o portão final.

A regra instalada terminou negativa nos três períodos e em -USD 1.267,00 no total.

## Consequências

- esta decisão não altera ainda o indicador instalado;
- nenhuma execução automática ou acesso à conta;
- a próxima mudança de código deve criar uma nova rodada e preservar os parâmetros;
- compras continuam como hipótese rejeitada nesta amostra;
- resultados mensais negativos continuam possíveis.
