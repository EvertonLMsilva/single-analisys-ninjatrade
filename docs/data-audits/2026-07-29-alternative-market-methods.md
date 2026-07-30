# Auditoria — métodos alternativos de análise de mercado

Data: 2026-07-29.

## Mudança de abordagem

Os quatro meses foram tratados integralmente como desenvolvimento. Em vez de procurar
novos cruzamentos, pullbacks ou scores, a pesquisa testou hipóteses econômicas
diferentes:

- momentum da primeira para a última meia hora;
- combinação da primeira e da penúltima meia hora;
- momentum e reversão overnight;
- continuação e reversão da abertura;
- preenchimento do gap;
- força relativa entre MNQ e MES;
- comportamento separado por volume e volatilidade da abertura.

Referências principais:

- https://doi.org/10.1016/j.jfineco.2018.05.009
- https://doi.org/10.1016/j.jbef.2021.100557
- https://doi.org/10.1016/j.finmar.2021.100623

O estudo de Gao, Han, Li e Zhou documenta que o retorno da primeira meia hora,
incluindo informação overnight, prevê o retorno da última meia hora em mercados dos
Estados Unidos e que o efeito é mais forte em dias de maior volume e volatilidade.

## Escopo

- 270 candidatos individuais;
- 984 combinações de regimes complementares;
- MNQ e MES;
- 81 pregões após 20 sessões de aquecimento;
- custo de USD 5 por operação;
- stop máximo de USD 75 por micro;
- no máximo uma operação por ativo e dia;
- todas as decisões usam somente informação disponível antes da entrada.

Uma inconsistência foi encontrada durante o desenvolvimento: a versão preliminar do
gap-fill consultava 30 minutos de volume para uma entrada aos 15 minutos. A entrada
foi movida para depois dos 30 minutos e toda a pesquisa foi executada novamente.

## Candidato congelado

Ativo: MNQ.

### Alta volatilidade na abertura

Quando a soma das amplitudes dos primeiros 30 minutos é igual ou superior à mediana
das 20 sessões anteriores:

1. calcular o retorno do fechamento anterior até o fim da primeira meia hora;
2. na abertura da última meia hora, comprar se o retorno for positivo e vender se for
   negativo;
3. sair no fechamento regular;
4. aplicar stop de USD 75 por micro.

### Baixa volatilidade na abertura

Quando a volatilidade de abertura fica abaixo da mediana:

1. somar o retorno do fechamento anterior até o fim da primeira meia hora com o
   retorno da penúltima meia hora;
2. na abertura da última meia hora, comprar se a soma for positiva e vender se for
   negativa;
3. sair no fechamento regular;
4. aplicar stop de USD 75 por micro.

Não existe limiar adicional de magnitude. O sinal ocorre em todos os pregões após o
aquecimento.

## Resultado histórico de desenvolvimento

| Métrica | Resultado |
|---|---:|
| Operações | 81 |
| Resultado líquido, 1 micro | +USD 1.244,00 |
| Média por operação | +USD 15,36 |
| Taxa de acerto | 46,91% |
| Profit factor | 1,416 |
| Drawdown máximo | USD 621,00 |
| Maior intervalo sem sinal | 0 pregão |
| Meses positivos | 4 de 5 |

Resultado mensal:

- março: +USD 51,00;
- abril: +USD 70,00;
- maio: +USD 430,00;
- junho: +USD 887,50;
- julho: -USD 194,50.

O resultado é lucrativo, mas concentrado em junho e ainda instável em julho.

## Meta de 20 pregões

O melhor dimensionamento sob risco máximo de USD 300 por operação foi:

- 2 micros em alta volatilidade;
- 4 micros em baixa volatilidade;
- aprovação histórica: 50%;
- falha por drawdown: 11,29%;
- drawdown P90: USD 1.592,80.

Portanto, o candidato não alcançou o portão anterior de 60% de aprovação e drawdown
P90 de até USD 1.000. O dimensionamento não será usado no primeiro teste futuro.

## Protocolo futuro congelado

- somente dados posteriores a 29/07;
- mínimo de 20 novos pregões;
- um micro de MNQ;
- mínimo de 12 operações;
- resultado líquido positivo;
- PF mínimo de 1,20;
- drawdown máximo de USD 500;
- no máximo cinco pregões consecutivos sem sinal;
- nenhuma mudança de regra durante a rodada;
- dimensionamento econômico somente depois da confirmação da vantagem.

## Interpretação

Esta é a primeira nova abordagem que produziu lucro histórico com frequência diária e
uma hipótese respaldada por pesquisa externa. Ela é um **candidato de
desenvolvimento**, não uma estratégia validada. Como todos os quatro meses foram
usados para construir e escolher a regra, somente dados futuros podem validar o
resultado.

Artefatos:

- `research/alternative_market_methods.py`;
- `research/results/2026-07-29-alternative-market-methods.json`.
