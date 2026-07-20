# Single Analysis NinjaTrader

Base mínima de um assistente visual de análise para NinjaTrader 8. O código identifica cruzamentos de médias, calcula níveis hipotéticos com ATR e desenha entrada, stop e alvo no gráfico.

> **Segurança:** este projeto não envia ordens. Não existem chamadas de entrada, saída ou alteração de posições.

## Primeira entrega

- sinais visuais de compra e venda por cruzamento de EMA;
- entrada hipotética no fechamento do candle do sinal;
- stop calculado por múltiplo do ATR;
- alvo calculado pela relação risco/retorno;
- validade visual configurável em candles;
- painel fixo indicando `ANALYSIS ONLY`;
- análise, modelo de sinal e renderização separados.
- acompanhamento hipotético de alvo, stop, expiração e casos ambíguos;
- métricas em `R` exibidas no painel;
- zonas transparentes de risco e retorno;
- opção para limitar ou ocultar sinais antigos.

## Estrutura

```text
NinjaTrader/
└── NinjaScript/
    └── TradeAssistant/
        ├── Analysis/
        │   └── SignalAnalyzer.cs
        ├── Models/
        │   ├── SignalDirection.cs
        │   ├── SignalStatus.cs
        │   ├── SignalStatistics.cs
        │   ├── TrackedSignal.cs
        │   └── TradeSignal.cs
        ├── Tracking/
        │   └── SignalTracker.cs
        └── Indicators/
            └── TradeAnalysisAssistant.cs
docs/
└── architecture.md
```

## Instalação no NinjaTrader 8

1. Feche o NinjaScript Editor.
2. Copie a pasta `NinjaTrader/NinjaScript/TradeAssistant` para `Documents/NinjaTrader 8/bin/Custom/TradeAssistant`.
3. Abra o NinjaScript Editor e compile os scripts.
4. Em um gráfico, adicione o indicador **Trade Analysis Assistant**.
5. Mantenha-a em ambiente simulado enquanto valida os sinais e os parâmetros.

Os parâmetros de EMA, ATR, risco/retorno, validade e aparência podem ser alterados na tela de propriedades do indicador.

As métricas existem apenas durante a execução atual do indicador. Recarregar o gráfico reinicia a contagem.

## Próximas etapas sugeridas

1. validar o desenho em Playback/Market Replay;
2. migrar uma única regra real do projeto antigo;
3. registrar o desfecho hipotético de cada sinal;
4. adicionar métricas somente depois que o primeiro setup estiver validado.

Consulte [docs/architecture.md](docs/architecture.md) para os limites desta base.
