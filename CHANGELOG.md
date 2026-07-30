# Histórico de versões

## 1.3.1-beta.1 — 2026-07-30

- todos os sinais universais dos dias carregados permanecem desenhados no gráfico;
- removido o limite visual antigo de cinco ou vinte sinais no modo universal;
- painel identifica uma única estratégia visível: `PULLBACK CONTEXTUAL`;
- painel informa explicitamente que nenhum ativo está aprovado nessa estratégia;
- comparação EMA continua desativada por padrão e momentum não é misturado ao modo
  universal;
- mantida a avaliação a cada fechamento de candle de 5 minutos, sem ordens.

## 1.3.0-beta.1 — 2026-07-30

- adicionado modo universal experimental para qualquer instrumento carregado;
- avaliação acontece durante a sessão, no fechamento de cada candle de 5 minutos;
- compras e vendas exigem pullback contextual com EMA, VWAP, ATR, candle e volume;
- entrada, stop, alvo e valor financeiro usam tick e valor do ponto do ativo;
- bloqueada sobreposição de sinais universais no mesmo gráfico;
- painel mostra ativo, período, janela, estado atual e resultado hipotético diário;
- registros universais isolados em `TradeAssistant/Realtime`, com rodada própria;
- candidato de momentum MNQ preservado, mas desligado por padrão no modo universal;
- mantida a ausência total de chamadas de execução de ordens.

## Pesquisa de portfólios — 2026-07-29 (sem alteração de versão)

- adicionadas rejeição da VWAP, rompimento da abertura e retorno à VWAP à pesquisa
  offline, além das três famílias existentes;
- pesquisadas compras e vendas em MNQ e MES, com uma única posição hipotética por
  vez;
- avaliados 4.224 componentes e 1.099 portfólios únicos de dois ou três setups;
- removidas configurações e composições que produziam operações idênticas;
- adicionado teto obrigatório de USD 1.000 para o drawdown P90;
- 60 portfólios passaram na seleção e nenhum passou na validação;
- trecho final marcado como contaminado após acesso durante desenvolvimento
  preliminar e bloqueado na execução canônica;
- todos os candidatos reprovados e indicador mantido sem alteração.

## Pesquisa econômica — 2026-07-29 (sem alteração de versão)

- criado simulador da avaliação de 25k com meta de USD 1.500 em até 20 pregões;
- aplicados trailing drawdown de USD 1.500, cinco dias ativos e consistência abaixo
  de 50%;
- avaliadas todas as 84 janelas históricas e 10.000 reamostragens em blocos para
  cada quantidade de 1 a 30 micros;
- nenhuma quantidade atingiu o portão de 60% de aprovação e no máximo 15% de falha
  por drawdown;
- `QualifiedPullback` reprovado como estratégia principal da avaliação e preservado
  somente para pesquisa;
- indicador, versão e comportamento no gráfico mantidos sem alteração.

## 1.1.0-beta.1 — 2026-07-29

- criada a rodada `qualified-149d-2026-07-v5`, com início prospectivo em 30/07;
- adicionado `QualifiedPullback` como único candidato visível no MNQ;
- congelados venda, score mínimo 5/6, distância máxima de 2 ATR da VWAP, volume
  relativo mínimo 1, alvo de 1,5R e validade de 12 candles;
- definido risco técnico aceito entre USD 5 e USD 50 por contrato;
- `EvidencePullback`, compras, MES e demais setups passam a referências silenciosas;
- painel identifica o setup como `MNQ VENDA VALIDADA 149D`;
- corrigido o backtest para tratar expiração como 0R bruto, igual ao acompanhamento
  do NinjaTrader;
- seleção ampliada validada em 103 sessões de MNQ, com +USD 216,25 e PF 1,877 no
  teste final separado;
- mantida ausência total de execução automática e acesso à conta.

## 1.0.0-beta.1 — 2026-07-29

- adotada seleção offline da estratégia em vez de ajustes sucessivos no gráfico;
- analisados 531 registros v8 e 380 candidatos de pullback entre 22 e 29/07;
- criado `EvidencePullback` para MNQ vendido, abaixo de VWAP descendente, score mínimo 4/6 e risco máximo de USD 50;
- definido alvo de 1R e rodada `evidence-2026-07-v4`, iniciando em 30/07;
- MES, compras e setups anteriores permanecem registrados silenciosamente, mas deixam de aparecer como recomendação;
- regra congelada por pelo menos dez resultados decididos;
- análise e decisão registradas na documentação;
- mantida ausência total de execução automática e acesso à conta.

