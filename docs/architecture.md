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

O sinal começa a ser avaliado no candle seguinte ao cruzamento. O primeiro toque em alvo ou stop encerra o acompanhamento. Se ambos forem tocados no mesmo candle, o resultado é classificado como ambíguo e não entra na taxa de acerto. Se nenhum nível for tocado dentro da validade configurada, o sinal expira.

Somente uma operação hipotética de cada setup pode permanecer ativa. `TrendPullback` e `EmaCrossBaseline` podem ser acompanhados ao mesmo tempo para permitir comparação sobre os mesmos dados, mas um novo sinal do mesmo setup só é aceito depois do encerramento anterior.

Quando a política de risco estiver em `DescartarAcimaDoLimite`, um sinal que ultrapasse o limite financeiro é registrado imediatamente como `RiskRejected`. Ele não se torna ativo, não bloqueia sinais futuros e não participa da taxa de acerto ou do resultado em R.

## Limite de segurança

O componente é um indicador exclusivamente visual. Ele não contém métodos de envio, cancelamento ou gerenciamento de ordens e nenhuma conta é acessada.

## Setups da versão 0.6

`TrendPullback` é o experimento visível. Para compra, EMA rápida e lenta devem apontar para cima, o candle deve alcançar a região da EMA rápida dentro da tolerância configurada, fechar acima dela e fechar acima da abertura. A venda usa condições simétricas. A entrada hipotética fica no fechamento e o stop, um tick além do extremo do candle de confirmação. O sinal é rejeitado se esse stop técnico superar o limite financeiro.

`EmaCrossBaseline` conserva a regra anterior de cruzamento e stop por ATR. Ela não é desenhada no gráfico e existe somente para comparação no CSV. Nenhum dos dois setups representa uma estratégia comprovada ou recomendação operacional.
