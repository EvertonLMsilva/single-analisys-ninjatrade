# Single Analysis NinjaTrader

Assistente visual de análise para NinjaTrader 8. A versão experimental identifica pullbacks a favor da tendência, calcula níveis hipotéticos e desenha entrada, stop e alvo no gráfico. O cruzamento de médias anterior continua registrado apenas como linha de comparação.

> **Segurança:** este projeto não envia ordens. Não existem chamadas de entrada, saída ou alteração de posições.

## Versão

Versão atual: `1.1.0-beta.1`. A versão em execução aparece no cabeçalho do painel do indicador.

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
- formato CSV v5 com identificação explícita de resultado hipotético;
- acompanhamento paralelo de 1R, 1,5R e 2R, sem mudar a entrada;
- primeiro evento e horários registrados, com ambiguidade preservada quando stop e alvo aparecem no mesmo candle;
- chave global por ativo e período, sem colisão entre MNQ e MES.
- rodada de validação `forward-2026-07-v1` com parâmetros congelados;
- MES com `EmaCrossBaseline` candidato visível em 1R e pullback pausado visualmente;
- MNQ com pullback visível em observação no alvo de 1,5R;
- resumo diário automático em CSV com resultado em R e moeda, sequência de stops e drawdown;
- bloqueio de novos sinais quando os parâmetros divergem da rodada congelada.
- CSV v7 sincronizado por dia para remover registros órfãos após reprocessamento;
- análise diagnóstica automática por direção, hora e faixa de risco.
- novo `ContextPullback` com VWAP de sessão, inclinação das EMAs, força do candle, distância em ATR e volume relativo;
- score contextual de 0 a 6, com critérios simétricos para compras e vendas;
- pullback anterior preservado silenciosamente como referência.
- `EvidencePullback` como único candidato visível: MNQ vendido, abaixo de VWAP descendente, score mínimo 4/6, risco até USD 50 e alvo de 1R;
- MES, compras e demais setups preservados somente para pesquisa silenciosa;
- seleção offline separada por período temporal e regra congelada por pelo menos dez resultados decididos.
- `QualifiedPullback` substitui o candidato visual anterior após o backtest de 149
  dias: MNQ vendido, score mínimo 5/6, distância máxima de 2 ATR da VWAP, volume
  relativo mínimo 1, risco entre USD 5 e USD 50, alvo 1,5R e validade de 12 candles;
- rodada `qualified-149d-2026-07-v5` exige pelo menos 20 resultados decididos em dez
  sessões, sem execução de ordens.

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
        │   ├── ComparisonStatus.cs
        │   ├── FirstOutcomeEvent.cs
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

O modo de validação vem ativado. Durante a rodada `qualified-149d-2026-07-v5`,
mantenha EMA 9/21, ATR 14, stop 1,5 ATR, tolerância 0,1 ATR, intervalo de 3 candles,
risco/retorno configurado em 2R, validade configurada em 3 candles, risco máximo de
USD 50 e política de descarte acima do limite. O candidato qualificado usa
internamente alvo 1,5R e validade de 12 candles, independentemente desses dois campos
gerais. Se a configuração-base divergir, o painel bloqueia novos sinais.

O histórico bruto CSV v8 fica salvo em `Documents/NinjaTrader 8/TradeAssistant/Data`. Os resumos diários `validation_v3` ficam em `Documents/NinjaTrader 8/TradeAssistant/Summaries`, e os segmentos `segments_v2` ficam em `Documents/NinjaTrader 8/TradeAssistant/Analysis`, sempre separados por ativo, período e dia. Os formatos anteriores permanecem preservados. A propriedade **Risco máximo por contrato** aceita um valor na moeda do ativo; use `0` apenas fora da rodada congelada. A propriedade **Política do limite** define se o indicador apenas avisa ou descarta o sinal acima desse valor.

A rodada qualificada começa em `2026-07-30`. Se o NinjaTrader recalcular dias
anteriores, eles serão marcados como `HistoricalReference` e `EligibleForReview=No`.

Os valores financeiros são estimativas para um contrato baseadas na distância dos níveis e no valor do ponto. Não incluem comissão, taxas, slippage ou conversão para a moeda da conta.

## Próximo marco

