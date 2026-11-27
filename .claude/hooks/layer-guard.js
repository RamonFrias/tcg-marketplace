'use strict';
// Hook 2 · PreToolUse (Write|Edit|MultiEdit)
// Garante Clean Architecture e o isolamento entre microsserviços em src/services/<Servico>/<Servico>.<Camada>/.
//   Functions → Infrastructure → Application → Domain   (dependências só apontam para dentro)
//   Domain e Application não usam framework/infra (EF Core, Npgsql, Azure SDK, OpenSearch, ASP.NET, HTTP).
//   Um serviço nunca referencia o código de outro: integração é por eventos em src/building-blocks (Contracts).

const path = require('path');
const L = require('./lib');

const INFRA_NAMESPACES = [
  /^Microsoft\.EntityFrameworkCore\b/, /^Npgsql\b/, /^Azure\b/, /^Microsoft\.Azure\b/,
  /^OpenSearch\b/, /^Microsoft\.AspNetCore\b/, /^Microsoft\.Extensions\.Http\b/, /^System\.Net\.Http\b/,
];
const INFRA_PACKAGES = [
  /^Microsoft\.EntityFrameworkCore/i, /^Npgsql/i, /^Azure\./i, /^Microsoft\.Azure\./i,
  /^OpenSearch/i, /^Microsoft\.AspNetCore/i, /^Aspire\./i,
];
const LAYER_RULES = {
  Domain: { forbiddenLayers: ['Application', 'Infrastructure', 'Functions'], noInfra: true },
  Application: { forbiddenLayers: ['Infrastructure', 'Functions'], noInfra: true },
  Infrastructure: { forbiddenLayers: ['Functions'], noInfra: false },
};

function usings(code) {
  const out = [];
  const re = /^\s*(?:global\s+)?using\s+(?:static\s+)?(?:\w+\s*=\s*)?([A-Za-z_][\w.]*)\s*;/gm;
  let m;
  while ((m = re.exec(code))) out.push(m[1]);
  return out;
}

function includes(xml, tag) {
  const out = [];
  const re = new RegExp(`<${tag}\\b[^>]*\\bInclude\\s*=\\s*"([^"]+)"`, 'gi');
  let m;
  while ((m = re.exec(xml))) out.push(m[1]);
  return out;
}

// "Inventory.Domain.Entities" ou "TcgMarketplace.Inventory.Domain" → { service: "Inventory", layer: "Domain" }
function classify(name, services) {
  const segs = name.split('.');
  const i = segs[0] === 'TcgMarketplace' ? 1 : 0;
  if (!services.includes(segs[i])) return { service: null, layer: null };
  return { service: segs[i], layer: segs[i + 1] || null };
}

L.main((evt) => {
  const rel = L.relPath(evt, L.filePath(evt));
  if (!rel || !/\.(cs|csproj)$/.test(rel)) return;
  const m = rel.match(/^src\/services\/([^/]+)\/([^/]+)\//);
  if (!m) return;

  const [, svc, project] = m;
  const layer = project.startsWith(`${svc}.`) ? project.slice(svc.length + 1).split('.')[0] : project.split('.').pop();
  const rule = LAYER_RULES[layer];
  const services = L.services(evt);
  const code = L.newContent(evt);
  if (!code) return;

  const problems = [];
  const crossService = (what, other) =>
    `${what}: o serviço ${svc} não pode depender do código do serviço ${other}. ` +
    'Integração entre serviços é por eventos/contratos em src/building-blocks (Contracts) ou por HTTP pelo gateway.';
  const wrongLayer = (what, target) =>
    `${what}: a camada ${layer} não pode depender de ${target}. ` +
    'Em Clean Architecture as dependências apontam para dentro: Functions → Infrastructure → Application → Domain.';

  if (rel.endsWith('.cs')) {
    for (const ns of usings(code)) {
      const { service, layer: target } = classify(ns, services);
      if (service && service !== svc) problems.push(crossService(`\`using ${ns}\``, service));
      else if (rule && service === svc && rule.forbiddenLayers.includes(target)) problems.push(wrongLayer(`\`using ${ns}\``, target));
      else if (rule && rule.noInfra && INFRA_NAMESPACES.some((r) => r.test(ns))) {
        problems.push(
          `\`using ${ns}\`: ${layer} não pode usar framework de infraestrutura (EF Core, Npgsql, Azure SDK, OpenSearch, ASP.NET, HTTP). ` +
          `Declare uma interface aqui e implemente em ${svc}.Infrastructure.`
        );
      }
    }
  } else {
    for (const ref of includes(code, 'ProjectReference')) {
      const name = path.posix.basename(L.toPosix(ref), '.csproj');
      const { service, layer: target } = classify(name, services);
      if (service && service !== svc) problems.push(crossService(`ProjectReference ${name}`, service));
      else if (rule && service === svc && rule.forbiddenLayers.includes(target)) problems.push(wrongLayer(`ProjectReference ${name}`, target));
      else if (layer === 'Domain' && !service && !/^BuildingBlocks\.(SharedKernel|Domain)\b/.test(name)) {
        problems.push(`ProjectReference ${name}: o Domain só pode referenciar BuildingBlocks.SharedKernel.`);
      }
    }
    if (rule && rule.noInfra) {
      for (const pkg of includes(code, 'PackageReference')) {
        if (INFRA_PACKAGES.some((r) => r.test(pkg))) {
          problems.push(`PackageReference ${pkg}: pacote de infraestrutura não entra em ${project}. Coloque em ${svc}.Infrastructure.`);
        }
      }
    }
  }

  if (problems.length) L.deny(`[layer-guard] ${rel}\n- ${problems.join('\n- ')}`);
}, { guard: true });
