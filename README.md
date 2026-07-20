# Single Analysis NinjaTrader

Base mínima de um assistente visual de análise para NinjaTrader 8. O código identifica cruzamentos de médias, calcula níveis hipotéticos com ATR e desenha entrada, stop e alvo no gráfico.

> **Segurança:** este projeto não envia ordens. Não existem chamadas de entrada, saída ou alteração de posições.

## Versão

Versão atual: `0.3.0-beta.1`. A versão em execução aparece no cabeçalho do painel do indicador.

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
- histórico automático em CSV, separado por ativo, período gráfico e dia;
- atualização do mesmo registro quando o sinal atinge alvo, stop ou expira;
- chave estável para evitar sinais duplicados ao recarregar o gráfico.

## Estrutura

```text
NinjaTrader/
└── NinjaScript/
    └── TradeAssistant/
        ├── Analysis/
        │   └── SignalAnalyzer.cs
        ├── Configuration/
        │   └── TradeAssistantVersion.cs
        ├── Models/
        │   ├── SignalDirection.cs
        │   ├── SignalStatus.cs
        │   ├── SignalStatistics.cs
        │   ├── TrackedSignal.cs
        │   └── TradeSignal.cs
        ├── Persistence/
        │   └── CsvSignalJournal.cs
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

As métricas do painel existem apenas durante a execução atual do indicador. O histórico em CSV permanece salvo em `Documents/NinjaTrader 8/TradeAssistant/Data` e pode ser desligado pela propriedade **Salvar histórico CSV**.

## Próximas etapas sugeridas

1. validar o desenho em Playback/Market Replay;
2. migrar uma única regra real do projeto antigo;
3. registrar o desfecho hipotético de cada sinal;
4. adicionar métricas somente depois que o primeiro setup estiver validado.

Consulte [docs/architecture.md](docs/architecture.md) para os limites desta base.
