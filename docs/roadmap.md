# Roteiro do projeto

O objetivo atual permanece: apoiar a decisão do operador com sinais explicáveis e
mensuráveis, sem executar ordens. A arquitetura será preparada para uma execução futura,
mas qualquer integração de ordens permanecerá fisicamente separada e desligada até cumprir
os marcos de segurança e ser permitida pelas regras da conta utilizada.

## Marco 1 — Validar a persistência atual

Situação: em andamento.

Critérios para concluir:

- NinjaScript compila sem erros;
- painel mostra `v0.3.0-beta.1` e `Histórico CSV: ATIVO`;
- o primeiro sinal cria o arquivo do dia;
- o encerramento atualiza a mesma linha;
- recarregar o gráfico não duplica o sinal;
- os valores do gráfico correspondem aos valores do CSV.

Nenhuma regra de entrada será alterada antes desse marco.

## Marco 2 — Relatório e qualidade dos dados

Situação: implementado na versão `0.8.0-beta.1`; aguardando validação real no NinjaTrader.

- gerar resumo diário a partir dos arquivos registrados;
- detectar arquivos inválidos ou registros incompletos;
- permitir filtrar versão, ativo, período, horário e direção;
- separar claramente alvo, stop, expirado e ambíguo;
- mostrar expectativa em R, MFE e MAE sem esconder a quantidade da amostra.

O resumo diário automático cobre resultado em R e moeda, riscos médios, sequência de stops e drawdown. Filtros e consolidação entre vários dias continuam como evolução posterior.

## Marco 3 — Linha de base da regra demonstrativa

- escolher ativo, contrato, período gráfico e horário de análise fixos;
- executar Playback em uma amostra previamente definida;
- guardar os arquivos brutos sem editar os resultados;
- produzir um relatório da linha de base;
- registrar problemas visuais e operacionais observados.

A regra de cruzamento de EMA continua sendo apenas uma demonstração técnica. Seus resultados não comprovam uma estratégia operacional.

## Marco 4 — Migrar uma regra real

- selecionar somente um setup do projeto antigo;
- documentar suas condições de entrada, invalidação, stop e alvo;
- implementar a regra sem remover a linha de base;
- comparar as duas regras sobre os mesmos dados;
- aceitar, ajustar ou descartar a nova regra com justificativa registrada.

Antes da migração do projeto antigo, foi aprovado um experimento controlado de pullback para MNQ. Consulte `docs/decisions/0001-test-mnq-pullback.md`. Ele deve permanecer separado da regra demonstrativa e respeitar o limite de USD 75.

## Marco 5 — Melhorar a interface

- priorizar entrada, stop, alvo, situação e justificativa;
- reduzir informações que não ajudam na decisão imediata;
- adicionar filtros visuais somente depois de observar uso real;
- validar legibilidade em diferentes escalas e temas do gráfico.

## Marco 4.5 — Viabilidade econômica da avaliação

Situação: o candidato atual foi reprovado; redesenho pendente.

- simular meta de USD 1.500 em no máximo 20 pregões;
- aplicar drawdown trailing, mínimo de dias e regra de consistência;
- testar quantidades sem confundir alavancagem com vantagem estatística;
- exigir pelo menos 60% de aprovação e no máximo 15% de falha por drawdown;
- manter seleção, validação e teste final separados;
- impedir publicação no indicador enquanto nenhum candidato passar pelo portão.

O `QualifiedPullback` atingiu no máximo 19,05% de aprovação nas janelas históricas.
Nenhuma quantidade entre 1 e 30 micros passou pelo portão.

A primeira busca de portfólios também foi concluída sem aprovação: 60 de 1.099
portfólios únicos passaram na seleção, mas nenhum confirmou pelo menos 60% de
aprovação na validação mantendo drawdown P90 de até USD 1.000. O próximo incremento
de pesquisa deve usar walk-forward, e a confirmação final deve ocorrer somente em
dados posteriores a 29/07.

## Marco 6 — Preparar execução simulada

- criar um contrato de execução separado do analisador;
- reproduzir entrada, stop, alvo, slippage, comissões e rejeições no Playback/Sim101;
- registrar ordem solicitada, preenchimento, cancelamento e posição resultante;
- implementar limite diário, limite por operação, quantidade máxima e botão de emergência;
- provar que nenhuma implementação simulada consegue alcançar uma conta real;
- comparar preenchimentos simulados com as hipóteses atuais do CSV.

## Marco 7 — Confirmação manual assistida

- apresentar a ordem preparada sem enviá-la;
- exigir confirmação explícita do operador;
- revalidar preço, risco, horário e estado da posição imediatamente antes do envio;
- impedir ordens duplicadas e operações fora da janela permitida;
- manter trilha de auditoria completa;
- disponibilizar somente em ambiente onde assistência de execução seja permitida.

## Marco 8 — Automação controlada

- habilitar somente após validação prospectiva, Playback e Sim101;
- exigir configuração explícita por conta e ambiente;
- iniciar desligada e falhar sempre para o estado seguro;
- usar uma conta própria ou um programa que autorize bots por escrito;
- bloquear contas da Take Profit Trader PRO enquanto a regra oficial proibir bots e algos;
- permitir desligamento imediato e reconciliação da posição com a corretora;
- liberar gradualmente, começando por um contrato e risco reduzido.

## Fora do escopo atual

- envio de ordens;
- acesso à conta;
- gerenciamento de posição real;
- promessa de rentabilidade;
- otimização de parâmetros antes de obter uma linha de base confiável.

Os três primeiros itens permanecem fora do escopo da versão atual, mas passam a ter marcos
futuros explícitos. Planejamento de automação não representa autorização para operar uma
conta real nem para contrariar regras de uma mesa proprietária.
