var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Rotas e clusters vêm do appsettings.json (seção ReverseProxy). Os endereços usam service discovery
// do Aspire: "https+http://catalog-functions" resolve para o Function App registrado no AppHost.
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapReverseProxy();

await app.RunAsync();
