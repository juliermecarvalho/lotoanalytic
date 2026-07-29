const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "..");
const htmlPath = path.join(root, "index.html");
const cssPath = path.join(root, "styles.css");
const jsPath = path.join(root, "app.js");

assert.ok(fs.existsSync(htmlPath), "index.html deve existir");
assert.ok(fs.existsSync(cssPath), "styles.css deve existir");
assert.ok(fs.existsSync(jsPath), "app.js deve existir");

const html = fs.readFileSync(htmlPath, "utf8");
const css = fs.readFileSync(cssPath, "utf8");
const js = fs.readFileSync(jsPath, "utf8");

assert.match(html, /LotoAnalytics/, "landing deve apresentar o nome do projeto");
assert.match(html, /Monte jogos com critérios visíveis/, "hero deve comunicar a promessa principal refinada");
assert.match(html, /não garante prêmio/i, "landing deve conter aviso responsável com acentos");
assert.match(html, /Prova prática/, "gerador deve ser posicionado como prova prática");
assert.match(html, /id="criteria-summary"/, "gerador deve resumir critérios selecionados");
assert.match(html, /id="reset-generator"/, "gerador deve permitir limpar a amostra");
assert.match(html, /Planilha Premium/, "landing deve vender a planilha premium");
assert.match(html, /Grupo VIP/, "landing deve vender o grupo VIP");
assert.match(html, /Combo Completo/, "landing deve destacar o combo completo");
assert.match(html, /O gerador aumenta minhas chances\?/, "FAQ deve responder objeção sobre chance");
assert.match(css, /criteria-panel/, "hero deve ter painel analítico de critérios");
assert.match(css, /mobile-sticky-cta/, "mobile deve ter CTA persistente");
assert.match(css, /@media\s*\(prefers-reduced-motion:\s*reduce\)/, "layout deve respeitar redução de movimento");
assert.match(js, /tryGenerateGame/, "script deve tentar gerar jogos com critérios");
assert.match(js, /return null/, "script não deve usar fallback silencioso quando critérios falham");
assert.match(js, /resetGenerator/, "script deve permitir limpar o gerador");

console.log("Landing page contract passed");
