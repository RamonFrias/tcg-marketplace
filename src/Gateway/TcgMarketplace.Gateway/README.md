# Gateway (YARP)

Porta de entrada única da API. Cada serviço ganha uma rota `/api/<servico>/{**catch-all}` apontando para o
Function App dele, resolvido pelo service discovery do Aspire.

Ao criar um serviço (skill `novo-servico`), acrescente em `appsettings.json`:

```json
"ReverseProxy": {
  "Routes": {
    "catalog": { "ClusterId": "catalog", "Match": { "Path": "/api/catalog/{**catch-all}" } }
  },
  "Clusters": {
    "catalog": { "Destinations": { "functions": { "Address": "https+http://catalog-functions" } } }
  }
}
```

O nome `catalog-functions` é o nome do recurso no AppHost, e o Function App responde em `/api/catalog/...`.
No AppHost, o gateway precisa de `.WithReference(<servico>)` para receber o endereço.
