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

## Fronteira para execução futura

A execução futura não será incorporada diretamente ao `SignalAnalyzer` nem ao indicador
visual. A separação planejada é:

```text
SignalAnalyzer -> TradeSignal -> Simulação/Confirmação -> Adaptador permitido
```

O adaptador permitido deverá validar ambiente, conta, horário, risco, posição existente e
duplicidade imediatamente antes de qualquer envio. Na ausência de confirmação ou diante de
qualquer divergência, o estado padrão será não enviar.

A primeira implementação será exclusivamente simulada. A confirmação manual será uma etapa
separada. Automação real somente poderá existir em conta própria ou programa que permita
bots explicitamente; a conta PRO da Take Profit Trader deverá permanecer bloqueada enquanto
a regra oficial atual proibir bots e algos.

## Persistência v6

O CSV v6 preserva os formatos anteriores e adiciona rodada, etapa, alvo selecionado, tolerância e intervalo do pullback. A chave estável inclui todos os parâmetros congelados. Um segundo arquivo em `TradeAssistant/Summaries` consolida cada dia e setup com resultados em R e moeda, riscos médios, sequência de stops e drawdown. Custos permanecem identificados como não incluídos.

## Persistência v7 e diagnóstico

O CSV v7 mantém as colunas do v6, mas grava cada dia como um retrato completo dos sinais existentes na execução atual. Durante uma recarga histórica, o arquivo do dia é substituído pelo conjunto atual, impedindo que sinais que deixaram de ser gerados permaneçam como registros órfãos.

Os arquivos v6 não são alterados. O resumo `validation_v2` e a análise `segments_v1` também usam nomes novos. A análise segmentada agrupa cada setup por direção, hora do gráfico e faixa de risco financeiro (`0-25`, `25-50`, `50-75` e `75+`). Ela é diagnóstica, não altera a criação dos sinais e não representa aprovação operacional.

## Contexto da versão 0.9

`ContextPullback` começa pelo mesmo candidato de pullback do setup anterior, mas exige contexto adicional. A VWAP é calculada por sessão com preço típico dos candles ponderado por volume e reiniciada no primeiro candle do template de horário configurado no gráfico. É uma aproximação por candle, não a implementação tick a tick do Order Flow VWAP.

O contexto registra e pontua seis componentes: lado da VWAP, inclinação da VWAP, inclinação das EMAs, qualidade do candle, distância máxima da VWAP e volume relativo. Lado da VWAP, inclinação, EMAs, candle e extensão são obrigatórios; o score mínimo é 5 de 6. As condições são invertidas de forma simétrica entre compra e venda.

O pullback-base continua sendo acompanhado sem desenho para permitir comparação direta. O CSV v8 grava VWAP, inclinações normalizadas por ATR, máxima e mínima da sessão, corpo e localização do fechamento do candle, volume relativo, score, aprovação e justificativa. O resumo `validation_v3` e os segmentos `segments_v2` preservam os formatos anteriores e acrescentam agrupamento por score.

## Candidato qualificado da versão 1.1

`QualifiedPullback` reutiliza o gatilho vendido do pullback-base e o contexto já
registrado, mas congela a regra selecionada no backtest de 149 dias:

- somente MNQ vendido;
- score mínimo 5 de 6;
- distância máxima de 2 ATR até a VWAP;
- volume relativo mínimo 1;
- risco técnico entre USD 5 e USD 50;
- alvo de 1,5R;
- validade de 12 candles.

O alvo e a validade do setup são constantes da rodada e não dependem dos campos gerais
do indicador. Apenas um `QualifiedPullback` pode permanecer ativo. Ele é o único setup
desenhado no modo de validação; `EvidencePullback` e os demais continuam registrados
silenciosamente para preservar comparações. Não existe integração com conta ou envio
de ordens.

## Plano da versão 0.8

`ValidationPlan` mantém a classificação por ativo e setup sem alterar o analisador. `ValidationStatisticsCalculator` calcula as métricas do alvo escolhido, e `CsvValidationSummaryJournal` grava o resumo diário. O sinal bruto continua acompanhando 1R, 1,5R e 2R em paralelo.

No MES, o cruzamento de EMA é o candidato visível em 1R; o pullback fica pausado visualmente, mas continua coletado. No MNQ, o pullback permanece visível em observação com 1,5R; o cruzamento fica como referência silenciosa. Uma configuração diferente da rodada bloqueia apenas a criação de novos sinais; sinais já ativos ainda são atualizados para não perder seu desfecho.

## Setups da versão 0.7

`TrendPullback` é o experimento visível. Para compra, EMA rápida e lenta devem apontar para cima, o candle deve alcançar a região da EMA rápida dentro da tolerância configurada, fechar acima dela e fechar acima da abertura. A venda usa condições simétricas. A entrada hipotética fica no fechamento e o stop, um tick além do extremo do candle de confirmação. O sinal é rejeitado se esse stop técnico superar o limite financeiro.

`EmaCrossBaseline` conserva a regra anterior de cruzamento e stop por ATR. Ela não é desenhada no gráfico e existe somente para comparação no CSV. Nenhum dos dois setups representa uma estratégia comprovada ou recomendação operacional.