## 0.9.1-beta.1 — 2026-07-29

- corrigida a migração de indicadores já salvos no workspace com limite de risco de USD 75;
- no modo de validação, limites antigos acima de USD 50 passam a ser reduzidos automaticamente para o valor congelado da rodada;
- a divergência antiga deixa de bloquear toda a avaliação de tendência e contexto;
- limites mais restritivos e políticas diferentes continuam preservados e sinalizados como configuração divergente;
- mantidos os critérios do `ContextPullback`, a rodada `context-2026-07-v3` e o formato CSV v8.

## 0.9.0-beta.1 — 2026-07-28

- criada a rodada `context-2026-07-v3`, com início prospectivo em 29/07;
- adicionada regra experimental `ContextPullback`, sem remover o pullback anterior usado como referência;
- criada VWAP aproximada de sessão com preço típico e volume dos candles, reiniciada conforme o template de horário do gráfico;
- contexto passa a avaliar lado e inclinação da VWAP, inclinação das EMAs, força do candle, extensão em ATR e volume relativo;
- compras e vendas usam critérios simétricos e precisam atingir pelo menos 5 de 6 confirmações, além dos critérios obrigatórios;
- máximo de risco reduzido de USD 75 para USD 50 por contrato;
- alvo visual e de validação do novo setup definido em 1R;
- painel passa a mostrar score, VWAP, distância em ATR e volume relativo;
- criado CSV v8 com os valores completos do contexto;
- criados resumo `validation_v3` e segmentos `segments_v2`, incluindo agrupamento por score;
- mantida ausência total de execução automática e acesso à conta.

## 0.8.1-beta.1 — 2026-07-28

- encerrada sem aprovação operacional a rodada `forward-2026-07-v1`;
- iniciada a rodada diagnóstica `diagnostic-2026-07-v2`, sem alterar entradas, stops, alvos ou limite de risco;
- criado CSV bruto v7 para preservar integralmente os arquivos v6 da rodada encerrada;
- o CSV bruto passa a substituir o retrato completo do dia durante um reprocessamento, removendo registros órfãos;
- criado resumo diário `validation_v2`, separado dos resumos anteriores;
- criada análise automática em `TradeAssistant/Analysis` por direção, hora e faixa de risco;
- adicionados testes para remoção de registros órfãos e para os novos segmentos;
- custos permanecem excluídos e nenhuma execução automática foi adicionada.

Todas as mudanças relevantes do projeto são registradas neste arquivo. Enquanto o indicador estiver em validação, as versões usarão o sufixo `beta`.

## 0.8.0-beta.1 — 2026-07-22

- iniciada a rodada congelada `forward-2026-07-v1`;
- MES `EmaCrossBaseline` passa a ser o candidato visível em 1R;
- MES `TrendPullback` fica pausado visualmente, mas continua registrado para comparação;
- MNQ `TrendPullback` permanece visível em observação com alvo de validação em 1,5R;
- configurações divergentes da rodada bloqueiam novos sinais e geram aviso no painel;
- painel passa a mostrar setup, etapa, alvo, resultado diário em R e moeda, sequência de stops e drawdown;
- criado CSV bruto v6 com rodada, etapa, alvo e parâmetros completos do pullback;
- criado resumo diário automático por ativo, período e setup em `TradeAssistant/Summaries`;
- registros anteriores a 23/07 são marcados como referência histórica e ficam inelegíveis para a revisão prospectiva;
- resumo inclui alvos, stops, expirados, ambíguos, rejeitados, taxa de acerto, resultado, risco médio, sequência de perdas e drawdown;
- custos continuam explicitamente excluídos e nenhuma execução automática foi adicionada.

## 0.7.0-beta.1 — 2026-07-22

- painel passa a declarar `RESULTADO HIPOTÉTICO | SEM ORDENS`;
- adicionada medição simultânea de 1R, 1,5R e 2R;
- registrado o primeiro evento entre 1R, stop, expiração, ambiguidade e rejeição;
- cada alvo possui preço, situação e horário próprios;
- toques de alvo e stop no mesmo candle permanecem ambíguos quando a ordem não pode ser conhecida;
- criado CSV v5 com `EvaluationType`, `EntryAssumption` e `OutcomeBasis`;
- `RecordKey` passa a incluir instrumento e período, eliminando colisões entre MNQ e MES;
- entrada, stop técnico, validade, risco e setups foram preservados;
- adicionados testes de alvo antes do stop, stop antes do alvo e ambiguidade no mesmo candle;
- mantida ausência total de execução automática e acesso à conta.

