# Auditoria dos CSVs OHLCV de 149 dias

Data da auditoria: 29/07/2026

## Resultado

Os dois arquivos são adequados para pesquisa offline após a exclusão das sessões
incompletas do MNQ. Nenhum arquivo foi copiado para o repositório.

| Ativo | Candles | Primeira barra | Última barra | SHA-256 |
|---|---:|---|---|---|
| MNQ | 28.941 | 01/03/2026 20:05 | 29/07/2026 15:55 | `5DC438B4D6A571B6C4354F0C757968EFDAA36263716B32BD84C22F5912FADD92` |
| MES | 29.546 | 01/03/2026 20:05 | 29/07/2026 15:55 | `B1D8B2E8F2C84BF40C76BE35DFDCA5A553819D49B98D90581859558910D826BE` |

Foram confirmados:

- cabeçalho `Time,Open,High,Low,Close,Volume`;
- timestamps estritamente crescentes;
- ausência de duplicações;
- OHLC consistente;
- volume positivo;
- todos os preços aderentes ao tick de 0,25.

## Alinhamento

- 28.941 timestamps em comum;
- nenhum candle exclusivo do MNQ;
- 605 candles presentes apenas no MES;
- cobertura conjunta de 97,9523%.

Distribuição dos candles ausentes no MNQ:

| Sessão | Candles ausentes |
|---|---:|
| 06/04/2026 | 276 |
| 07/04/2026 | 177 |
| 08/07/2026 | 80 |
| 09/07/2026 | 60 |
| 17/07/2026 | 12 |

Essas cinco sessões foram excluídas da pesquisa de MNQ. Sessões encurtadas presentes
nos dois arquivos, como feriados, foram mantidas.

## Horário das sessões

O horário dos arquivos acompanha a mudança de horário dos Estados Unidos:

- antes de 08/03/2026, a nova sessão começa às 20:00 no relógio do CSV;
- de 08/03/2026 até o fim do horário de verão americano, começa às 19:00;
- a janela regular passa de 11:30–18:00 para 10:30–17:00.

O simulador foi corrigido para calcular dinamicamente o segundo domingo de março e o
primeiro domingo de novembro. Autotestes cobrem os dois lados da mudança.

## Divergências com a exportação anterior

No MNQ, duas barras sobrepostas divergiram:

- 11/06 às 12:40: preços idênticos e revisão apenas no volume;
- 29/07 às 15:50: a exportação anterior capturou um candle ainda em formação.

A exportação de 149 dias, produzida mais tarde, foi adotada como fonte da rodada.
