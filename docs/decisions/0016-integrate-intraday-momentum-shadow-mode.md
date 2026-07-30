# Decisao 0016 - Integrar momentum intradiario em modo de observacao

Data: 2026-07-29.

## Contexto

O candidato `intraday-momentum-2026-07-v1` foi congelado apos a pesquisa historica.
Era necessario transforma-lo em uma coleta prospectiva reproduzivel no
NinjaTrader, sem confundir o resultado com os setups anteriores e sem executar
ordens.

## Decisao

- integrar somente em MNQ;
- usar uma serie interna de um minuto;
- iniciar com um micro hipotetico;
- exigir aquecimento de 20 sessoes completas;
- permitir no maximo uma analise por sessao;
- usar stop de USD 75 e saida no fechamento regular;
- descontar USD 5 por operacao hipotetica;
- interromper novos sinais antigos enquanto o candidato estiver ligado;
- gravar os resultados em CSV separado;
- manter todas as APIs de ordens fora do projeto.

## Interface

O painel do MNQ mostra apenas a rodada de momentum, fase da amostra, status,
entrada, stop, regime e resultado carregado. O grafico desenha seta, linha de
entrada, zona/linha de stop e o resultado no encerramento.

## Validacao

Foram adicionados testes do horario regular em BRT, elegibilidade do ativo,
aquecimento, classificacao de regime, stop, saida temporal, custos e persistencia.
O projeto real `NinjaTrader.Custom.csproj` compilou com zero erros.

Isso valida a implementacao tecnica, nao a rentabilidade futura. A regra permanece
congelada por 20 sessoes prospectivas e deve ser avaliada pelo protocolo da decisao
0015.
