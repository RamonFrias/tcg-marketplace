# 0007. Serviço Catalog: o card canônico

- **Status:** Aceito
- **Data:** 2026-11-26
- **Serviços afetados:** Catalog (e, a seguir, Inventory e Search, que referenciam o card)

## Contexto

O mesmo card é vendido por várias lojas, em condições, idiomas e preços diferentes. Se cada oferta guardasse os dados do card, o catálogo ficaria duplicado e inconsistente entre lojas.

## Decisão

- O **Catalog** guarda o card canônico, um por **(jogo, coleção, número)**, com índice único no banco: nome, coleção, raridade e URL da imagem.
- Condição, idioma, foil, edição, preço e estoque são da **oferta** (Listing), no serviço Inventory, que referencia o card pelo id.
- Jogos: Magic, Pokemon, YuGiOh, OnePiece e Lorcana (enum; texto na API).
- Código da coleção sempre em minúsculas; número do card é texto (`107`, `4/102`, `SV001`); imagem só por URL https.
- Rotas: `POST /api/catalog/cards` (cadastro), `GET /api/catalog/cards/{id}` e `GET /api/catalog/cards?game=&set=&name=&page=&pageSize=` (máximo 100 por página).
- Em Development o serviço aplica as migrations e carrega um **seed fictício** (22 cards, 5 jogos, 2 coleções cada). A importação das APIs públicas fica para a skill `importar-cards`.
- Persistência com **EF Core 10** + Npgsql 10 e a integração do Aspire para Npgsql (13.6.5). O EF Core 11 exige .NET 11; o projeto segue no .NET 10 (LTS).

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Card e oferta na mesma entidade | Duplica o card por loja e impede "card X em quais lojas" |
| Jogo como tabela | Os 5 jogos são fixos nesta fase; enum é mais simples |
| EF Core 11 | Só roda em .NET 11 (STS); o projeto fica no LTS |

## Consequências

- **Mais fácil:** buscar um card e comparar ofertas; reimportar sem duplicar (upsert pela chave única).
- **Mais difícil:** Inventory e Search precisam do card por id e, sem consulta síncrona, por evento (`CardRegistered` vira evento de integração quando a mensageria entrar).
- **A fazer:** publicar o evento de integração do card junto com a infraestrutura de Outbox.
