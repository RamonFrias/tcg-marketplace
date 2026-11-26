# 0003. Postgres com um banco por serviço, via EF Core

- **Status:** Aceito
- **Data:** 2026-11-26
- **Serviços afetados:** todos os que guardam estado

## Contexto

Microsserviço que compartilha banco com outro fica acoplado pelo esquema. Ao mesmo tempo, subir um servidor de banco por serviço localmente é caro.

## Decisão

- Um único container Postgres no Aspire, com **um database por serviço** (`catalogdb`, `inventorydb`...). Nenhum serviço lê ou escreve no banco de outro.
- Acesso a dados com EF Core + Npgsql, `DbContext` no `<Svc>.Infrastructure`, tabelas em snake_case e schema com o nome do serviço.
- Concorrência otimista com `xmin` nas tabelas disputadas (estoque, pedidos), por causa das cartas de unidade única.
- Migrations geradas e revisadas com a skill `nova-migration`; aplicadas ao subir em Development.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| SQL Server | Funciona, mas o container é mais pesado e o Postgres tem `xmin` e JSONB nativos |
| Cosmos DB Emulator | Pesado e instável localmente |
| Schema por serviço no mesmo database | Isolamento mais fraco; facilita join "escondido" entre serviços |
| Dapper | Mais trabalho manual de mapeamento; EF Core basta para o volume do projeto |

## Consequências

- **Mais fácil:** isolamento real entre serviços; trocar o banco de um serviço sem afetar os outros.
- **Mais difícil:** consultas que cruzam serviços dependem de eventos e modelos de leitura (ex.: Search).
