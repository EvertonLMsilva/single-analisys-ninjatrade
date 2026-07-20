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

## Estrutura

```text
NinjaTrader/
└── NinjaScript/
    └── TradeAssistant/
        ├── Analysis/
        │   └── SignalAnalyzer.cs
        ├── Models/
        │   ├── SignalDirection.cs
        │   └── TradeSignal.cs
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

Os parâmetros de EMA, ATR, risco/retorno e validade do sinal podem ser alterados na tela de propriedades da estratégia.

## Próximas etapas sugeridas

1. validar o desenho em Playback/Market Replay;
2. migrar uma única regra real do projeto antigo;
3. registrar o desfecho hipotético de cada sinal;
4. adicionar métricas somente depois que o primeiro setup estiver validado.

Consulte [docs/architecture.md](docs/architecture.md) para os limites desta base.
