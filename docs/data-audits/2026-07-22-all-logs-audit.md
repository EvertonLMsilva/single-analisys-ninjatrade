# Auditoria consolidada de todos os logs em 2026-07-22

## Escopo

Foram inspecionados todos os 36 arquivos CSV existentes em `Documents/NinjaTrader 8/TradeAssistant/Data`:

- 10 arquivos v2, com 80 registros;
- 10 arquivos v3, com 84 registros;
- 16 arquivos v4, com 595 registros.

Os formatos v2 e v3 são fotografias anteriores de períodos que depois foram regenerados no v4. Eles foram preservados e verificados, mas não foram somados ao desempenho para evitar dupla ou tripla contagem. As métricas comparáveis usam somente o v4.

O conjunto v4 abrange sinais de 2026-07-14 21:10 até 2026-07-22 00:50. O dia 22 ainda estava parcial e foi separado das conclusões dos dias completos.

## Integridade

- nenhum arquivo possui chaves vazias ou duplicadas internamente;
- todos os 16 arquivos v4 têm o mesmo cabeçalho de 34 colunas;
- todos os 595 registros v4 usam `0.6.0-beta.1`;
- nenhum erro foi encontrado no cálculo `|entrada - stop| × valor do ponto = risco financeiro`;
- existe um sinal ativo no MES às 00:50 de 22/07, situação normal para um arquivo ainda em atualização.

Ao consolidar os ativos, aparecem 150 grupos de `RecordKey` repetidos. Todos ocorrem entre MNQ e MES; não existem repetições dentro do mesmo instrumento. Os arquivos separados não estão corrompidos, mas a chave deve incluir instrumento e período antes de qualquer consolidação global por identificador.

## Desempenho dos dias completos até 21/07

| Ativo | Setup | Candidatos | Aceitos | Rejeitados | Alvos | Stops | Expirados | Ambíguos | Taxa decidida | Resultado | Bruto hipotético |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| MNQ | TrendPullback | 227 | 158 | 69 | 24 | 70 | 61 | 3 | 25,53% | -22R | -USD 1.334,50 |
| MNQ | EmaCrossBaseline | 63 | 19 | 44 | 0 | 6 | 13 | 0 | 0,00% | -6R | -USD 385,50 |
| MES | TrendPullback | 238 | 236 | 2 | 21 | 91 | 122 | 2 | 18,75% | -49R | -USD 1.247,50 |
| MES | EmaCrossBaseline | 61 | 61 | 0 | 3 | 10 | 48 | 0 | 23,08% | -4R | -USD 215,00 |

Com alvo de 2R e stop de 1R, o ponto de equilíbrio teórico exige mais de 33,33% de alvos entre resultados decididos, antes dos custos. Nenhum grupo atingiu esse nível.

## Pullback por dia

| Data | MNQ R | MNQ USD | MES R | MES USD |
|---|---:|---:|---:|---:|
| 14/07 | 0R | +20,00 | 0R | -5,00 |
| 15/07 | -12R | -445,00 | -12R | -303,75 |
| 16/07 | -6R | -188,50 | -6R | -243,75 |
| 17/07 | +4R | +168,50 | +1R | -11,25 |
| 19/07 | -2R | -109,50 | -2R | -50,00 |
| 20/07 | 0R | -227,00 | -10R | -238,75 |
| 21/07 | -6R | -553,00 | -20R | -395,00 |

Somente 17/07 foi positivo em R nos dois ativos. A diferença entre R e dólares ocorre porque cada sinal tem risco financeiro diferente.

## Parcial de 22/07

### MNQ

- dois candidatos de pullback;
- um rejeitado por risco;
- um aceito e encerrado em stop;
- parcial de -1R e -USD 55,50.

### MES

- dois pullbacks aceitos, ambos encerrados em stop: -2R e -USD 31,25;
- dois sinais da linha de base: um stop e um ainda ativo às 00:50.

