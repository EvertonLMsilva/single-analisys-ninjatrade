# Análise consolidada do dia 2026-07-20

> **Relatório superado:** os arquivos de 2026-07-20 foram regenerados e ampliados em 2026-07-21. As conclusões atualizadas estão em `2026-07-21-v4-log-audit.md`. Este documento permanece preservado para registrar o estado parcial observado anteriormente.

## Escopo

Foram analisados os arquivos CSV v4 do MNQ 09-26 e MES 09-26 em gráfico de 5 minutos. Os horários citados são os horários gravados pelo gráfico do NinjaTrader; o fuso não foi inferido.

## Resumo por ativo e setup

| Ativo | Setup | Candidatos | Aceitos | Rejeitados | Alvos | Stops | Expirados | Ambíguos | Resultado |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|
| MNQ | EmaCrossBaseline | 10 | 0 | 10 | 0 | 0 | 0 | 0 | 0R |
| MNQ | TrendPullback | 35 | 23 | 12 | 3 | 9 | 10 | 1 | -3R |
| MES | EmaCrossBaseline | 11 | 11 | 0 | 1 | 2 | 8 | 0 | 0R |
| MES | TrendPullback | 40 | 39 | 1 | 2 | 12 | 25 | 0 | -8R |

Sem comissão, taxas ou slippage, o resultado hipotético do pullback foi aproximadamente `-USD 285,50` no MNQ e `-USD 198,75` no MES.

## O que o dia permite definir

1. O cruzamento com stop por ATR não é compatível com o orçamento atual do MNQ: os 10 candidatos ficaram acima de USD 75.
2. O pullback melhora a viabilidade financeira do MNQ: 23 de 35 candidatos ficaram dentro do limite.
3. Viabilidade não significou qualidade suficiente: no MNQ houve três alvos contra nove stops e resultado de -3R.
4. O MES não foi uma alternativa melhor neste dia: o pullback encerrou em -8R.
5. O limite de USD 75 não deve ser aumentado para tentar aceitar os sinais rejeitados.
6. A parada operacional pessoal de USD 225 permanece adequada. No MNQ, o acumulado teria passado de -USD 225 quando o sinal das 14:40 encerrou às 14:55, chegando a aproximadamente -USD 230,50. Parar nesse ponto teria evitado dois stops posteriores e encerrado o dia melhor que os -USD 285,50 finais.
7. O setup deve permanecer experimental e não deve ser usado como autorização automática de entrada.

## Hipóteses reveladas pelo dia

Todos os três alvos do MNQ ocorreram entre 06:20 e 08:35 no horário do gráfico. A faixa de 06:00 a 08:59 produziu +5R, enquanto o restante do dia produziu -8R. Isso é uma hipótese de janela de maior qualidade, não um filtro aprovado, pois representa somente um dia.

Dos dez sinais expirados no MNQ, cinco alcançaram pelo menos 1R de MFE sem alcançar o alvo de 2R dentro da validade. Alguns sinais que terminaram em stop também passaram de 1R favorável antes de reverter. Isso indica que alvo e gerenciamento podem ser gargalos tão importantes quanto a entrada.

## Próxima rodada recomendada

Não alterar a entrada nem aumentar o risco. A próxima mudança deve ser apenas de medição:

- acompanhar separadamente 1R, 1,5R e 2R;
- registrar qual nível foi atingido primeiro em relação ao stop;
- manter a validade atual como referência;
- manter USD 75 por operação e adicionar a observação da parada diária pessoal em USD 225;
- avaliar a faixa de 06:00 a 08:59 sobre os demais dias antes de criar qualquer filtro de horário.

Depois dessa medição, decidir entre ajustar o alvo, testar gerenciamento após 1R ou descartar o setup. Não combinar essas mudanças na mesma rodada.
