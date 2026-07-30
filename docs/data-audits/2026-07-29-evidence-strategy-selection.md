# Seleção da estratégia por evidência — 29/07/2026

## Objetivo

Substituir o ciclo de ajustes a cada poucos dias por uma seleção offline e reproduzível
feita sobre todos os registros v8 disponíveis. A estratégia escolhida precisa ser simples,
respeitar o limite financeiro da conta e permanecer positiva depois de um corte temporal.

## Dados utilizados

- 14 arquivos CSV v8;
- 531 registros entre 22 e 29/07;
- 380 candidatos do setup base `TrendPullback`;
- 254 candidatos dentro do limite de risco configurado;
- MES e MNQ em candles de 5 minutos;
- resultados avaliados no alvo de 1R;
- sinais acima de USD 50 descartados;
- ambiguidades tratadas como perda completa;
- expirações tratadas como resultado zero antes de custos;
- custo conservador assumido de USD 3 por operação completa.

O custo é uma premissa de pesquisa, não uma tarifa confirmada da conta. A sensibilidade
também foi calculada com USD 2 e USD 5 por operação.

## Separação temporal

- seleção: 22 a 26/07;
- verificação posterior: 27 a 29/07;
- nova coleta prospectiva: a partir de 30/07.

Nenhuma regra foi selecionada apenas pela taxa de acerto. Foram considerados resultado
líquido, profit factor, drawdown, quantidade de operações e comportamento por dia.

## Resultado da busca

O MES não apresentou nenhuma combinação simples com pelo menos dez sinais no período de
seleção, cinco no período posterior e resultado líquido positivo nos dois períodos.

As compras de MNQ também não apresentaram estabilidade suficiente. Misturar compras e
vendas degradou o resultado, confirmando que a direção não deve ser liberada apenas para
aumentar a quantidade de sinais.

O único padrão simples positivo nos dois períodos foi:

1. ativo MNQ;
2. direção vendida;
3. pullback base confirmado;
4. entrada abaixo da VWAP da sessão;
5. VWAP inclinada para baixo;
6. score contextual mínimo de 4/6;
7. risco máximo de USD 50;
8. alvo de 1R.

## Resultado retrospectivo do padrão selecionado

| Métrica | Seleção | Período posterior | Total |
|---|---:|---:|---:|
| Sinais | 15 | 8 | 23 |
| Resultado líquido com USD 3 de custo | USD 173,00 | USD 99,00 | USD 272,00 |
| Profit factor líquido | 2,02 | 2,90 | 2,23 |
| Drawdown máximo | USD 85,00 | USD 49,00 | USD 85,00 |

No total ocorreram 15 alvos, quatro stops, duas ambiguidades e duas expirações. Considerando
alvos contra stops e ambiguidades, a taxa de acerto decidida foi 71,4%. Quatro dos cinco
dias com sinais foram positivos.

## Sensibilidade a custos

| Custo por operação | Resultado líquido | Profit factor | Drawdown |
|---|---:|---:|---:|
| USD 2 | USD 295,00 | 2,38 | USD 83,00 |
| USD 3 | USD 272,00 | 2,23 | USD 85,00 |
| USD 5 | USD 226,00 | 1,95 | USD 89,00 |

## Relação com o contrato e a conta

O multiplicador do MNQ é USD 2 por ponto e o tick de 0,25 ponto vale USD 0,50. O MES possui
multiplicador de USD 5 por ponto e tick de USD 1,25. Fonte:
https://www.cmegroup.com/articles/faqs/frequently-asked-questions-micro-e-mini-equity-index-futures.html

A conta de USD 25 mil informada possui objetivo e drawdown de USD 1.500. A estratégia
continua limitada a um risco de USD 50 por sinal, equivalente a aproximadamente 3,3% desse
drawdown, e não envia ordens. Referência oficial consultada:
https://takeprofittraderhelp.zendesk.com/hc/en-us/articles/15169070804125-Rule-1-Hit-Your-Profit-Target

## Decisão

Criar `EvidencePullback` como único candidato visível:

- MNQ vendido;
- abaixo de VWAP descendente;
- score de pelo menos 4/6;
- risco de até USD 50;
- alvo de 1R.

MES, compras e os setups anteriores continuam registrados silenciosamente para pesquisa,
mas não aparecem como recomendação operacional.

## Limitações

- a amostra ainda é pequena e não garante lucro futuro;
- os custos são estimados;
- expirações não possuem preço real de saída no CSV;
- o período posterior também foi inspecionado durante esta decisão e não é um teste cego;
- a partir de 30/07 a regra deve permanecer congelada até completar pelo menos dez
  resultados decididos, sem alterações intermediárias.

