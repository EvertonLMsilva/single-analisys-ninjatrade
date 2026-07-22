# Decisão 0003 — Iniciar validação prospectiva controlada

Data: 2026-07-22

## Contexto

Os CSVs v5 reuniram 540 registros entre 15 e 22 de julho. A amostra permitiu eliminar combinações claramente fracas, mas ainda não comprova uma estratégia operacional. O resultado do MNQ também mostrou que saldo positivo em R pode coexistir com perda em dólares quando o risco por sinal varia.

## Decisão

Iniciar a rodada `forward-2026-07-v1`, sem execução de ordens e sem alterar entradas ou stops:

- MES `EmaCrossBaseline`: candidato em validação, alvo de 1R e exibição no gráfico;
- MES `TrendPullback`: pausado visualmente, mantendo coleta silenciosa em 1R;
- MNQ `TrendPullback`: observação, alvo de 1,5R e exibição no gráfico;
- MNQ `EmaCrossBaseline`: referência silenciosa em 1R.

As configurações da rodada são EMA 9/21, ATR 14, stop ATR 1,5, tolerância do pullback 0,1 ATR, intervalo de 3 candles, validade de 3 candles, medição configurada de 2R em paralelo e descarte acima de USD 75 por contrato.

## Critério de revisão

A primeira revisão ocorre somente depois de cinco sessões completas e pelo menos 30 resultados decididos por candidato. A avaliação deve considerar simultaneamente R, dólares por um contrato, concentração do resultado por dia, sequência de perdas, drawdown, expirados, ambíguos e custos estimados.

A data de corte é 23/07/2026. Qualquer recálculo anterior é identificado como referência histórica e não é elegível para a revisão.

Esta rodada não autoriza uso com dinheiro real.
