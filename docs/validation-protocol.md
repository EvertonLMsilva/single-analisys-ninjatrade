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

Esses parâmetros também acompanham cada linha do CSV quando se aplicam ao cálculo do sinal.

## 2. Teste funcional do CSV

1. compilar o NinjaScript com F5;
2. confirmar a versão e `Histórico CSV: ATIVO` no painel;
3. aguardar um sinal e localizar o arquivo em `Documents/NinjaTrader 8/TradeAssistant/Data`;
4. confirmar que a linha começa com situação `Active`;
5. avançar até o encerramento e confirmar que a mesma linha mudou de situação;
6. recarregar o gráfico e confirmar que a quantidade de linhas não aumentou indevidamente;
7. comparar entrada, stop e alvo do CSV com o gráfico.

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
