---
name: importar-cards
description: Popula o Catalog do tcg-marketplace com cards, por seed local (todos os jogos) ou importação opcional de APIs públicas (Scryfall para Magic, pokemontcg.io para Pokémon), normalizando para o card canônico. Use quando pedirem seed, dados de exemplo, importar cards, coleções ou sets, ou popular o catálogo.
argument-hint: seed | magic <codigo-do-set> | pokemon <id-do-set>
---

# Importar cards para o Catalog

Pedido: `$ARGUMENTS`. Decisão do projeto: **seed fake + importação opcional**. As imagens ficam como **URL externa** (não baixamos arquivo).

## Card canônico (o que o Catalog guarda)

| Campo | Exemplo | Observação |
|---|---|---|
| `Game` | `Magic`, `Pokemon`, `YuGiOh`, `OnePiece`, `Lorcana` | enum |
| `SetCode` / `SetName` | `dmu` / `Dominaria United` | código em minúsculas |
| `CollectorNumber` | `107`, `4/102`, `SV001` | texto, não número |
| `Name` | nome em inglês | idioma da oferta é atributo do Listing, não do card |
| `Rarity` | `Common`, `Uncommon`, `Rare`, `Mythic`, `Secret`... | normalizado por jogo |
| `ImageUrl` | URL https | sem baixar |
| `ExternalSource` / `ExternalId` | `scryfall` / `uuid` | evita duplicar na reimportação |

Chave única: `(Game, SetCode, CollectorNumber)`. A importação faz **upsert**: atualiza o que mudou e não apaga nada.

## Modo `seed`

- Arquivos em `src/services/Catalog/Catalog.Infrastructure/Seed/<game>.json`, de 20 a 40 cards por jogo, cobrindo raridades variadas e pelo menos 2 sets por jogo.
- Yu-Gi-Oh!, One Piece e Lorcana usam só seed por enquanto.
- O seed roda ao subir em Development, só se a tabela estiver vazia. Fica numa classe `CatalogSeeder` no Infrastructure, chamada pelo `Program.cs` do Functions.

## Modo importação (Magic / Pokémon)

Antes da primeira importação:
- Confira os termos de uso atuais da API, no site oficial, e mostre ao usuário o que eles pedem (atribuição, cache, limites).
- Projeto de estudo e privado: mesmo assim, respeite os termos.

Implementação no Catalog:
- **Application:**
  - interface `ICardSource` com `GetSetAsync(setCode, ct)`, que devolve `IAsyncEnumerable<ImportedCard>`;
  - caso de uso `ImportSet`, que faz o upsert pelo agregado `Card`.
- **Infrastructure:** `ScryfallCardSource` e `PokemonTcgCardSource`, com `HttpClient` tipado registrado via `AddHttpClient` e resiliência padrão do ServiceDefaults.
- **Functions:** `POST /api/catalog/admin/import/{game}/{set}`. Só para admin (JWT com papel `admin`). Responde 202 e processa em background (fila do Azurite), para não estourar o timeout HTTP.

Regras de cada fonte:

| Fonte | Endpoint | Regras |
|---|---|---|
| Scryfall | `https://api.scryfall.com/cards/search?q=set:<code>&unique=prints`, paginando por `next_page` | Headers `User-Agent: tcg-marketplace/0.1` e `Accept: application/json` obrigatórios. Intervalo de 100 ms entre requisições. Imagem em `image_uris.normal` ou na primeira face de `card_faces` |
| pokemontcg.io | `https://api.pokemontcg.io/v2/cards?q=set.id:<id>&page=N&pageSize=250` | Header `X-Api-Key` opcional, lido de user-secrets ou de variável de ambiente, nunca no código. Respeitar 429 com backoff. Imagem em `images.small` |

Em qualquer fonte: 429 ou 5xx → retry com backoff exponencial (máximo 5); 4xx → para e registra.

## Testes

- **Normalização:** um JSON de exemplo de cada fonte, gravado em `tests/Catalog.Tests/Fixtures/`, vira o `ImportedCard` esperado. Cubra raridade, número e card de duas faces.
- **Upsert:** importar duas vezes não duplica.
- **Sem rede:** os testes nunca chamam a API real; usam `HttpMessageHandler` falso.

## Ao terminar

Mostre ao usuário:
- quantos cards entraram, quantos foram atualizados e quantos foram ignorados;
- um exemplo de busca: `GET /api/catalog/cards?game=Magic&set=dmu`.
