# Decisão 0006 — Selecionar pullback de MNQ vendido por evidência

## Situação

Aceita em 29/07/2026.

## Contexto

A alteração frequente de filtros não estava produzindo uma decisão operacional estável.
Os CSVs v8 já permitem comparar o pullback base por ativo, direção, risco e contexto.

## Decisão

Adicionar o setup `EvidencePullback` e torná-lo o único candidato visível. Ele aceita
somente pullbacks vendidos no MNQ quando:

- o preço está abaixo da VWAP;
- a VWAP está inclinada para baixo;
- o score contextual é de pelo menos 4/6;
- o risco por contrato não supera USD 50.

O alvo de validação é 1R. O início prospectivo é 30/07/2026. A regra permanecerá congelada
por pelo menos dez resultados decididos.

## Consequências

- compras e MES deixam de aparecer como recomendação sem serem apagados da pesquisa;
- o contexto estrito anterior permanece como referência silenciosa;
- o indicador continua sem execução de ordens;
- a quantidade de sinais cai, mas cada sinal passa a corresponder à única hipótese que
  permaneceu positiva nos dois períodos temporais analisados;
- qualquer nova estratégia será primeiro testada offline, sem alterar este candidato.

## Evidência

Consultar `docs/data-audits/2026-07-29-evidence-strategy-selection.md`.

