---
name: nova-entidade
description: Cria entidade, agregado ou value object de domínio (DDD) num serviço do tcg-marketplace usando TDD, testes primeiro. Use quando pedirem para criar ou modelar uma entidade, agregado, value object ou regra de negócio de domínio (ex.: Card, Listing, Store, Cart, Order, Money, CardCondition).
argument-hint: <Servico> <NomeDaEntidade> [descrição das regras]
---

# Nova entidade de domínio (DDD + TDD)

Pedido: `$ARGUMENTS`.

## 1. Entender antes de codar

- Identifique o serviço (`src/services/<Svc>/<Svc>.Domain`). Se ele não existir, use antes a skill `novo-servico`.
- Classifique o que está sendo pedido:
  - **Agregado (raiz):** tem identidade e ciclo de vida próprio, é carregado e salvo inteiro. Exemplos: `Listing`, `Order`, `Cart`.
  - **Entidade interna:** tem identidade, mas só existe dentro de um agregado. Exemplo: `OrderItem`.
  - **Value object:** sem identidade, imutável e comparado por valor. Exemplos: `Money`, `CardCondition`, `Language`, `CollectorNumber`.
- Liste as **invariantes**, as regras que nunca podem ser violadas.
- Regra de negócio que não foi decidida: **pergunte ao usuário, não invente.** O vocabulário do domínio está na seção "Linguagem do domínio" abaixo.

## 2. Testes primeiro (red)

Em `tests/<Svc>.Tests/Domain/<Pasta>/<Nome>Tests.cs`:
- Um teste por comportamento, com nome em português no formato `Metodo_Cenario_Resultado`. Exemplo: `Reservar_QuantidadeMaiorQueEstoque_RetornaErro`.
- Cubra a criação válida, cada invariante violada, cada transição de estado e os eventos de domínio emitidos.
- Rode `dotnet test` e **confirme que falha** pelo motivo certo (não compila, ou assert falha).

## 3. Implementar (green)

Padrões do projeto (BuildingBlocks.SharedKernel):
- Agregado herda `AggregateRoot<TId>`. Value object é `sealed record` ou herda `ValueObject`.
- Ids fortemente tipados: `public readonly record struct ListingId(Guid Value)`.
- **Construtor privado + fábrica estática** que valida: `public static Result<Listing> Create(...)`. Nada de `new` com estado inválido.
- Setters privados. Mudança de estado só por métodos com nome de negócio (`Reserve`, `Release`, `ChangePrice`), nunca `SetQuantity`.
- Erro de regra retorna `Result.Failure(Error)`, com erros estáticos por agregado (`ListingErrors.InsufficientStock`). Exceção só para bug de programação.
- Eventos de domínio no passado (`ListingCreated`, `StockReserved`), adicionados com `RaiseDomainEvent(...)`.
- Dinheiro sempre `Money(decimal Amount, string Currency)`, nunca `decimal` solto. Moeda padrão `BRL`.
- Sem atributos de EF, JSON ou validação no Domain. O mapeamento fica no Infrastructure (`IEntityTypeConfiguration`).

Rode os testes até passarem.

## 4. Refatorar

- Remova duplicação e confira SOLID: uma responsabilidade por classe, e regras no agregado, não em serviços anêmicos.
- Testes continuam verdes.
- Chame o agent `qa` com os arquivos do agregado para procurar casos de borda que faltaram. Se ele achar bug, corrija e rode de novo.

## 5. Persistência (se o agregado é salvo)

- `Infrastructure/Persistence/Configurations/<Nome>Configuration.cs` com `IEntityTypeConfiguration<T>`:
  - tabela em snake_case;
  - value objects como `ComplexProperty` ou `OwnsOne`;
  - conversão dos ids tipados;
  - concorrência otimista com `xmin` no Postgres (obrigatório em estoque, por causa das cartas de unidade única).
- Interface do repositório em `Application/Abstractions/I<Nome>Repository.cs`, implementação no Infrastructure.
- Depois disso, use a skill `nova-migration`.

## Linguagem do domínio (TCG)

| Termo | Significado |
|---|---|
| Game | Magic, Pokemon, YuGiOh, OnePiece, Lorcana |
| Card | Card canônico do catálogo: jogo, coleção, número, nome, raridade, imagem |
| Listing | Oferta de uma loja para um card, com condição, idioma, foil, edição, preço e quantidade |
| CardCondition | NM, LP, MP, HP, DMG |
| Store | Loja vendedora (marketplace com várias lojas) |
| Reservation | Reserva de estoque feita no checkout, expira em 20 min |
| Order / SubOrder | O pedido é dividido em sub-pedidos, um por loja |
