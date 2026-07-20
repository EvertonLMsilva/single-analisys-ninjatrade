# Roteiro do projeto

O objetivo permanece: apoiar a decisão do operador com sinais explicáveis e mensuráveis, sem executar ordens.

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

- gerar resumo diário a partir dos arquivos registrados;
- detectar arquivos inválidos ou registros incompletos;
- permitir filtrar versão, ativo, período, horário e direção;
- separar claramente alvo, stop, expirado e ambíguo;
- mostrar expectativa em R, MFE e MAE sem esconder a quantidade da amostra.

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

## Marco 5 — Melhorar a interface

- priorizar entrada, stop, alvo, situação e justificativa;
- reduzir informações que não ajudam na decisão imediata;
- adicionar filtros visuais somente depois de observar uso real;
- validar legibilidade em diferentes escalas e temas do gráfico.

## Fora do escopo atual

- envio de ordens;
- acesso à conta;
- gerenciamento de posição real;
- promessa de rentabilidade;
- otimização de parâmetros antes de obter uma linha de base confiável.