O dia parcial não deve ser usado para decidir filtros ou parâmetros.

## Horário e direção

Não foi encontrado horário robusto nos dias completos. No MNQ, 05:00 acumulou +4R em sete sinais, mas horários próximos e a maior parte do restante permaneceram negativos. A antiga hipótese de 06:00 a 08:59 não se sustentou: esse intervalo somou -5R após o fechamento completo de 21/07.

No MNQ, compras terminaram em -10R e vendas em -12R. No MES, compras terminaram em -30R e vendas em -19R. Esses resultados não justificam aprovar uma direção isolada.

## Faixas de risco

### MNQ

| Risco por sinal | Sinais | Alvos | Stops | Expirados | Taxa decidida | Resultado |
|---|---:|---:|---:|---:|---:|---:|
| Até USD 25 | 32 | 9 | 15 | 5 | 37,50% | +3R |
| USD 25,01–50 | 65 | 10 | 27 | 28 | 27,03% | -7R |
| USD 50,01–75 | 61 | 5 | 28 | 28 | 15,15% | -18R |

O resultado positivo abaixo de USD 25 não é robusto: os dias 15, 16 e 17 perderam nessa faixa; todo o ganho ficou concentrado em 20 e 21/07. O limite não deve ser reduzido para USD 25 como regra com esta amostra.

### MES

Todas as faixas ficaram negativas: -29R até USD 25, -17R entre USD 25,01 e 50 e -3R entre USD 50,01 e 75.

## MFE e hipótese de alvo

Entre os expirados:

- MNQ: 50 de 61 chegaram a 0,5R, 29 chegaram a 1R e 13 chegaram a 1,5R;
- MES: 85 de 122 chegaram a 0,5R, 51 chegaram a 1R e 18 chegaram a 1,5R.

Entre os sinais que terminaram em stop, 10 de 70 no MNQ e 5 de 91 no MES tiveram MFE de pelo menos 1R. MFE não revela a ordem intrabar dos eventos; portanto, não permite afirmar retrospectivamente que um alvo menor teria ocorrido antes do stop. É necessário rastrear 1R, 1,5R e 2R simultaneamente.

## Sequências de perdas

- MNQ: maior sequência de 12 stops entre resultados decididos;
- MES: maior sequência de 18 stops entre resultados decididos.

Essas sequências reforçam que o setup não deve ser usado em operação real na configuração atual.

## Efeito da parada pessoal de USD 225

A parada teria sido acionada em quatro dos sete dias completos em cada ativo. Aplicada separadamente aos resultados hipotéticos:

- MNQ: reduziria o acumulado bruto de -USD 1.334,50 para aproximadamente -USD 888,50;
- MES: reduziria o acumulado bruto de -USD 1.247,50 para aproximadamente -USD 1.017,50.

Ela reduz exposição em dias ruins, mas não transforma o setup em positivo. A Take Profit Trader informa que a conta Test não possui limite diário obrigatório; USD 225 continua sendo uma proteção pessoal. Fonte consultada: `https://try.takeprofittrader.com/nf100-1125-TPT-FAQs`.

## Conclusão e próxima alteração recomendada

Os logs são consistentes, mas os setups atuais são negativos. Não aumentar o risco, não aplicar filtro de horário e não aprovar uma direção.

A próxima versão deve alterar somente a medição:

1. mostrar explicitamente `RESULTADO HIPOTÉTICO` no painel e CSV;
2. rastrear 1R, 1,5R e 2R em paralelo, registrando a ordem dos eventos;
3. incluir instrumento e período no `RecordKey`;
4. preservar entrada, stop técnico, validade, limite de USD 75 e ausência de ordens automáticas;
5. iniciar uma nova versão de CSV para não misturar os formatos.

Somente depois dessa coleta deve-se decidir entre alvo menor, gerenciamento após 1R ou descarte do setup.
