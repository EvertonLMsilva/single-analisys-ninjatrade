# Estado atual do Trade Assistant

Atualizado em: 2026-08-03

Este arquivo e o ponto canonico de continuidade entre tarefas. Uma nova tarefa deve
le-lo antes do `worklog` ou de qualquer pesquisa antiga. Nao recuperar uma estrategia
descartada sem verificar as decisoes citadas aqui.

## Objetivo

Construir um assistente visual para NinjaTrader 8 que identifique oportunidades
hipoteticas durante a sessao, mostre entrada, stop, alvo, contexto e risco financeiro,
e grave resultados para validacao. O projeto nao pode enviar, alterar ou cancelar
ordens. Automacao e apenas uma possibilidade futura, condicionada a validacao e nova
autorizacao explicita.

O pedido atual e substituir a baixa cobertura do pullback por uma unica analise de
fluxo/impulso que aproveite variacao e volume com eficiencia, sem limitar o instrumento
carregado e sem declarar uma estrategia vencedora antes da evidencia.

## Repositorio e Git

- repositorio local: `D:\04-Projetos\17-single-analisys-ninjatrade`;
- remoto: `https://github.com/EvertonLMsilva/single-analisys-ninjatrade.git`;
- branch de continuidade: `agent/order-flow-experiment`;
- checkpoint funcional em desenvolvimento: `ecdb509`;
- base do checkpoint: `master` em `8da1c06`;
- PR anterior incorporado ao `master`: [PR #7](https://github.com/EvertonLMsilva/single-analisys-ninjatrade/pull/7);
- a branch experimental deve receber um novo PR em modo rascunho;
- ao iniciar, executar `git status -sb` e `git log -3 --oneline` e confirmar que a
  arvore esta limpa.

## Versao e instalacao

- versao declarada no repositorio: `1.3.1-beta.1`;
- versao declarada na instalacao: `1.3.1-beta.1`;
- pasta instalada:
  `C:\Users\evert\OneDrive\Documentos\NinjaTrader 8\bin\Custom\TradeAssistant`;
- no checkpoint, os cinco arquivos alterados/adicionados do experimento estavam
  sincronizados e com hash igual entre repositorio e instalacao;
- a versao ainda nao foi elevada para representar o experimento de delta/impulso;
- nao executar `dotnet build` na pasta ativa `NinjaTrader 8\bin\Custom`. A compilacao
  real deve ser confirmada com F5 no NinjaScript Editor.

## Estado publicado anterior

A versao `1.3.1-beta.1` do `master` usa `ContextPullback` em grafico de cinco minutos,
janela padrao 10:30-17:00 no fuso configurado no NinjaTrader, uma operacao hipotetica
ativa por grafico e CSV separado em `TradeAssistant\Realtime`. Ela aceita qualquer
instrumento com tick e valor do ponto validos, mas nenhum instrumento foi aprovado.

Esse pullback apresentou baixa cobertura e resultado insuficiente. Nao trata volume
total como direcao compradora ou vendedora.

## Experimento local preservado no checkpoint

O commit `ecdb509` preserva trabalho em andamento que ainda nao esta aprovado:

- `MarketData/MarketDeltaTracker.cs` classifica negocios em compra, venda ou
  desconhecido usando bid/ask e, como fallback, tick rule;
- `MarketData/MarketDeltaSnapshot.cs` calcula delta, volume classificado e percentual;
- `UniversalRealtimePlan.IsDeltaConfirmed` usa minimo de 25 unidades classificadas e
  delta absoluto minimo de 8%;
- o indicador recebe negocios em `OnMarketData` e mostra delta no painel;
- o pullback contextual fica desligado por padrao;
- o impulso contextual fica ligado por padrao e exige tendencia, rompimento do candle
  anterior, VWAP alinhada, corpo entre 0,30 e 1,30 ATR, volume relativo minimo 1,10,
  fechamento proximo ao extremo e distancia maxima de 1 ATR da EMA rapida;
- apesar do nome, `PassesRealtimeDeltaFilter` retorna sempre `true`. O delta e apenas
  coletado/exibido e nao bloqueia entradas enquanto o alinhamento temporal nao for
  validado;
- os CSVs atuais nao possuem colunas proprias para delta;
- o diff do indicador inclui grande reformatacao mecanica. Separar mudanca funcional
  de formatacao antes do PR final, se praticavel.

Portanto, nao afirmar que o codigo atual usa Order Flow para decidir entradas. Ele e
um prototipo de coleta com filtro deliberadamente desativado.

## Dados disponiveis

### Barras de um minuto

- MNQ:
  `C:\Users\evert\OneDrive\Área de Trabalho\NT_Optimizer_Data_MNQ_09-26_1Min_177d.csv`;
- cobertura MNQ: 2026-02-01 20:01 ate 2026-07-29 17:29;
- SHA-256 MNQ:
  `77EA1DB90EB8CDBD1833BA2B6C4C498751B8BA0C3FBE19B12AC35B5E6B50CA94`;
- MES:
  `C:\Users\evert\OneDrive\Área de Trabalho\NT_Optimizer_Data_MES_09-26_1Min_177d.csv`;
- cobertura MES: 2026-02-01 20:01 ate 2026-07-29 17:29;
- SHA-256 MES:
  `CBD8EA117052E25BE5B44C5D18701E938508DD2FAB0B4512CE57BD5165AA9EAE`.

Confirmar os caminhos com `Test-Path` em vez de reconstruí-los manualmente.

### Ticks locais

- pasta: `C:\Users\evert\OneDrive\Documentos\NinjaTrader 8\db\tick`;
- existem 89 arquivos para `MNQ 09-26`;
- cobertura nominal: `202607262000.Last.ncd` ate `202607301500.Last.ncd`;
- sao arquivos `Last`; nao presumir que contenham historico bid/ask classificado;
- nao foi encontrada cobertura equivalente de MES no checkpoint;
- a disponibilidade/licenca de barras `Volumetric` ainda nao foi confirmada pelo
  usuario.

## Resultados que nao podem ser esquecidos

### Logs universais reprocessados ate 31/07

Os arquivos em `TradeAssistant\Realtime` continham:

| Instrumento | Sinais | Periodo | Alvos | Stops | Expirados | Resultado |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| MES 09-26 | 65 | 01/06-31/07 | 4 | 14 | 24 | -6R |
| MNQ 09-26 | 63 | 01/06-31/07 | 2 | 12 | 17 | -8R |

Esses registros misturam pullback e impulso reprocessados e nao constituem amostra
prospectiva de delta. Nao aprovar nenhum dos dois instrumentos com esses numeros.

### Momentum MNQ de fechamento

A regra congelada `intraday-momentum-2026-07-v1` teve, na amostra ampliada:

- 100 operacoes;
- +USD 1.633 por micro;
- media de USD 16,33 por operacao;
- PF 1,4746;
- drawdown de USD 621;
- julho negativo em USD 194,50;
- mesma regra no MES: -USD 204,73 e PF 0,7075.

Ela e historicamente positiva apenas no MNQ, opera perto do fechamento e nao passou o
portao economico da avaliacao. Com dimensionamento limitado a USD 300 (2 micros em alta
volatilidade e 4 em baixa), passou em 45,68% das janelas de 20 pregoes; P90 de drawdown
foi USD 1.223. Nao e resposta aprovada para a meta.

### Modelos de evento e volume

O modelo anterior avaliou 5.389 eventos com volume relativo, VWAP, candle, momentum e
volatilidade. O melhor observado perdeu USD 145,50 na confirmacao, ficou em -USD 37
fora da amostra e teve PF 0,9503. O melhor dimensionamento atingiu somente 28,57% de
aprovacao. Volume agregado e variacao, isoladamente, nao provaram vantagem.

## Regras da conta e objetivo economico

- mesa informada: TakeProfit;
- tamanho: 25k;
- meta: USD 1.500 em no maximo 20 pregoes;
- drawdown/trailing maximo informado: USD 1.500;
- usuario esclareceu que nao ha limite diario formal informado;
- foi estudado um teto operacional de USD 300, mas nao foi aprovado como regra final;
- existe preocupacao de perder a avaliacao apos mais de cinco dias uteis sem operar;
- risco por operacao ainda nao foi definido;
- qualquer simulacao deve incluir custos, janelas de 20 pregoes, falha por drawdown,
  dias ativos e consistencia aplicavel a conta real usada.

## Seguranca obrigatoria

- nenhuma chamada `EnterLong`, `EnterShort`, `ExitLong`, `ExitShort`, `SubmitOrder`,
  `ChangeOrder`, `CancelOrder`, `Account.CreateOrder` ou `AtmStrategyCreate`;
- nenhuma leitura ou manipulacao de conta;
- resultado deve continuar identificado como hipotetico e sem ordens;
- nao transformar aumento de frequencia em alegacao de eficiencia ou lucro;
- nao implementar automacao sem nova autorizacao explicita e marcos de validacao;
- preservar ambiguidades quando stop e alvo ocorrem no mesmo candle;
- nao otimizar parametros usando o bloco final que sera usado como confirmacao.

## Validacoes executadas no checkpoint

- `dotnet run --project tests/TradeAssistant.Core.Tests/TradeAssistant.Core.Tests.csproj`:
  aprovado;
- varredura pelas APIs proibidas de ordens: nenhuma ocorrencia;
- limitacao: o projeto de testes nao inclui ainda `MarketData/*.cs`, portanto o coletor
  de delta nao foi coberto por esses testes;
- limitacao: a compilacao F5 do checkpoint experimental nao foi confirmada nesta tarefa;
- limitacao: o filtro de delta permanece desativado.

## Proximo passo exato

1. Confirmar branch `agent/order-flow-experiment` e ler este arquivo integralmente.
2. Inspecionar o commit `ecdb509` sem reformatar novamente o indicador.
3. Adicionar `MarketData/*.cs` ao projeto de testes e criar testes para classificacao
   bid/ask, fallback tick rule, fronteiras de candle, concorrencia basica e limiar de
   confirmacao.
4. Verificar no NinjaTrader se `Volumetric` esta disponivel e se o provedor entrega
   historico bid/ask. O usuario nao sabe localizar essa opcao; orientar com passos
   simples ou testar via compilacao controlada.
5. Corrigir e validar o alinhamento entre `OnMarketData`, o horario final do candle de
   cinco minutos e `Time[0]`.
6. Criar persistencia separada para snapshots de delta antes de ativar o filtro.
7. Manter somente o impulso contextual como candidato visual durante esse experimento;
   pullback desligado e momentum de fechamento nao misturado no painel.
8. Coletar dados prospectivos. Nao existe historico suficiente para chamar delta de
   vencedor.
9. Somente depois de validar a coleta, testar regra congelada em blocos cronologicos e
   comparar com meta de USD 1.500/20 dias e drawdown de USD 1.500.
10. Elevar a versao, atualizar changelog/decisao, sincronizar, compilar com F5 e abrir
    PR rascunho apenas quando o incremento estiver verificavel.

## Arquivos para ler na nova tarefa

Ler, nesta ordem:

1. `docs/CURRENT_STATE.md`;
2. `docs/development-process.md`;
3. `docs/decisions/0018-add-universal-realtime-observation.md`;
4. `docs/decisions/0017-retain-frozen-momentum-after-177d.md`;
5. somente se precisar dos numeros completos:
   `docs/data-audits/2026-07-29-alternative-market-methods-177d.md` e
   `docs/data-audits/2026-07-29-trigger-and-event-model-search.md`.

Nao e necessario ler toda a conversa anterior nem todos os arquivos de pesquisa para
iniciar.
