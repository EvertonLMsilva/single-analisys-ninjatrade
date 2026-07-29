# Decisão 0014 — Reprovar o melhor modelo observado

Data: 2026-07-29.

## Contexto

Após a reprovação do primeiro reteste estrutural, foram testados mecanismos manuais
adicionais, portfólios de até dois playbooks e um classificador de eventos com
validação cronológica.

## Decisão

- reconhecer MNQ com alvo 2R e modelo logístico congelado como melhor cenário
  observado;
- reprovar esse cenário para uso no indicador e para automação;
- não aumentar contratos para compensar a perda na confirmação;
- não continuar escolhendo filtros nos mesmos 107 pregões;
- preservar os pesquisadores reproduzíveis;
- reservar dados posteriores a 29/07 para evidência prospectiva;
- manter o indicador e o NinjaTrader sem alterações.

## Evidência

O cenário ganhou USD 108,50 na validação e perdeu USD 145,50 na confirmação. Com oito
micros, alcançou somente 28,57% de aprovação nas janelas de 20 pregões e drawdown P90
de USD 1.684.

Detalhes:
`docs/data-audits/2026-07-29-trigger-and-event-model-search.md`.

## Consequência

Não existe atualmente um cenário que atenda simultaneamente estabilidade, frequência,
meta de USD 1.500 e controle de drawdown. O próximo resultado utilizável dependerá de
novos dados, não de mais ajustes retrospectivos no mesmo período.
