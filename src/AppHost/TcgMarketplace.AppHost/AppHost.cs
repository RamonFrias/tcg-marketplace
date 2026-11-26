var builder = DistributedApplication.CreateBuilder(args);

// Postgres único com um banco por serviço (ADR 0003).
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("tcg-marketplace-postgres")
    .WithLifetime(ContainerLifetime.Persistent);

// Service Bus Emulator: um tópico por evento de integração, uma subscription por serviço consumidor (ADR 0004).
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

// Serviços: caminhos relativos à pasta do AppHost (Aspire 17).
var catalogDb = postgres.AddDatabase("catalogdb");
var catalog = builder.AddAzureFunctionsProject("catalog-functions", "../../services/Catalog/Catalog.Functions/Catalog.Functions.csproj")
    .WithReference(catalogDb)
    .WaitFor(catalogDb)
    .WithHostStorage(storage);

builder.AddDotnetProject("gateway", "../../Gateway/TcgMarketplace.Gateway/TcgMarketplace.Gateway.csproj")
    .WithExternalHttpEndpoints()
    .WithReference(catalog);

// Ainda sem consumidor nesta fase: entram com os próximos serviços (Inventory, Search).
_ = (messaging, opensearch);

await builder.Build().RunAsync();
