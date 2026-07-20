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

## Critério de acompanhamento

O sinal começa a ser avaliado no candle seguinte ao cruzamento. O primeiro toque em alvo ou stop encerra o acompanhamento. Se ambos forem tocados no mesmo candle, o resultado é classificado como ambíguo e não entra na taxa de acerto. Se nenhum nível for tocado dentro da validade configurada, o sinal expira.

## Limite de segurança

O componente é um indicador exclusivamente visual. Ele não contém métodos de envio, cancelamento ou gerenciamento de ordens e nenhuma conta é acessada.

## Regra demonstrativa

O cruzamento de uma EMA rápida com uma EMA lenta existe apenas para validar a integração visual. Ele não representa uma recomendação operacional nem uma estratégia comprovada. A primeira migração do projeto antigo deve substituir essa regra de forma isolada e verificável.
