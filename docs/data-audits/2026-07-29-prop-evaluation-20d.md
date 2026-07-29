# Simulação da avaliação de 25k em 20 pregões

Data: 2026-07-29.

## Decisão

O `QualifiedPullback` está **reprovado economicamente** para o objetivo de atingir
USD 1.500 em no máximo 20 pregões.

O sinal pode continuar coletando evidência como pesquisa, mas não deve ser usado para
iniciar uma avaliação esperando aprovação dentro desse prazo.

## Objetivo obrigatório

- lucro líquido: USD 1.500;
- prazo: no máximo 20 pregões;
- drawdown trailing: USD 1.500;
- mínimo: cinco dias com operações;
- consistência: o melhor dia deve representar menos de 50% do lucro líquido;
- posição analisada: de 1 a 30 micros de MNQ;
- custo conservador: USD 5 por operação completa e por microcontrato.

Fontes das regras:

- https://takeprofittraderhelp.zendesk.com/hc/en-us/articles/15169070804125-Rule-1-Hit-Your-Profit-Target
- https://takeprofittraderhelp.zendesk.com/hc/en-us/articles/15170265979165-Rule-3-Do-Not-Hit-End-Of-Day-EOD-Maximum-Trailing-Drawdown
- https://takeprofittraderhelp.zendesk.com/hc/en-us/articles/15170316538013-Rule-5-Be-Consistent
- https://takeprofittraderhelp.zendesk.com/hc/en-us/articles/15169066911133-Rule-2-Do-Not-Exceed-Maximum-Position-Size

O limite de 20 pregões é um requisito econômico definido para este projeto. A
assinatura oficial não impõe esse prazo, mas a renovação mensal torna uma estratégia
muito lenta inadequada para o objetivo do operador.

## Dados e reconciliação

Foram reutilizados os históricos sincronizados de 149 dias:

- 28.941 candles de MNQ;
- 29.546 candles de MES usados como referência de alinhamento;
- 103 sessões válidas de MNQ;
- cinco sessões incompletas excluídas;
- 86 operações hipotéticas do candidato qualificado;
- resultado reconciliado em USD 442,50, PF 1,301 e drawdown de USD 205,25 para um
  microcontrato.

O simulador interrompe uma tentativa quando:

1. o saldo toca o piso trailing vigente;
2. a meta e a consistência são satisfeitas após pelo menos cinco dias ativos; ou
3. termina o vigésimo pregão.

O piso começa em -USD 1.500, sobe com o maior saldo de fim de dia e deixa de subir em
zero. A violação é verificada depois de cada operação hipotética, usando o piso
determinado pelo fechamento anterior.

## Cenários históricos

Existem 84 janelas rolantes e sobrepostas de 20 sessões no histórico válido.

| Micros | Risco técnico máximo por operação | Aprovação | Falha por drawdown | Melhor resultado final |
|---:|---:|---:|---:|---:|
| 1 | USD 50 | 0,00% | 0,00% | USD 340,25 |
| 3 | USD 150 | 0,00% | 0,00% | USD 1.020,75 |
| 5 | USD 250 | 7,14% | 0,00% | USD 1.701,25 |
| 8 | USD 400 | 17,86% | 0,00% | USD 2.722,00 |
| 9 | USD 450 | 19,05% | 0,00% | USD 3.062,25 |
| 10 | USD 500 | 19,05% | 11,90% | USD 3.402,50 |
| 18 | USD 900 | 19,05% | 66,67% | USD 6.124,50 |
| 30 | USD 1.500 | 14,29% | 77,38% | USD 10.207,50 |

O resultado final máximo não representa aprovação automática: uma tentativa pode
continuar acima da meta sem satisfazer cinco dias ativos ou a regra de consistência,
e pode ser eliminada posteriormente pelo piso trailing.

Pelo retorno histórico médio, seriam necessários 17,46 micros, arredondados para 18,
para projetar USD 1.500 em 20 sessões. Nessa quantidade, porém, 66,67% das janelas
históricas foram eliminadas pelo drawdown. Aumentar a posição não corrige a vantagem
pequena; apenas concentra o risco.

## Reamostragem em blocos

Foram executadas 10.000 reamostragens determinísticas para cada quantidade de
contratos, em blocos móveis de cinco pregões para preservar parte da sequência de
regimes.

| Micros | Aprovação estimada | Falha estimada por drawdown |
|---:|---:|---:|
| 1 | 0,00% | 0,00% |
| 3 | 0,97% | 0,00% |
| 5 | 9,67% | 1,46% |
| 8 | 21,12% | 13,40% |
| 9 | 23,07% | 19,20% |
| 10 | 25,19% | 24,21% |
| 18 | 25,13% | 52,38% |
| 30 | 21,74% | 64,83% |

Nenhuma das 30 quantidades atingiu simultaneamente:

- pelo menos 60% de aprovação histórica; e
- no máximo 15% de falha por drawdown.

## Interpretação

- um microcontrato é seguro em relação ao drawdown observado, mas não alcança a meta;
- cinco a nove micros começam a produzir aprovações isoladas, ainda muito abaixo do
  mínimo de confiabilidade;
- a partir de nove micros, a estimativa de quebra ultrapassa o limite aceito;
- 18 micros alcançam a média aritmética necessária, mas transformam a maioria das
  tentativas em reprovação;
- usar os 30 micros permitidos não é uma solução de risco.

## Limitações

- candles de cinco minutos não determinam a ordem intrabar;
- o piso é verificado pelo resultado fechado de cada operação; excursões não
  realizadas podem tornar a quebra real maior;
- quantidade de contratos foi escalada linearmente;
- custos e slippage real podem ser maiores;
- as 84 janelas rolantes se sobrepõem e não são independentes;
- bootstrap estima cenários e não prevê resultados futuros;
- o histórico é suficiente para reprovar este candidato para a meta, mas não para
  provar uma nova estratégia.

## Próximo passo

Redesenhar a pesquisa para selecionar estratégias pela probabilidade de aprovação,
e não apenas por resultado líquido e profit factor. Os próximos candidatos devem
combinar oportunidades independentes, permanecer separados entre seleção, validação
e teste final e passar o mesmo simulador antes de qualquer alteração no indicador.
