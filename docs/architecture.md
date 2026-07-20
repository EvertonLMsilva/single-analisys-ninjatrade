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
 TradeAnalysisAssistant
   (desenho no gráfico)
```

## Responsabilidades

- `SignalAnalyzer` recebe valores já calculados e cria um sinal imutável. Ele não conhece o gráfico nem a conta.
- `TradeSignal` transporta direção, entrada, stop, alvo, horário, validade e justificativa.
- `TradeAnalysisAssistant` é um indicador que lê EMA e ATR do NinjaTrader e apresenta o sinal no gráfico.

## Limite de segurança

O componente é um indicador exclusivamente visual. Ele não contém métodos de envio, cancelamento ou gerenciamento de ordens e nenhuma conta é acessada.

## Regra demonstrativa

O cruzamento de uma EMA rápida com uma EMA lenta existe apenas para validar a integração visual. Ele não representa uma recomendação operacional nem uma estratégia comprovada. A primeira migração do projeto antigo deve substituir essa regra de forma isolada e verificável.
