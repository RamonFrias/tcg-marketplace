# 0005. Clean Architecture, DDD e TDD em cada serviço

- **Status:** Aceito
- **Data:** 2026-11-26
- **Serviços afetados:** todos

## Contexto

O projeto quer praticar os padrões de mercado (TDD, DDD, Clean Architecture, SOLID) e mantê-los à medida que cresce, inclusive quando o código é escrito com ajuda do Claude.

## Decisão

- **Camadas por serviço:** `Functions → Infrastructure → Application → Domain`, com dependências só para dentro. Domain e Application não usam EF Core, Npgsql, Azure SDK, OpenSearch nem ASP.NET. Um serviço não referencia o código de outro.
- **Domínio (DDD):** agregados herdam `AggregateRoot<TId>`; ids tipados; fábricas que validam e devolvem `Result<T>`; erros de regra como `Error` (sem exceção); value objects como `sealed record` com propriedades só de leitura (ex.: `Money`); eventos de domínio no passado.
- **Casos de uso:** commands e queries com handlers próprios (`ICommandHandler`, `IQueryHandler` em `BuildingBlocks.Application`), registrados por `AddHandlersFromAssembly`. Não usamos MediatR (licença comercial desde 2025). Validação de entrada com FluentValidation.
- **TDD:** teste antes do código, nomes em português (`Metodo_Cenario_Resultado`). Só testes unitários (xUnit v3 + NSubstitute), rodando pelo Microsoft.Testing.Platform (`global.json`).
- **Garantias automáticas:** o hook `layer-guard` bloqueia dependências proibidas; `tdd-reminder` lembra o teste; `build-test-on-stop` compila e testa os serviços alterados; os agents `arquiteto` e `qa` revisam design e casos de borda.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| MediatR | Licença comercial; os handlers próprios cobrem o que precisamos |
| Exceções para erro de regra | Esconde o fluxo e custa caro; `Result` deixa o erro explícito na assinatura |
| Testes de integração com Testcontainers | Decidido começar só com unitários; reavaliar quando houver Docker no CI |

## Consequências

- **Mais fácil:** regra de negócio testável sem infraestrutura; trocar infraestrutura sem tocar no domínio.
- **Mais difícil:** mais projetos e mapeamentos (entidade ↔ DTO ↔ tabela) por serviço.
