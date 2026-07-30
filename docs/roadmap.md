# Roteiro do projeto

## Estado atual - observação universal em tempo real

A versão `1.3.0-beta.1` apresenta sinais durante a sessão em qualquer gráfico de
5 minutos. Esse modo foi criado para acelerar a coleta comparável entre ativos,
mas permanece experimental: aparecer no gráfico não significa que a regra tenha
vantagem estatística naquele instrumento. Os resultados ficam isolados em
`TradeAssistant/Realtime` e devem ser revisados antes de qualquer automação.

## Estado atual - momentum intradiario

Implementacao concluida na versao `1.2.0-beta.1`: o candidato roda no MNQ em modo
visual, com um micro hipotetico, CSV proprio e sem ordens. O proximo passo nao e
otimizar a regra; e coletar 20 sessoes posteriores a 29/07 e aplicar o protocolo
congelado de pelo menos 12 operacoes, lucro positivo, PF minimo 1,20, drawdown
maximo de USD 500 e intervalo sem sinal de no maximo cinco sessoes.

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

## Próximo experimento de mercado

A primeira hipótese por regime e estrutura em barras de um minuto foi reprovada. O
MNQ perdeu USD 846,76; o MES apresentou somente seis operações. O classificador de
regime fica preservado como infraestrutura, mas o reteste simples não será ajustado
por busca massiva. O próximo incremento deve:

- registrar MFE e MAE de todos os eventos estruturais, inclusive os que não viraram
  operação pelo limite financeiro;
- distinguir rompimento aceito, falso rompimento e recuperação de nível;
- usar walk-forward por pregões inteiros;
- manter fluxo bid/ask fora do modelo até existir histórico de ticks ou barras
  volumétricas;
- aceitar somente regras estáveis em blocos cronológicos e no portão econômico.

Os ajustes desse incremento foram concluídos. Entre gatilhos manuais, portfólios
pequenos e 12 modelos de eventos, o melhor observado foi MNQ em 2R. Ele ganhou
USD 108,50 na validação e perdeu USD 145,50 na confirmação, sendo reprovado. Não
serão escolhidos novos filtros sobre o mesmo histórico. A próxima seleção exige um
bloco adicional de dados posteriores a 29/07.

Esse bloco de desenvolvimento foi concluído com uma mudança de hipótese. O candidato
de momentum intradiário do MNQ ganhou USD 1.244 por micro em 81 pregões, com PF 1,416
e frequência diária. Ele foi congelado para 20 pregões futuros com um micro. A regra
não pode ser ajustada durante a rodada, e o dimensionamento só será reconsiderado se
o PF permanecer em pelo menos 1,20 com drawdown de até USD 500.

## Fora do escopo atual

- envio de ordens;
- acesso à conta;
- gerenciamento de posição real;
- promessa de rentabilidade;
- otimização de parâmetros antes de obter uma linha de base confiável.

Os três primeiros itens permanecem fora do escopo da versão atual, mas passam a ter marcos
futuros explícitos. Planejamento de automação não representa autorização para operar uma
conta real nem para contrariar regras de uma mesa proprietária.
