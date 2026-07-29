# Decisão 0012 — Reprovar a primeira busca de portfólios para a avaliação

Data: 2026-07-29.

## Contexto

O candidato visual `QualifiedPullback` não alcançou a meta de USD 1.500 em 20
pregões. Foi criada uma busca separada com seis famílias de setups, MNQ, MES, compras,
vendas, posições de 1 a 30 micros e portfólios de até três componentes.

## Decisão

- reprovar todos os portfólios da primeira busca;
- não alterar o indicador;
- manter o teto de USD 1.000 para o drawdown P90;
- não reduzir o mínimo de 60% de aprovação depois de observar a validação;
- considerar contaminado o trecho final consultado durante o desenvolvimento
  preliminar;
- exigir dados posteriores a 29/07 como próxima evidência realmente fora da amostra.

## Evidência

Dos 1.099 portfólios únicos, 60 passaram na seleção e nenhum passou na validação.
Todos os 60 ficaram abaixo de 60% de aprovação no período seguinte.

Detalhes:
`docs/data-audits/2026-07-29-prop-strategy-redesign.md`.

## Consequência

O resultado evita publicar uma combinação aparentemente forte na seleção, mas
dependente do regime. A próxima pesquisa pode usar walk-forward para desenhar uma
regra, porém somente dados novos poderão confirmar sua capacidade de aprovação.
