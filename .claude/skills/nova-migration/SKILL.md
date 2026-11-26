---
name: nova-migration
description: Gera e revisa uma migration do EF Core para o banco Postgres de um serviço do tcg-marketplace, conferindo perda de dados antes de aplicar. Use depois de criar ou alterar entidade, configuração de mapeamento ou DbContext, ou quando pedirem migration, atualizar o banco ou alterar tabela.
argument-hint: <Servico> <DescricaoDaMudanca>
---

# Nova migration (EF Core + Postgres, um banco por serviço)

Pedido: `$ARGUMENTS`.

## 1. Preparar

- Serviço: `src/services/<Svc>`. DbContext em `<Svc>.Infrastructure/Persistence/<Svc>DbContext.cs`.
- Confirme que o build passa: `dotnet build src/services/<Svc>/<Svc>.Infrastructure`.
- Confirme que a ferramenta existe: `dotnet ef --version`.
  - Se faltar, avise e sugira `dotnet tool install --global dotnet-ef` ou, melhor, um tool manifest local (`dotnet new tool-manifest` e `dotnet tool install dotnet-ef`).
  - Não instale sem o usuário pedir.
- A geração usa o `DesignTimeDbContextFactory` do Infrastructure, que lê `ConnectionStrings__<svc>db`. Para gerar a migration não precisa de banco rodando. Para aplicar, precisa.

## 2. Gerar

Nome em PascalCase descrevendo a mudança (`AddListingCondition`, `CreateStoresTable`):
```powershell
dotnet ef migrations add <Nome> `
  --project src/services/<Svc>/<Svc>.Infrastructure `
  --startup-project src/services/<Svc>/<Svc>.Infrastructure `
  --context <Svc>DbContext `
  --output-dir Persistence/Migrations
```

## 3. Revisar antes de seguir (obrigatório)

Leia o `Up()` e o `Down()` gerados e confira:

| Risco | O que procurar | O que fazer |
|---|---|---|
| Perda de dados | `DropColumn`, `DropTable`, `AlterColumn` que reduz tamanho ou muda tipo | Pare e mostre ao usuário. Renomeação deve ser `RenameColumn`, não drop+add |
| Coluna obrigatória nova em tabela com dados | `nullable: false` sem `defaultValue` | Defina um default ou faça em duas etapas |
| Nomes | Tabelas e colunas em snake_case, schema do serviço | Ajuste a configuração e gere de novo |
| Concorrência | `xmin` mapeado nas tabelas de estoque e pedido | Confira se está na configuração |
| Índices | FKs e campos de busca (ex.: `card_id`, `store_id`) com índice | Adicione na configuração |

Se a migration ficou errada, ajuste a configuração e gere de novo com `dotnet ef migrations remove`, **só se ela ainda não foi aplicada nem commitada**. Migration já commitada não se apaga: crie outra corrigindo.

## 4. Aplicar localmente

- Em desenvolvimento, o serviço aplica as migrations pendentes ao subir (`Database.MigrateAsync()` só com `Environment = Development`). Basta subir com a skill `rodar-local`.
- Manual, com o Postgres do Aspire no ar:
  ```powershell
  dotnet ef database update --project src/services/<Svc>/<Svc>.Infrastructure --context <Svc>DbContext
  ```
  Use a connection string mostrada no dashboard do Aspire na variável `ConnectionStrings__<svc>db`.

## 5. Ao terminar

Mostre ao usuário:
- o nome da migration e um resumo do `Up()`, em uma linha por operação;
- qualquer risco encontrado na revisão;
- como aplicar.
