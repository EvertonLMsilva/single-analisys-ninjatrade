# Decisão 0011 — Reprovar o QualifiedPullback para a meta da avaliação

Data: 2026-07-29.

## Contexto

O backtest de 149 dias aprovou o `QualifiedPullback` pelo critério estatístico
anterior: resultado positivo na seleção, validação e teste final. Esse critério não
respondia se a estratégia conseguiria gerar USD 1.500 em até 20 pregões sem violar
o drawdown de USD 1.500.

## Decisão

- reprovar o `QualifiedPullback` como estratégia principal para iniciar a avaliação;
- preservá-lo somente como pesquisa visual e coleta de evidência;
- não aumentar contratos para compensar a expectativa baixa;
- adotar como novo portão econômico:
  - meta de USD 1.500 em até 20 pregões;
  - mínimo de 60% de aprovação nas janelas históricas;
  - máximo de 15% de falha por drawdown;
  - confirmação fora do período usado para selecionar parâmetros;
- não modificar o indicador enquanto um novo candidato não passar por esse portão.

## Evidência

O simulador de 1 a 30 micros não encontrou nenhuma quantidade aprovada. O melhor
índice histórico chegou a 19,05%, muito abaixo dos 60% exigidos. A quantidade de 18
micros, necessária para alcançar a meta pela média histórica, falhou por drawdown em
66,67% das janelas.

Detalhes:
`docs/data-audits/2026-07-29-prop-evaluation-20d.md`.

## Consequência

A versão `1.1.0-beta.1` permanece sem ordens e sem mudança de código. Sua qualificação
estatística não deve ser interpretada como qualificação econômica para a mesa.
