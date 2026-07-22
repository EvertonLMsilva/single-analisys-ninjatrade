# Single Analysis NinjaTrader

Assistente visual de análise para NinjaTrader 8. A versão experimental identifica pullbacks a favor da tendência, calcula níveis hipotéticos e desenha entrada, stop e alvo no gráfico. O cruzamento de médias anterior continua registrado apenas como linha de comparação.

> **Segurança:** este projeto não envia ordens. Não existem chamadas de entrada, saída ou alteração de posições.

## Versão

Versão atual: `0.6.0-beta.1`. A versão em execução aparece no cabeçalho do painel do indicador.

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
- bloqueio de novos sinais enquanto houver uma operação hipotética ativa.
- cálculo de distância em pontos e ticks conforme o ativo carregado;
- estimativa de risco e alvo financeiro para um contrato;
- limite financeiro configurável e situação exibida no painel;
- preços de entrada, stop e alvo ajustados ao tick válido do instrumento.
- política configurável para avisar ou descartar sinais acima do risco máximo;
- perfil inicial da avaliação Take Profit Trader 25k com limite de USD 75;
- registro de sinais descartados no CSV sem tratá-los como operações ativas.
- setup experimental de pullback com confirmação por candle e stop no extremo técnico;
- tolerância até a EMA rápida e intervalo mínimo entre candidatos configuráveis;
- linha de base por cruzamento de EMA executada em paralelo, sem poluir o gráfico;
- acompanhamento independente para cada setup;
- formato CSV v4 com identificação do setup em cada registro.

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
        │   ├── SignalSetup.cs
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
├── architecture.md
├── development-process.md
├── roadmap.md
├── validation-protocol.md
└── worklog.md
```

## Instalação no NinjaTrader 8

1. Feche o NinjaScript Editor.
2. Copie a pasta `NinjaTrader/NinjaScript/TradeAssistant` para `Documents/NinjaTrader 8/bin/Custom/TradeAssistant`.
3. Abra o NinjaScript Editor e compile os scripts.
4. Em um gráfico, adicione o indicador **Trade Analysis Assistant**.
5. Mantenha-a em ambiente simulado enquanto valida os sinais e os parâmetros.

Os parâmetros de EMA, tolerância do pullback, intervalo entre candidatos, ATR, risco/retorno, validade e aparência podem ser alterados na tela de propriedades do indicador. Os valores experimentais iniciais são `0,1 ATR` de tolerância e `3 candles` de intervalo mínimo por direção.

As métricas do painel existem apenas durante a execução atual do indicador. O histórico em CSV permanece salvo em `Documents/NinjaTrader 8/TradeAssistant/Data` e pode ser desligado pela propriedade **Salvar histórico CSV**. A propriedade **Risco máximo por contrato** aceita um valor na moeda do ativo; use `0` para manter o limite desativado. A propriedade **Política do limite** define se o indicador apenas avisa ou descarta o sinal acima desse valor.

Os valores financeiros são estimativas para um contrato baseadas na distância dos níveis e no valor do ponto. Não incluem comissão, taxas, slippage ou conversão para a moeda da conta.

## Próximo marco

Compilar `0.6.0-beta.1` no NinjaTrader e coletar os setups `TrendPullback` e `EmaCrossBaseline` sobre os mesmos dias. Nenhuma conclusão operacional deve ser tomada antes da amostra mínima definida no protocolo.

## Documentação

- [Arquitetura e limites](docs/architecture.md)
- [Processo obrigatório de desenvolvimento](docs/development-process.md)
- [Roteiro priorizado](docs/roadmap.md)
- [Protocolo de validação](docs/validation-protocol.md)
- [Registro cronológico do trabalho](docs/worklog.md)
- [Auditoria inicial dos CSVs v2](docs/data-audits/2026-07-20-initial-v2-audit.md)
- [Auditoria dos CSVs v2 regenerados](docs/data-audits/2026-07-20-regenerated-v2-audit.md)
- [Análise de risco e alcance dos alvos](docs/data-audits/2026-07-20-risk-target-analysis.md)
- [Plano provisório de risco para Take Profit Trader 25k](docs/risk-plans/takeprofit-25k.md)
- [Diagnóstico do filtro de risco no MNQ](docs/data-audits/2026-07-20-mnq-risk-filter-behavior.md)
- [Validação do pullback v4 no MNQ](docs/data-audits/2026-07-20-mnq-pullback-v4-validation.md)
- [Análise consolidada do dia 20](docs/data-audits/2026-07-20-full-day-analysis.md)
- [Auditoria atualizada dos CSVs v4 em 21/07](docs/data-audits/2026-07-21-v4-log-audit.md)
- [Auditoria consolidada de todos os logs em 22/07](docs/data-audits/2026-07-22-all-logs-audit.md)
- [Decisão de testar pullback no MNQ](docs/decisions/0001-test-mnq-pullback.md)
- [Histórico de versões](CHANGELOG.md)
