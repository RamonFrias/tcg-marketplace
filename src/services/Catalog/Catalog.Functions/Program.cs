using Catalog.Application;
using Catalog.Infrastructure;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.AddServiceDefaults();
builder.Services.AddCatalogApplication();
builder.AddCatalogInfrastructure();

var host = builder.Build();

if (builder.Environment.IsDevelopment())
{
    await host.Services.PrepareCatalogDatabaseAsync();
}

await host.RunAsync();
