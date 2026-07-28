# Arquitetura inicial

O projeto começa com um fluxo propositalmente pequeno:

```text
Dados do candle e indicadores
            |
            v
      SignalAnalyzer
            |
            v
       TradeSignal
            |
            v
      SignalTracker
            |
            v
 TradeAnalysisAssistant
   (desenho no gráfico)
```

## Responsabilidades

- `SignalAnalyzer` recebe valores já calculados e cria um sinal imutável. Ele não conhece o gráfico nem a conta.
- `TradeSignal` transporta direção, entrada, stop, alvo, horário, validade e justificativa.
- `SignalTracker` acompanha o resultado hipotético, sem acessar conta ou enviar ordens.
- `TradeAnalysisAssistant` é um indicador que lê EMA e ATR do NinjaTrader e apresenta o sinal no gráfico.

O indicador também lê do instrumento carregado o tamanho do tick, o valor monetário do ponto e a moeda. Os níveis são arredondados para preços válidos e o risco financeiro mostrado corresponde a um contrato. Esses dados não alteram a conta e não definem quantidade de contratos. A estimativa não inclui comissão, taxas, slippage ou conversão para a moeda da conta.

## Critério de acompanhamento

O sinal começa a ser avaliado no candle seguinte à sua criação. O resultado principal continua usando o alvo configurado. Em paralelo, o rastreador acompanha 1R, 1,5R e 2R. Um alvo atingido em candle anterior permanece registrado mesmo que o stop seja tocado depois. Se alvo e stop forem tocados pela primeira vez no mesmo candle, o nível é classificado como ambíguo porque OHLC não revela a ordem intrabar. Se nenhum nível for tocado dentro da validade, ele expira.

Somente uma operação hipotética de cada setup pode permanecer ativa. `TrendPullback` e `EmaCrossBaseline` podem ser acompanhados ao mesmo tempo para permitir comparação sobre os mesmos dados, mas um novo sinal do mesmo setup só é aceito depois do encerramento anterior.

Quando a política de risco estiver em `DescartarAcimaDoLimite`, um sinal que ultrapasse o limite financeiro é registrado imediatamente como `RiskRejected`. Ele não se torna ativo, não bloqueia sinais futuros e não participa da taxa de acerto ou do resultado em R.

## Limite de segurança

O componente é um indicador exclusivamente visual. Ele não contém métodos de envio, cancelamento ou gerenciamento de ordens e nenhuma conta é acessada.

O painel e o CSV identificam os resultados como hipotéticos. A entrada é presumida no fechamento do candle do sinal e os eventos são inferidos pelas máximas e mínimas dos candles seguintes. Não são considerados preenchimento real, decisão do usuário, comissão, taxas ou slippage.

## Persistência v6

O CSV v6 preserva os formatos anteriores e adiciona rodada, etapa, alvo selecionado, tolerância e intervalo do pullback. A chave estável inclui todos os parâmetros congelados. Um segundo arquivo em `TradeAssistant/Summaries` consolida cada dia e setup com resultados em R e moeda, riscos médios, sequência de stops e drawdown. Custos permanecem identificados como não incluídos.

## Persistência v7 e diagnóstico

O CSV v7 mantém as colunas do v6, mas grava cada dia como um retrato completo dos sinais existentes na execução atual. Durante uma recarga histórica, o arquivo do dia é substituído pelo conjunto atual, impedindo que sinais que deixaram de ser gerados permaneçam como registros órfãos.

Os arquivos v6 não são alterados. O resumo `validation_v2` e a análise `segments_v1` também usam nomes novos. A análise segmentada agrupa cada setup por direção, hora do gráfico e faixa de risco financeiro (`0-25`, `25-50`, `50-75` e `75+`). Ela é diagnóstica, não altera a criação dos sinais e não representa aprovação operacional.

## Plano da versão 0.8

`ValidationPlan` mantém a classificação por ativo e setup sem alterar o analisador. `ValidationStatisticsCalculator` calcula as métricas do alvo escolhido, e `CsvValidationSummaryJournal` grava o resumo diário. O sinal bruto continua acompanhando 1R, 1,5R e 2R em paralelo.

No MES, o cruzamento de EMA é o candidato visível em 1R; o pullback fica pausado visualmente, mas continua coletado. No MNQ, o pullback permanece visível em observação com 1,5R; o cruzamento fica como referência silenciosa. Uma configuração diferente da rodada bloqueia apenas a criação de novos sinais; sinais já ativos ainda são atualizados para não perder seu desfecho.

## Setups da versão 0.7

`TrendPullback` é o experimento visível. Para compra, EMA rápida e lenta devem apontar para cima, o candle deve alcançar a região da EMA rápida dentro da tolerância configurada, fechar acima dela e fechar acima da abertura. A venda usa condições simétricas. A entrada hipotética fica no fechamento e o stop, um tick além do extremo do candle de confirmação. O sinal é rejeitado se esse stop técnico superar o limite financeiro.

`EmaCrossBaseline` conserva a regra anterior de cruzamento e stop por ATR. Ela não é desenhada no gráfico e existe somente para comparação no CSV. Nenhum dos dois setups representa uma estratégia comprovada ou recomendação operacional.
