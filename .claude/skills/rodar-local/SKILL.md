---
name: rodar-local
description: Sobe o tcg-marketplace localmente com o .NET Aspire (Functions, Postgres, Service Bus Emulator, Azurite, OpenSearch, gateway YARP), confere os pré-requisitos e diagnostica problemas. Use quando pedirem para rodar, subir, iniciar ou testar o projeto local, abrir o dashboard, ou quando algum emulador ou serviço não subir.
argument-hint: [serviço específico ou "diagnosticar"]
---

# Rodar local (Aspire)

Pedido: `$ARGUMENTS`.

## 1. Pré-requisitos

Rode e mostre um quadro com ✔ ou ✖ para cada item:

| Item | Comando | Necessário para |
|---|---|---|
| .NET 10 SDK | `dotnet --version` | tudo |
| Azure Functions Core Tools v4 | `func --version` | rodar os Function Apps pelo Aspire |
| Docker em execução | `docker info` (precisa responder, não só `docker --version`) | Postgres, Service Bus Emulator, Azurite, OpenSearch |
| Memória livre | ≥ 8 GB recomendado | o Service Bus Emulator sobe junto um SQL Server e o OpenSearch é pesado |

Se o Docker faltar ou não estiver rodando, **pare** e diga ao usuário o que instalar ou abrir (Docker Desktop com WSL2). Não tente instalar nada sozinho.

## 2. Subir

- **Comando:** `dotnet run --project src/AppHost/TcgMarketplace.AppHost`. Se o Aspire CLI estiver instalado, `aspire run` também serve.
- **Em background:** rode em segundo plano para não travar a sessão e espere a linha com a URL do dashboard (`Login to the dashboard at https://localhost:.../login?t=...`).
- **O que mostrar ao usuário:**
  - a URL do dashboard (tem token, então só local);
  - os recursos e o estado de cada um (Running, Waiting ou Failed);
  - a URL do gateway (`/api/<svc>/...`).
- **Para conferir os serviços:** chame `GET /api/<svc>/health` em cada um pelo gateway.

## 3. Diagnóstico

| Sintoma | Causa provável | O que fazer |
|---|---|---|
| Recurso em `Waiting` para sempre | Depende de outro que não ficou saudável | Veja no dashboard qual dependência falhou |
| Service Bus Emulator cai ou reinicia | Pouca memória, ou o SQL Server interno não subiu | Logs do container no dashboard; feche outros containers |
| OpenSearch sai com código 137 | Sem memória | Limite o heap (`OPENSEARCH_JAVA_OPTS=-Xms512m -Xmx512m`) no AppHost |
| OpenSearch reclama de `vm.max_map_count` | Configuração do WSL2 | Oriente o usuário a ajustar no WSL (`sysctl -w vm.max_map_count=262144`) |
| Function não inicia: `func` não encontrado | Core Tools fora do PATH | Instalar ou ajustar o PATH (o usuário faz) |
| Porta em uso | Outra instância rodando | Liste os processos `dotnet` / `func` e pergunte antes de encerrar |
| Erro de migration ao subir | Migration nova não aplicada ou com problema | Veja os logs do serviço; use a skill `nova-migration` |
| 401 no gateway | Rota protegida sem token | Gere um token pelo Identity ou teste uma rota anônima |

## 4. Parar

Encerre o processo do AppHost e os containers param junto.
**Não use `docker volume rm`, `docker system prune` nem `docker compose down -v`:** isso apaga os dados locais, e o hook bloqueia.
