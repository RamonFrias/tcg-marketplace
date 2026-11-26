var builder = DistributedApplication.CreateBuilder(args);

// Postgres único com um banco por serviço (cada serviço chama postgres.AddDatabase("<svc>db")).
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("tcg-marketplace-postgres")
    .WithLifetime(ContainerLifetime.Persistent);

// Service Bus Emulator: um tópico por evento de integração, uma subscription por serviço consumidor.
var messaging = builder.AddAzureServiceBus("messaging")
    .RunAsEmulator(emulator => emulator.WithLifetime(ContainerLifetime.Persistent));

// Azurite: storage exigido pelo host das Azure Functions e pelo Durable Functions.
var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite => azurite
        .WithDataVolume("tcg-marketplace-azurite")
        .WithLifetime(ContainerLifetime.Persistent));

// OpenSearch para o serviço Search (modelo de leitura). Segurança desligada: só uso local.
var opensearch = builder.AddContainer("opensearch", "opensearchproject/opensearch", "3.10.1")
    .WithEnvironment("discovery.type", "single-node")
    .WithEnvironment("DISABLE_SECURITY_PLUGIN", "true")
    .WithEnvironment("DISABLE_INSTALL_DEMO_CONFIG", "true")
    .WithEnvironment("OPENSEARCH_JAVA_OPTS", "-Xms512m -Xmx512m")
    .WithHttpEndpoint(targetPort: 9200, name: "http")
    .WithVolume("tcg-marketplace-opensearch", "/usr/share/opensearch/data")
    .WithLifetime(ContainerLifetime.Persistent);

// Caminho relativo à pasta do AppHost (Aspire 17: AddDotnetProject substitui AddProject<T>).
var gateway = builder.AddDotnetProject("gateway", "../../Gateway/TcgMarketplace.Gateway/TcgMarketplace.Gateway.csproj")
    .WithExternalHttpEndpoints();

// Os serviços entram aqui a partir da Fase 1 (skill novo-servico), usando postgres, messaging, storage e opensearch,
// e o gateway recebe .WithReference(<servico>) para rotear /api/<servico>/...
_ = (postgres, messaging, storage, opensearch, gateway);

await builder.Build().RunAsync();
