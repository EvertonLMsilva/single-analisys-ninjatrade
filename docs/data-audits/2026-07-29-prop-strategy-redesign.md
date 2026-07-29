# Redesenho de estratégias para a avaliação em 20 pregões

Data: 2026-07-29.

## Decisão

Nenhum componente ou portfólio pesquisado está aprovado para alterar o indicador ou
iniciar a avaliação da mesa.

A busca encontrou regras muito fortes no período de seleção, mas todas perderam
estabilidade na validação cronologicamente posterior. Aumentar contratos ou escolher
o melhor resultado depois de observar os períodos seguintes esconderia esse problema.

## Portão econômico

Cada candidato precisava cumprir simultaneamente:

- atingir USD 1.500 em no máximo 20 pregões;
- respeitar o drawdown trailing de USD 1.500;
- obter pelo menos 60% de aprovação nas janelas;
- limitar a no máximo 15% as eliminações por drawdown;
- manter o drawdown P90 em no máximo USD 1.000;
- usar pelo menos cinco dias ativos;
- manter o melhor dia abaixo de 50% do lucro líquido;
- incluir USD 5 de custo por operação completa e por microcontrato.

O teto de USD 1.000 não é uma regra externa da mesa. É a margem de segurança do
projeto para não depender dos últimos USD 500 do drawdown.

## Separação temporal

Foram usadas 103 sessões compartilhadas e válidas:

- seleção: 61 sessões, de 02/03 a 27/05;
- validação: 21 sessões, de 28/05 a 25/06;
- teste final reservado: 21 sessões, de 26/06 a 29/07.

Os parâmetros e a quantidade de micros foram escolhidos somente na seleção. A
validação manteve esses valores congelados.

## Famílias pesquisadas

- pullback na EMA;
- retomada da VWAP;
- rompimento de sessão;
- rejeição da VWAP;
- rompimento da abertura;
- retorno à VWAP após extensão.

Foram consideradas compras e vendas em MNQ e MES. Os portfólios continham dois ou
três componentes e mantinham somente uma posição hipotética por vez, inclusive entre
os dois ativos.

## Resultado da busca

| Etapa | Quantidade |
|---|---:|
| Componentes avaliados | 4.224 |
| Componentes lucrativos na seleção | 210 |
| Componentes únicos após remover operações duplicadas | 192 |
| Componentes levados à composição | 21 |
| Portfólios avaliados antes da deduplicação | 1.126 |
| Portfólios únicos | 1.099 |
| Sobreviventes na seleção | 60 |
| Sobreviventes na validação | **0** |

Entre os 60 sobreviventes da seleção:

- os 60 ficaram abaixo de 60% de aprovação na validação;
- 28 ultrapassaram 15% de falha por drawdown;
- 43 ultrapassaram USD 1.000 de drawdown P90.

## Melhor aproximação

O portfólio que mais se aproximou do portão na validação usava cinco micros:

- MES vendido em retomada da VWAP, RTH, score 5/6, volume relativo 1, alvo 1R;
- MNQ vendido em rejeição da VWAP, tendência da VWAP obrigatória, volume relativo
  1, alvo 2R;
- MNQ comprado em retorno à VWAP, score 4/6, volume relativo 0,8, alvo 1,5R.

Resultado:

| Métrica | Seleção | Validação |
|---|---:|---:|
| Aprovação das janelas | 95,24% | **50,00%** |
| Falha por drawdown | 0,00% | 0,00% |
| Drawdown P90 | USD 892,50 | USD 468,75 |

Ele ficou abaixo do mínimo de 60% na validação. Como existem apenas duas janelas
rolantes de 20 pregões dentro das 21 sessões de validação, 50% significa que uma
janela passou e a outra não. Relaxar o portão depois de observar esse resultado seria
ajustar a regra à resposta desejada.

## Controle de duplicidade

Configurações diferentes que produziram exatamente as mesmas operações na seleção
foram reduzidas a uma regra canônica antes da composição. Portfólios com operações
idênticas também foram deduplicados usando somente a seleção. Validação e teste não
participaram dessa escolha.

## Integridade do teste final

Durante uma execução preliminar do desenvolvimento, antes da inclusão do teto de
drawdown P90 e da escolha de um único campeão, o trecho final chegou a ser consultado.
Por isso:

- a execução canônica não abre o teste final, pois não há sobrevivente validado;
- o período de 26/06 a 29/07 está marcado como contaminado para pesquisas futuras;
- nenhum resultado desse período pode voltar a ser apresentado como teste inédito;
- a próxima confirmação realmente fora da amostra precisa usar dados posteriores a
  29/07.

Essa limitação foi preservada no JSON em vez de ser escondida.

## Limitações

- candles de cinco minutos não determinam a sequência intrabar;
- excursões não realizadas podem aumentar o drawdown real;
- custos são estimados e o slippage real pode ser maior;
- a busca de milhares de variantes aumenta o risco de sobreajuste;
- validação e teste possuem apenas 21 sessões;
- a correlação entre MNQ e MES continua relevante mesmo com uma posição por vez;
- backtest não garante resultado futuro.

## Próximo passo

Não alterar o indicador. O histórico disponível já pode ser usado para pesquisa e
desenho, mas deixou de possuir um trecho final inédito. Uma nova regra só poderá ser
promovida após:

1. seleção por walk-forward nos dados históricos;
2. congelamento integral da regra e da quantidade;
3. validação prospectiva com dados posteriores a 29/07;
4. aprovação no mesmo portão econômico sem relaxar limites.
