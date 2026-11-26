# 0008. Fase 1 sem autenticação

- **Status:** Aceito
- **Data:** 2026-11-26
- **Serviços afetados:** Catalog, Store, Inventory, Search

## Contexto

Na Fase 1 lojistas cadastram lojas e ofertas, e um admin cadastra cards. O serviço Identity (JWT e papéis) ainda não existe, e construí-lo agora atrasaria a vitrine.

## Decisão

- As rotas da Fase 1 ficam **abertas** (`AuthorizationLevel.Anonymous`). Operações de lojista recebem o id da loja no corpo da requisição.
- O **Identity** (cadastro, login, JWT com papéis cliente, lojista e admin) entra na **Fase 2**, junto com o carrinho. Nessa hora, as rotas de escrita da Fase 1 passam a exigir o papel certo e o dono do recurso.
- Nada da Fase 1 vai para um ambiente público antes disso.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| Identity mínimo já na Fase 1 | Fase 1 maior; decidido priorizar a vitrine |

## Consequências

- **Mais fácil:** Fase 1 menor e testável de ponta a ponta sem login.
- **Mais difícil:** na Fase 2 é preciso voltar às rotas de escrita para proteger cada uma.
- **A fazer:** na Fase 2, listar todas as rotas abertas e cobrir cada uma com autorização e teste.
