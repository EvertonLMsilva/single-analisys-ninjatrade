# Redesenho da operação a partir dos logs v7

Data da análise: 2026-07-28

## Objetivo

Reavaliar as operações sem preservar por obrigação os perfis escolhidos na rodada anterior e identificar uma hipótese mais simples para uma nova validação prospectiva.

Os resultados permanecem hipotéticos. A entrada é presumida no fechamento do candle do sinal, os eventos são inferidos por OHLC e não estão incluídos comissão, taxas, slippage ou preenchimento real.

## Amostra

- 16 arquivos CSV v7;
- 504 registros;
- datas: 20, 21, 22, 23, 24, 26, 27 e 28/07/2026;
- 250 registros MES e 254 registros MNQ;
- uma única versão: `0.8.1-beta.1`;
- nenhuma chave duplicada, chave vazia ou inconsistência no cálculo financeiro;
- datas anteriores a 28/07 usadas apenas como diagnóstico histórico;
- 28/07 ainda era parcial no momento da análise.

Algumas datas são parciais e 26/07 corresponde à abertura de domingo. Portanto, as oito datas não equivalem a oito sessões completas.

## Comparação geral

| Ativo e setup | Alvo | Decididos | Acerto | Resultado R | Resultado USD | Profit factor | Drawdown USD |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| MES TrendPullback | 1R | 128 | 42,97% | -18R | -663,75 | 0,58 | 685,00 |
| MES EmaCrossBaseline | 1R | 13 | 53,85% | +1R | +28,75 | 1,16 | 72,50 |
| MNQ TrendPullback | 1R | 108 | 51,85% | +4R | -331,50 | 0,85 | 427,00 |
| MNQ TrendPullback | 1,5R | 104 | 40,38% | +1R | -701,00 | 0,73 | 725,75 |
| MNQ TrendPullback | 2R | 93 | 26,88% | -18R | -1.212,00 | 0,55 | 1.245,00 |
| MNQ EmaCrossBaseline | 1R | 8 | 25,00% | -4R | -239,50 | 0,38 | 314,00 |

Nenhum perfil atual demonstrou vantagem financeira robusta. O MES baseline ficou ligeiramente positivo, mas teve apenas 13 decisões e 38 expirações em 52 sinais. O MNQ pullback ficou positivo em R em 1R e 1,5R, mas negativo em moeda porque as perdas carregaram risco maior.

## Separação decisiva no MNQ

No MNQ pullback com alvo de 1R:

| Direção | Decididos | Acerto | Resultado R | Resultado USD |
| --- | ---: | ---: | ---: | ---: |
| Compra | 60 | 40,00% | -12R | -859,50 |
| Venda | 48 | 66,67% | +16R | +528,00 |

A mistura das duas direções escondeu um comportamento oposto. As compras foram responsáveis pela maior parte do prejuízo. As vendas permaneceram positivas nas seis datas em que tiveram resultados decididos, mas o risco até USD 75 elevou o drawdown.

## Hipótese candidata

Aplicando apenas:

- MNQ;
- setup `TrendPullback`;
- direção `Short`;
- risco financeiro máximo de USD 50 por contrato;
- alvo de 1R;
- validade atual de três candles;
- nenhuma filtragem adicional de horário;

o resultado diagnóstico foi:

| Métrica | Resultado |
| --- | ---: |
| Sinais aceitos | 46 |
| Decididos | 34 |
| Alvos | 24 |
| Stops | 10 |
| Acerto | 70,59% |
| Resultado | +14R |
| Resultado bruto hipotético | +428,00 USD |
| Expectativa por decisão | +12,59 USD |
| Profit factor | 2,24 |
| Maior sequência de stops | 3 |
| Drawdown máximo | 124,50 USD |
| Dias positivos/negativos | 5 / 1 |

O bloco anterior a 26/07 produziu +306,50 USD e o bloco de 26 a 28/07 produziu +121,50 USD. Isso reduz, mas não elimina, o risco de concentração em um único dia.

O intervalo de confiança Wilson de 95% para a taxa de acerto é aproximadamente 53,8% a 83,2%. O teste binomial unilateral contra 50% resulta em aproximadamente 0,012, mas esse valor é apenas descritivo: várias combinações foram examinadas na mesma amostra e a regra foi escolhida depois de observar os dados.

## Sensibilidade a custos

Os 46 sinais aceitos teriam o seguinte resultado após uma fricção hipotética por sinal:

| Custo total por sinal | Resultado líquido estimado |
| ---: | ---: |
| 1,00 USD | +382,00 USD |
| 2,00 USD | +336,00 USD |
| 3,00 USD | +290,00 USD |
| 5,00 USD | +198,00 USD |
| 7,50 USD | +83,00 USD |

O ponto de equilíbrio é aproximadamente 9,30 USD de custo total por sinal. Essa tabela não substitui a medição de comissão e slippage reais.

## Risco para a conta de 25k

Com drawdown informado de USD 1.500:

- risco máximo de USD 50 equivale a 3,33% do drawdown;
- o drawdown diagnóstico de USD 124,50 equivale a 8,30%;
- uma parada pessoal de USD 100 ou duas perdas consecutivas limita uma sessão a aproximadamente 6,67% do drawdown antes de custos.

A Take Profit Trader informa atualmente ausência de limite diário, mas diferencia o drawdown por tipo de conta: teste com drawdown de fim de dia e PRO com drawdown intradiário. A regra aplicável deve ser confirmada conforme o tipo exato da conta.

Fonte consultada: https://try.takeprofittrader.com/nf100-1125-TPT-FAQs

## Recomendação

Abrir uma nova rodada prospectiva com uma única hipótese visível:

1. MNQ `TrendPullback` somente vendido;
2. rejeitar risco acima de USD 50;
3. alvo principal em 1R;
4. manter a entrada, o stop técnico e a validade atuais;
5. não adicionar filtro de horário nesta primeira rodada;
6. interromper novos sinais após duas perdas consecutivas ou USD 100 de perda pessoal na sessão;
7. manter MES apenas como observação silenciosa;
8. exigir pelo menos cinco sessões completas e 30 resultados decididos;
9. registrar custos reais separadamente antes de qualquer uso operacional.

Esta é uma candidata para teste, não uma recomendação de operação real.

