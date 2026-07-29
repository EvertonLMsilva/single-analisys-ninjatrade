# Backtest offline ampliado — MNQ e MES, 149 dias

Data da análise: 29/07/2026

## Decisão

Uma regra de venda do MNQ passou pela seleção, validação e teste final. Ela está
qualificada para ser implementada como **novo candidato visual em modo somente
análise**, mas ainda não para execução automática.

Nenhuma regra de compra passou no teste final. MES comprado, MES vendido e MNQ
comprado permanecem reprovados.

## Amostra

| Ativo | Candles | Sessões válidas | Seleção | Validação | Teste final |
|---|---:|---:|---:|---:|---:|
| MNQ | 28.941 | 103 | 61 | 21 | 21 |
| MES | 29.546 | 108 | 64 | 22 | 22 |

No MNQ, as divisões cronológicas foram:

- seleção: 02/03 a 27/05;
- validação: 28/05 a 25/06;
- teste final: 26/06 a 29/07.

O teste final não participou da escolha dos parâmetros.

## Método

O método preserva a rodada anterior:

- 3.888 configurações por ativo e direção, totalizando 15.552;
- pullback na EMA, retomada da VWAP e rompimento de 12 candles;
- EMA 9/21, ATR 14, VWAP por sessão, volume relativo e score de seis critérios;
- entrada no fechamento do gatilho e avaliação a partir do candle seguinte;
- stop um tick além do candle de sinal;
- risco entre USD 5 e USD 50 por contrato;
- custo conservador de USD 5 por operação completa;
- pior caso quando stop e alvo aparecem no mesmo candle;
- somente uma operação simultânea por configuração;
- seleção cronológica 60/20/20;
- mínimo de 15 operações na seleção e 5 na validação;
- resultado positivo e profit factor mínimo de 1,10 nas duas primeiras etapas.

## Candidato aprovado

| Parâmetro | Regra |
|---|---|
| Ativo | MNQ |
| Direção | Venda |
| Família | Pullback na EMA |
| Score contextual | mínimo 5 de 6 |
| Distância da VWAP | máximo 2 ATR |
| Volume relativo | mínimo 1,0 |
| Horário | sessão completa |
| Alvo | 1,5R |
| Validade | 12 candles |
| Risco | máximo USD 50 |

A regra selecionada não impõe isoladamente a inclinação da VWAP. No histórico
selecionado, contudo, 100% dos sinais ficaram abaixo da VWAP, 95,35% ocorreram com
VWAP descendente, 93,02% tiveram inclinação das EMAs alinhada e 100% apresentaram
volume relativo de pelo menos 0,8.

## Resultado cronológico

| Período | Operações | Resultado líquido | Expectativa | PF | Drawdown |
|---|---:|---:|---:|---:|---:|
| Seleção | 58 | +USD 166,50 | +USD 2,87 | 1,169 | USD 170,75 |
| Validação | 13 | +USD 93,75 | +USD 7,21 | 1,380 | USD 113,50 |
| Teste final | 15 | **+USD 216,25** | **+USD 14,42** | **1,877** | USD 124,00 |
| Total | 86 | **+USD 476,50** | **+USD 5,54** | **1,323** | **USD 198,25** |

No total foram 43 alvos, 34 stops, cinco expirações e quatro ambiguidades tratadas
como stop.

## Estabilidade

| Mês | Operações | Resultado | PF |
|---|---:|---:|---:|
| Março | 27 | +USD 296,25 | 1,695 |
| Abril | 24 | -USD 29,75 | 0,926 |
| Maio | 11 | -USD 127,50 | 0,476 |
| Junho | 9 | +USD 121,25 | 1,763 |
| Julho | 15 | +USD 216,25 | 1,877 |

Três dos cinco meses foram positivos. Maio mostra que a regra ainda atravessa regimes
desfavoráveis e não deve ser tratada como renda constante.

Os 12 candidatos que sobreviveram antes de abrir o teste final também terminaram o
teste positivos e passaram o portão final. O resultado mediano deles foi +USD 87,88,
com intervalo entre +USD 28,00 e +USD 216,25. Isso reduz a dependência de uma única
combinação exata.

## Custos e risco

- risco mínimo observado: USD 9,50;
- risco mediano: USD 34,00;
- risco médio: USD 33,32;
- risco máximo: USD 50,00;
- com custo de USD 3: +USD 648,50 e PF 1,464;
- com custo de USD 5: +USD 476,50 e PF 1,323;
- com custo de USD 7: +USD 304,50 e PF 1,196.

A estratégia permaneceu positiva no cenário de custo mais alto.

## Comparação com a regra instalada

A reconstrução da regra `EvidencePullback` atualmente congelada terminou:

- 576 operações;
- -USD 1.548,50;
- profit factor 0,824;
- drawdown de USD 1.965,00;
- resultado negativo na seleção, validação e teste final.

O resultado inicial positivo dos logs cobria menos versões e menos dias. A base
ampliada mostra que essa regra não é estável e deve ser aposentada como candidato
visível quando a nova regra for implementada.

## Limites

- candles de cinco minutos não mostram a ordem intrabar;
- VWAP é aproximada por candle, não tick a tick;
- custos são estimados;
- houve dois meses negativos;
- o teste final contém apenas 15 operações;
- nenhuma conclusão autoriza execução real ou automática.

## Próximo portão

Implementar a regra aprovada como sinal visual, sem ordens, com nova identidade e
rodada prospectiva. Reavaliar depois de pelo menos 20 resultados decididos distribuídos
em no mínimo dez sessões, mantendo custo de USD 5, risco máximo de USD 50 e os
parâmetros congelados.
