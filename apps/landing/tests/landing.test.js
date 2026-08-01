const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");

const root = path.resolve(__dirname, "..");
const htmlPath = path.join(root, "index.html");
const cssPath = path.join(root, "styles.css");
const jsPath = path.join(root, "app.js");
const robotsPath = path.join(root, "robots.txt");
const sitemapPath = path.join(root, "sitemap.xml");
const manifestPath = path.join(root, "icons", "site.webmanifest");
const ogImagePath = path.join(root, "og-image.jpg");

assert.ok(fs.existsSync(htmlPath), "index.html deve existir");
assert.ok(fs.existsSync(cssPath), "styles.css deve existir");
assert.ok(fs.existsSync(jsPath), "app.js deve existir");
assert.ok(fs.existsSync(robotsPath), "robots.txt deve existir");
assert.ok(fs.existsSync(sitemapPath), "sitemap.xml deve existir");
assert.ok(fs.existsSync(manifestPath), "site.webmanifest deve existir");
assert.ok(fs.existsSync(ogImagePath), "og-image.jpg deve existir");

const html = fs.readFileSync(htmlPath, "utf8");
const css = fs.readFileSync(cssPath, "utf8");
const js = fs.readFileSync(jsPath, "utf8");

assert.match(html, /LotoAnalytics/, "landing deve apresentar o nome do projeto");
assert.match(html, /Monte jogos com critérios visíveis/, "hero deve comunicar a promessa principal refinada");
assert.match(html, /não garante prêmio/i, "landing deve conter aviso responsável com acentos");
assert.match(html, /Prova prática/, "gerador deve ser posicionado como prova prática");
assert.match(html, /id="criteria-summary"/, "gerador deve resumir critérios selecionados");
assert.match(html, /id="reset-generator"/, "gerador deve permitir limpar a amostra");
assert.doesNotMatch(html, /Planilha Premium/, "landing nao deve mais mencionar a planilha premium");
assert.doesNotMatch(html, /Grupo VIP/, "landing nao deve mais mencionar grupo VIP");
assert.doesNotMatch(html, /Combo Completo/, "landing nao deve mais mencionar combo completo");
assert.match(html, /O gerador aumenta minhas chances\?/, "FAQ deve responder objeção sobre chance");
assert.match(html, /sempre gratuita/, "landing deve reforçar que a ferramenta é sempre gratuita");
assert.match(html, /id="volante-script"/, "landing deve ter a área do script do volante");
assert.match(html, /id="build-script"/, "landing deve permitir gerar o script do volante");
assert.match(html, /nunca confirma nem paga/i, "script do volante deve avisar que não confirma nem paga");
assert.match(css, /criteria-panel/, "hero deve ter painel analítico de critérios");
assert.match(css, /mobile-sticky-cta/, "mobile deve ter CTA persistente");
assert.match(css, /@media\s*\(prefers-reduced-motion:\s*reduce\)/, "layout deve respeitar redução de movimento");
assert.match(js, /tryGenerateGame/, "script deve tentar gerar jogos com critérios");
assert.match(js, /return null/, "script não deve usar fallback silencioso quando critérios falham");
assert.match(js, /resetGenerator/, "script deve permitir limpar o gerador");
assert.match(js, /buildBookmarklet/, "script deve montar o bookmarklet do volante");
assert.match(js, /javascript:/, "bookmarklet deve usar o prefixo javascript:");

// SEO: favicon, manifest, dados estruturados e imagem social.
assert.match(html, /rel="manifest"/, "landing deve referenciar o webmanifest");
assert.match(html, /rel="apple-touch-icon"/, "landing deve ter apple-touch-icon");
assert.match(html, /icons\/icon\.svg/, "landing deve usar o favicon SVG da aplicação");
assert.match(html, /application\/ld\+json/, "landing deve conter dados estruturados JSON-LD");
assert.match(html, /"@type":\s*"FAQPage"/, "landing deve ter FAQPage estruturado");
assert.match(html, /og-image\.jpg/, "landing deve apontar og:image para a imagem social");

// Os blocos JSON-LD devem ser JSON válido.
const ldMatches = [...html.matchAll(/<script type="application\/ld\+json">([\s\S]*?)<\/script>/g)];
assert.ok(ldMatches.length >= 2, "deve haver pelo menos dois blocos JSON-LD");
for (const match of ldMatches) {
  assert.doesNotThrow(() => JSON.parse(match[1]), "cada bloco JSON-LD deve ser JSON válido");
}

// robots.txt deve apontar para o sitemap.
assert.match(fs.readFileSync(robotsPath, "utf8"), /Sitemap:\s*https?:\/\//, "robots.txt deve declarar o sitemap");

// O webmanifest deve ser JSON válido e da marca LotoAnalytics.
const manifest = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
assert.equal(manifest.name, "LotoAnalytics", "webmanifest deve usar o nome da marca");

console.log("Landing page contract passed");