O `QualifiedPullback` permanece disponível somente para pesquisa visual. Ele foi
reprovado para o objetivo econômico de atingir USD 1.500 em até 20 pregões, mesmo
testando de 1 a 30 micros. O próximo marco é pesquisar candidatos pela probabilidade
de aprovação da avaliação, sem alterar o indicador até que um candidato passe pelo
novo portão econômico.

## Pesquisa offline

O backtest reproduzível para arquivos OHLCV de cinco minutos está em
`research/offline_backtest.py`. Ele não acessa o NinjaTrader, não envia ordens e
separa cronologicamente seleção, validação e teste final.

Exemplo:

```powershell
python research/offline_backtest.py `
  --mnq CAMINHO_DO_MNQ.csv `
  --mes CAMINHO_DO_MES.csv `
  --output research/results/resultado.json `
  --self-test
```

A primeira rodada de 58 dias não aprovou uma estratégia. A rodada ampliada de 149
dias qualificou um novo candidato vendido de MNQ somente para observação visual,
sem execução. Consulte
`docs/data-audits/2026-07-29-offline-strategy-backtest-149d.md`.

O simulador econômico está em `research/prop_evaluation.py`. Ele aplica a meta de
USD 1.500, o prazo de 20 pregões, o trailing drawdown, a consistência e quantidades
de 1 a 30 micros sobre todas as janelas históricas e sobre reamostragem em blocos.

```powershell
python research/prop_evaluation.py `
  --mnq CAMINHO_DO_MNQ.csv `
  --mes CAMINHO_DO_MES.csv `
  --output research/results/resultado-avaliacao.json `
  --self-test
```

O candidato qualificado foi reprovado no portão econômico. Consulte
`docs/data-audits/2026-07-29-prop-evaluation-20d.md`.

## Documentação

- [Arquitetura e limites](docs/architecture.md)
- [Processo obrigatório de desenvolvimento](docs/development-process.md)
- [Roteiro priorizado](docs/roadmap.md)
- [Protocolo de validação](docs/validation-protocol.md)
- [Registro cronológico do trabalho](docs/worklog.md)
- [Backtest offline de MNQ e MES](docs/data-audits/2026-07-29-offline-strategy-backtest.md)
- [Backtest offline ampliado de 149 dias](docs/data-audits/2026-07-29-offline-strategy-backtest-149d.md)
- [Simulação da avaliação de 25k em 20 pregões](docs/data-audits/2026-07-29-prop-evaluation-20d.md)
- [Auditoria inicial dos CSVs v2](docs/data-audits/2026-07-20-initial-v2-audit.md)
- [Auditoria dos CSVs v2 regenerados](docs/data-audits/2026-07-20-regenerated-v2-audit.md)
- [Análise de risco e alcance dos alvos](docs/data-audits/2026-07-20-risk-target-analysis.md)
- [Plano provisório de risco para Take Profit Trader 25k](docs/risk-plans/takeprofit-25k.md)
- [Diagnóstico do filtro de risco no MNQ](docs/data-audits/2026-07-20-mnq-risk-filter-behavior.md)
- [Validação do pullback v4 no MNQ](docs/data-audits/2026-07-20-mnq-pullback-v4-validation.md)
- [Análise consolidada do dia 20](docs/data-audits/2026-07-20-full-day-analysis.md)
- [Auditoria atualizada dos CSVs v4 em 21/07](docs/data-audits/2026-07-21-v4-log-audit.md)
- [Auditoria consolidada de todos os logs em 22/07](docs/data-audits/2026-07-22-all-logs-audit.md)
- [Seleção da rodada prospectiva a partir dos CSVs v5](docs/data-audits/2026-07-22-v5-forward-selection.md)
- [Decisão de testar pullback no MNQ](docs/decisions/0001-test-mnq-pullback.md)
- [Decisão de medir 1R, 1,5R e 2R](docs/decisions/0002-measure-multiple-targets.md)
- [Decisão de iniciar a validação prospectiva](docs/decisions/0003-start-forward-validation.md)
- [Decisão de encerrar a primeira rodada e iniciar o diagnóstico](docs/decisions/0004-close-forward-round.md)
- [Auditoria da primeira rodada prospectiva](docs/data-audits/2026-07-28-forward-v1-review.md)
- [Redesenho da operação a partir dos logs v7](docs/data-audits/2026-07-28-v7-strategy-redesign.md)
- [Decisão de testar o pullback contextual](docs/decisions/0005-test-context-pullback.md)
- [Histórico de versões](CHANGELOG.md)
