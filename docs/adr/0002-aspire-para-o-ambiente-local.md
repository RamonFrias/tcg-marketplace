# 0002. Aspire 17 para orquestrar o ambiente local

- **Status:** Aceito
- **Data:** 2026-11-26
- **Serviços afetados:** todos

## Contexto

Localmente precisamos subir vários Function Apps, o gateway, Postgres, Service Bus, Azurite e OpenSearch, com connection strings e endereços certos entre eles e com logs e traces num lugar só.

## Decisão

Usamos o Aspire (versão 17) com o AppHost em `src/AppHost/TcgMarketplace.AppHost`:

- Postgres, Service Bus Emulator, Azurite e OpenSearch como recursos com volume e `ContainerLifetime.Persistent`, para não perder dados entre execuções.
- No Aspire 17 o `Aspire.AppHost.Sdk` ficou obsoleto (diagnóstico ASPIRE012). O AppHost usa `Microsoft.NET.Sdk` com os pacotes `Aspire.Hosting.AppHost` e `Aspire.Hosting.Dotnet`. Projetos .NET entram por caminho com `AddDotnetProject(nome, caminho)`; Function Apps com `AddAzureFunctionsProject(nome, caminho)`. O AppHost não tem `ProjectReference` para os serviços.
- `TcgMarketplace.ServiceDefaults` liga OpenTelemetry, health checks (`/health`, `/alive` em Development), service discovery e resiliência HTTP em todos os processos.
- Docker é pré-requisito para rodar local.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| docker-compose + `func start` por serviço | Muito ajuste manual de portas e connection strings, sem dashboard de traces |
| Aspire 13 com `Aspire.AppHost.Sdk` | Versão antiga; o SDK foi descontinuado no 17 |

## Consequências

- **Mais fácil:** um comando para subir tudo; dashboard com logs, métricas e traces; connection strings injetadas.
- **Mais difícil:** exige Docker e memória (o Service Bus Emulator sobe um SQL Server e o OpenSearch é pesado); a API do Aspire muda entre versões (o agent `pesquisador-dotnet` confere antes de escrever).
