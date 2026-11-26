'use strict';
// Hook 8 · PreToolUse (Write|Edit|MultiEdit)
// Contratos de eventos (src/building-blocks/*Contracts*/) são consumidos por outros serviços.
// Remover ou renomear um tipo ou campo de um contrato que já existe quebra os consumidores,
// então o hook pede confirmação ao usuário e sugere criar uma versão nova (ex.: StockReservedV2).

const fs = require('fs');
const path = require('path');
const L = require('./lib');

function splitTopLevel(s) {
  const out = [];
  let depth = 0;
  let cur = '';
  for (const ch of s) {
    if ('<([{'.includes(ch)) depth += 1;
    else if ('>)]}'.includes(ch)) depth -= 1;
    if (ch === ',' && depth === 0) { out.push(cur); cur = ''; } else cur += ch;
  }
  if (cur.trim()) out.push(cur);
  return out;
}

// Tira strings e comentários, para `class X` num comentário ou "(" numa string não confundirem a análise.
function stripNoise(code) {
  return String(code)
    .replace(/@?\$?"(?:[^"\\]|\\.|"")*"/g, '""')
    .replace(/\/\*[\s\S]*?\*\//g, ' ')
    .replace(/\/\/.*$/gm, ' ');
}

// Parâmetros de records posicionais, respeitando parênteses aninhados (atributos, valores padrão).
function positionalParams(code) {
  const out = [];
  const re = /\brecord\s+(?:class\s+|struct\s+)?[A-Z]\w*\s*(?:<[^>]*>)?\s*\(/g;
  let m;
  while ((m = re.exec(code))) {
    let depth = 1;
    let i = re.lastIndex;
    const start = i;
    while (i < code.length && depth > 0) {
      if (code[i] === '(') depth += 1;
      else if (code[i] === ')') depth -= 1;
      i += 1;
    }
    out.push(code.slice(start, i - 1));
  }
  return out;
}

function members(raw) {
  const code = stripNoise(raw);
  const set = new Set();
  let m;
  const types = /\b(?:record|class|struct|interface)\s+(?:class\s+|struct\s+)?([A-Z]\w*)/g;
  while ((m = types.exec(code))) set.add(`o tipo ${m[1]}`);
  const props = /\bpublic\s+(?:required\s+|virtual\s+|override\s+)*[\w<>[\],?.\s]+?\s+([A-Z]\w*)\s*\{\s*(?:get|init|set)/g;
  while ((m = props.exec(code))) set.add(`o campo ${m[1]}`);
  for (const params of positionalParams(code)) {
    for (const param of splitTopLevel(params)) {
      const name = param.replace(/\[[^\]]*\]/g, ' ').trim().replace(/\s*=.*$/s, '').split(/\s+/).pop();
      if (name && /^[A-Za-z_]\w*$/.test(name)) set.add(`o campo ${name}`);
    }
  }
  return set;
}

L.main((evt) => {
  const file = L.filePath(evt);
  const rel = L.relPath(evt, file);
  if (!rel || !rel.endsWith('.cs')) return;
  if (!/^src\/building-blocks\/[^/]*Contracts[^/]*\//.test(rel)) return;

  const abs = path.resolve(L.projectRoot(evt), file);
  if (!fs.existsSync(abs)) return; // contrato novo: nada a quebrar

  const before = members(L.oldContent(evt, abs));
  const after = members(L.newContent(evt));
  const removed = [...before].filter((x) => !after.has(x));
  if (!removed.length) return;

  L.ask(
    `[contracts-guard] ${rel}: a alteração remove ou renomeia ${removed.join(', ')} de um contrato de evento que já existe. ` +
    'Consumidores em outros serviços podem quebrar. O caminho seguro é criar uma versão nova (ex.: StockReservedV2) ' +
    'e manter a antiga até todos migrarem. Aprove só se nenhum serviço usa isso ainda.'
  );
});
