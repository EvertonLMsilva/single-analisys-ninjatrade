# Comportamento do filtro de risco no MNQ — 2026-07-20

## Pergunta investigada

Por que nenhuma operação apareceu no MNQ mesmo quando o gráfico parecia estar em tendência?

## Configuração observada

- versão: `0.5.0-beta.1`;
- instrumento: `MNQ 09-26`;
- período: 5 minutos;
- risco máximo: USD 75;
- política: `DescartarAcimaDoLimite`;
- arquivo: CSV v3.

## Resultado

- 44 oportunidades de cruzamento detectadas;
- 37 descartadas por risco, equivalentes a 84,1%;
- 7 aceitas;
- entre as aceitas: 5 expiraram e 2 atingiram stop;
- nenhum alvo foi atingido.

Os sinais mais recentes apresentaram riscos de USD 94 a USD 165,50, acima do limite. O exemplo das 14:00 apresentou risco de USD 154 e foi corretamente registrado como `RiskRejected`.

## Explicação

O indicador atual não gera sinal apenas porque existe tendência. A regra demonstrativa exige um novo cruzamento entre a EMA rápida e a EMA lenta. Depois do cruzamento, o sinal ainda precisa passar pelo limite financeiro.

Fluxo atual:

```text
Tendência visível
    ↓
Ocorreu novo cruzamento de EMA?
    ├── não → nenhum sinal
    └── sim
          ↓
       Risco ≤ USD 75?
          ├── não → RiskRejected
          └── sim → operação hipotética ativa
```

O aumento de 40 para 44 oportunidades em relação aos logs anteriores é esperado: sinais descartados não bloqueiam cruzamentos posteriores, enquanto uma operação ativa bloqueia novos sinais até encerrar.

## Decisão

O limite não deve ser elevado apenas para fazer operações aparecerem. Para um microcontrato de MNQ, USD 75 correspondem a no máximo 37,5 pontos de risco antes de custos. A regra atual frequentemente exige stops maiores.

Próximas alternativas a testar separadamente:

1. priorizar MES no perfil atual, pois seus stops cabem com mais frequência no orçamento;
2. criar um setup específico de pullback a favor da tendência para MNQ;
3. testar um período gráfico menor ou outro cálculo técnico de stop, sempre como nova configuração comparável;
4. manter o filtro de USD 75 durante os testes.

Não é recomendado simplesmente aproximar o stop ou aumentar o limite sem nova validação.
