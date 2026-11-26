---
name: novo-caso-de-uso
description: Cria um caso de uso completo num serviço do tcg-marketplace, do endpoint HTTP da Azure Function ao command/query no Application, validação, repositório no Infrastructure e testes. Use quando pedirem endpoint, rota, API, operação ou funcionalidade (ex.: cadastrar oferta, buscar card, adicionar ao carrinho, listar lojas).
argument-hint: <Servico> <descrição do caso de uso>
---

# Novo caso de uso (endpoint → Application → Infrastructure)

Pedido: `$ARGUMENTS`.

## 1. Definir o contrato

Antes de codar, escreva ao usuário em 3 a 5 linhas:
- **Tipo:** command (muda estado) ou query (só lê).
- **Rota:** `VERB /api/<svc>/<recurso>`, com recurso no plural e em kebab-case. Exemplo: `POST /api/inventory/listings`.
- **Entrada e saída:** campos do request e do response.
- **Erros:** quais acontecem e o status de cada um.
- **Autenticação:** quem pode chamar (anônimo, cliente, lojista, admin).

Se alguma regra de negócio não estiver clara, pergunte antes de seguir.

## 2. Testes primeiro

Em `tests/<Svc>.Tests/Application/<UseCase>HandlerTests.cs`:
- Teste o handler com repositórios falsos (NSubstitute): caminho feliz, validação e cada erro de negócio.
- Rode e veja falhar.

## 3. Application

Em `<Svc>.Application/UseCases/<Recurso>/<Acao>/`:

| Arquivo | Conteúdo |
|---|---|
| `<Acao>Command.cs` ou `<Acao>Query.cs` | `sealed record` com os dados de entrada |
| `<Acao>Handler.cs` | Implementa `ICommandHandler<TCommand, TResult>` ou `IQueryHandler<TQuery, TResult>` e retorna `Result<T>` |
| `<Acao>Validator.cs` | FluentValidation: formato, obrigatórios e limites |
| `<Acao>Response.cs` | DTO de saída. Nunca devolva a entidade de domínio |

- Não use MediatR: o projeto usa interfaces próprias de handler (licença do MediatR).
- O handler orquestra e não contém regra de negócio: carrega o agregado, chama o método de domínio, salva pelo `IUnitOfWork`.
- **Query de leitura** pode ir direto ao banco por uma interface de leitura (`I<Recurso>ReadService`), sem carregar o agregado (CQRS leve).
- Registre o handler e o validator no `DependencyInjection.cs` do Application.

## 4. Infrastructure

- Implemente as interfaces novas: repositório e serviço de leitura com `AsNoTracking()` e projeção direto para o DTO.
- Se o caso de uso **publica evento de integração**, grave no Outbox na mesma transação (skill `novo-evento`).

## 5. Function (HTTP)

Em `<Svc>.Functions/Http/<Recurso>Functions.cs`:
```csharp
[Function("<Acao><Recurso>")]
public async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "<svc>/<recurso>")] HttpRequest req,
    CancellationToken ct)
```
- Desserialize, chame o validator e o handler, e mapeie `Result` para a resposta HTTP:

| Situação | Status |
|---|---|
| Sucesso | 200 (query) / 201 com `Location` (criação) / 204 (sem corpo) |
| Validação | 400 com `ValidationProblemDetails` |
| Não encontrado | 404 com `ProblemDetails` |
| Conflito de regra ou concorrência | 409 com `ProblemDetails` |

- A Function fica fina: nada de regra nem acesso a dados.
- Rotas protegidas validam o JWT do serviço Identity. Exija o papel certo (cliente, lojista, admin).
- Listas são paginadas com `page` e `pageSize` (máximo 100) e devolvem `totalCount`.

## 6. Verificar

- `dotnet test tests/<Svc>.Tests` verde.
- Chame o agent `qa` com os arquivos do caso de uso para procurar casos de borda que faltaram.
- Mostre ao usuário um exemplo de chamada (`curl` ou `.http`) pela rota do gateway: `/api/<svc>/...`.
- Se criou algo persistido, lembre a skill `nova-migration`.
