# Protocolo de validação

Este protocolo evita comparar resultados produzidos com configurações diferentes ou tirar conclusões a partir de poucos sinais.

## 1. Identificação da execução

Antes de iniciar o Playback, registrar:

- versão do indicador;
- ativo e vencimento do contrato;
- tipo e período do gráfico;
- modelo de horário de negociação;
- fuso horário exibido no NinjaTrader;
- período analisado;
- EMA rápida e lenta;
- período do ATR e multiplicador do stop;
- relação risco/retorno;
- validade do sinal em candles.
- tolerância do pullback em ATR;
- intervalo mínimo entre pullbacks da mesma direção;
- situação das opções de pullback e comparação EMA.

Esses parâmetros também acompanham cada linha do CSV quando se aplicam ao cálculo do sinal.

## 2. Teste funcional do CSV

1. compilar o NinjaScript com F5;
2. confirmar a versão e `Histórico CSV: ATIVO` no painel;
3. aguardar um sinal e localizar o arquivo em `Documents/NinjaTrader 8/TradeAssistant/Data`;
4. confirmar que a linha começa com situação `Active`;
5. avançar até o encerramento e confirmar que a mesma linha mudou de situação;
6. recarregar o gráfico e confirmar que a quantidade de linhas não aumentou indevidamente;
7. comparar entrada, stop e alvo do CSV com o gráfico.
8. confirmar que tick, valor do ponto e moeda correspondem ao instrumento carregado;
9. conferir manualmente `risco em pontos × valor do ponto = risco financeiro`;
10. configurar um limite abaixo e acima do risco calculado e conferir a mudança de situação.
11. no modo `DescartarAcimaDoLimite`, confirmar que o sinal acima do limite aparece como descartado, não cria zonas operacionais e não bloqueia o próximo sinal;
12. confirmar no CSV v6 os campos `Setup`, `RiskLimitMode`, `RiskLimitStatus` e `Status=RiskRejected`;
13. confirmar que apenas `TrendPullback` é desenhado no gráfico e que `EmaCrossBaseline` aparece somente no CSV;
14. confirmar que os dois setups podem estar ativos ao mesmo tempo, mas não existem duas operações ativas do mesmo setup.
15. confirmar `EvaluationType=Hypothetical`, `EntryAssumption=SignalBarClose` e `OutcomeBasis=FollowingBarsHighLow`;
16. confirmar preços, situações e horários de 1R, 1,5R e 2R;
17. criar um cenário em que 1R ocorre antes do stop e confirmar `FirstEvent=Target1R`;
18. criar um cenário em que 1R e stop aparecem no mesmo candle e confirmar `Target1RStatus=Ambiguous`;
19. confirmar que `RecordKey` começa com ativo e período e não colide entre MNQ e MES.
20. confirmar `ValidationRound=forward-2026-07-v1`, etapa e alvo de validação;
21. confirmar que o MES mostra o cruzamento EMA em 1R e não desenha o pullback;
22. confirmar que o MNQ mostra o pullback em 1,5R e mantém o cruzamento somente no CSV;
23. confirmar a criação do resumo em `TradeAssistant/Summaries`;
24. alterar temporariamente um parâmetro e confirmar o aviso de configuração divergente e o bloqueio de novos sinais; depois restaurar o valor congelado.

## 3. Coleta da linha de base

Usar sempre a mesma configuração durante uma rodada. Como primeira amostra de trabalho, coletar pelo menos 20 pregões e buscar pelo menos 100 sinais. Esses números são um ponto de partida para reduzir conclusões precipitadas, não uma garantia de validade estatística ou rentabilidade.

Não excluir manualmente:

- stops;
- sinais expirados;
- resultados ambíguos;
- dias de baixa atividade;
- períodos que visualmente parecem desfavoráveis.

## 4. Métricas mínimas

- total de sinais;
- alvos, stops, expirados e ambíguos;
- taxa de acerto entre resultados decididos: `alvos / (alvos + stops)`;
- resultado líquido em R;
- resultado médio em R por sinal encerrado;
- MFE médio e máximo;
- MAE médio e máximo;
- distribuição por horário e direção;
- quantidade de sinais por pregão.

Expirados e ambíguos devem permanecer visíveis no relatório, mesmo quando não entrarem em determinada fórmula.

## 5. Critérios para comparar mudanças

Uma nova regra ou configuração deve ser avaliada sobre o mesmo conjunto de dias da linha de base. A comparação deve registrar:

- o que mudou;
- hipótese da mudança;
- versão anterior e nova;
- diferença na quantidade de sinais;
- diferença nas métricas;
- efeitos indesejados observados;
- decisão: aceitar, ajustar ou descartar.

Não alterar vários componentes da regra na mesma comparação.

Na primeira rodada da versão 0.7, manter `Tolerância do pullback = 0,1 ATR`, `Intervalo entre pullbacks = 3`, validade de 3 candles, risco máximo de USD 75 e um microcontrato de referência. Separar MNQ e MES e comparar os níveis pelas colunas próprias, sem alterar a entrada durante a coleta.

## 6. Rodada prospectiva 0.8

A rodada `forward-2026-07-v1` começa na primeira sessão completa após a instalação da versão `0.8.0-beta.1`. Não misturar os resultados anteriores com a decisão prospectiva.

- manter cinco sessões completas sem alteração de parâmetros;
- exigir pelo menos 30 resultados decididos por candidato para a primeira revisão;
- MES `EmaCrossBaseline`: candidato em 1R;
- MES `TrendPullback`: pausado visualmente, coleta silenciosa em 1R;
- MNQ `TrendPullback`: observação em 1,5R;
- MNQ `EmaCrossBaseline`: referência silenciosa em 1R;
- avaliar R e dólares em conjunto;
- rejeitar conclusão sustentada por apenas um dia;
- estimar comissão e slippage antes de qualquer etapa posterior.

Mesmo que a primeira revisão seja favorável, o objetivo maior permanece 20 sessões e 100 oportunidades antes de considerar encerrada a validação. Nenhum desses critérios autoriza automaticamente operação real.
