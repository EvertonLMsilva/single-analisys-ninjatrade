# Histórico de versões

Todas as mudanças relevantes do projeto são registradas neste arquivo. Enquanto o indicador estiver em validação, as versões usarão o sufixo `beta`.

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