## 0.6.0-beta.1 — 2026-07-20

- implementado setup experimental `TrendPullback` a favor da tendência das EMAs;
- exigidos retorno à região da EMA rápida e candle de confirmação;
- stop do pullback definido além do extremo técnico do candle, sem aproximação artificial;
- adicionadas tolerância em ATR e espera mínima entre candidatos da mesma direção;
- mantido `EmaCrossBaseline` em paralelo apenas para comparação no CSV;
- permitido um sinal ativo por setup, com acompanhamento independente;
- painel e elementos do gráfico passam a mostrar somente o experimento de pullback;
- criado CSV v4 com a coluna `Setup` e chave estável que também inclui o setup;
- mantidos o limite padrão de USD 75 por contrato e o bloqueio de sinais acima dele;
- adicionados testes de cálculo do pullback, isolamento dos setups e persistência v4.

## 0.5.0-beta.1 — 2026-07-20

- criado perfil inicial de risco para a avaliação Take Profit Trader 25k;
- risco máximo padrão definido em USD 75 por microcontrato;
- adicionadas políticas `SomenteAvisar` e `DescartarAcimaDoLimite`;
- sinais acima do limite podem ser registrados como `RiskRejected` sem virar operação ativa;
- adicionada contagem de descartados por risco no painel;
- sinais descartados recebem marcação visual discreta, sem linhas de entrada, stop e alvo;
- criado formato CSV v3 com a política de risco registrada e sem alterar arquivos anteriores;
- adicionados testes de rejeição, estatísticas e liberação do próximo sinal.

## 0.4.0-beta.1 — 2026-07-20

- adicionada leitura dinâmica de tick, valor do ponto e moeda do ativo no NinjaTrader;
- entrada, stop e alvo passam a respeitar o tick válido do instrumento;
- adicionados distância em pontos e ticks, risco e alvo financeiro para um contrato;
- adicionado limite financeiro configurável, inicialmente desativado com valor `0`;
- adicionadas situações `DENTRO DO LIMITE`, `ACIMA DO LIMITE` e `NÃO CONFIGURADO`;
- ampliado o CSV com os dados financeiros e criada a versão 2 do formato sem alterar arquivos antigos;
- adicionados testes comparando o cálculo financeiro com valores de ponto diferentes.

## 0.3.1-beta.1 — 2026-07-20

- corrigida a criação de operações hipotéticas sobrepostas;
- enquanto existir um sinal ativo, novos cruzamentos são ignorados;
- um novo sinal volta a ser permitido depois de alvo, stop, expiração ou resultado ambíguo;
- adicionada proteção tanto no indicador quanto no rastreador;
- adicionado teste automatizado específico para o bloqueio e a liberação do próximo sinal.

## 0.3.0-beta.1 — 2026-07-20

- adicionada gravação persistente dos sinais hipotéticos em CSV;
- criado um arquivo separado por dia, ativo e período gráfico;
- o registro ativo é atualizado quando o sinal atinge alvo, stop ou expira;
- adicionada chave estável por sinal e configuração para impedir duplicações ao recarregar o gráfico;
- registrados versão, parâmetros, preços, resultado em R, MFE, MAE e quantidade de candles;
- adicionada propriedade **Salvar histórico CSV** e situação da gravação no painel;
- validação automatizada do CSV e compilação estrutural concluídas sem erros.

Commit principal: `5316843`.

## 0.2.0-beta.1 — 2026-07-20

- redesenhada a apresentação visual dos sinais;
- adicionadas zonas de risco e retorno, cores distintas e limitação do histórico visual;
- adicionada a versão atual no cabeçalho do painel;
- mantido o modo exclusivamente visual, sem execução de ordens.

Commits principais: `2bfddac` e `11edee2`.

## 0.1.0-beta.1 — 2026-07-20

- criada a base mínima do indicador para NinjaTrader 8;
- adicionada regra demonstrativa de cruzamento de EMA;
- adicionados stop por ATR, alvo por risco/retorno e validade em candles;
- adicionado acompanhamento hipotético de alvo, stop, expiração e resultado ambíguo;
- corrigido o namespace de `DashStyleHelper` para compilação no NinjaTrader;
- confirmado por inspeção que não existem chamadas de execução ou gerenciamento de ordens.

Commits principais: `537d087`, `eae4d9f` e `ca2c4e1`.
