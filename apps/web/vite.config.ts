import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";
import react from "@vitejs/plugin-react";
import { defineConfig, Plugin } from "vitest/config";
import {
  canonicalUrl,
  INDEXABLE_PATHS,
  prerenderContent,
  resolveRouteSeo
} from "./src/lib/seoRoutes";

function escapeAttr(value: string): string {
  return value.replace(/&/g, "&amp;").replace(/"/g, "&quot;");
}

/** Substitui a tag existente (se houver) ou a insere antes de </head>. */
function setHeadTag(html: string, matcher: RegExp, tag: string): string {
  return matcher.test(html) ? html.replace(matcher, tag) : html.replace("</head>", `    ${tag}\n  </head>`);
}

/**
 * Prerender: apos o build, gera um HTML estatico por rota indexavel a partir do
 * dist/index.html (que ja referencia o bundle com hash). Cada arquivo recebe os
 * metadados de SEO da rota e conteudo real no #root. O nginx serve esses arquivos
 * via try_files ($uri.html); o SPA assume no cliente ao carregar. Sem headless
 * browser: robusto no build alpine do Docker e imune a auth/API/AdBlockGate.
 */
function prerenderPlugin(): Plugin {
  let outDir = "dist";
  return {
    name: "loto-prerender-seo",
    apply: "build",
    configResolved(config) {
      outDir = config.build.outDir || "dist";
    },
    closeBundle() {
      const template = readFileSync(join(outDir, "index.html"), "utf8");

      for (const path of INDEXABLE_PATHS) {
        const seo = resolveRouteSeo(path);
        const canonical = canonicalUrl(path);
        const title = escapeAttr(seo.title);
        const description = escapeAttr(seo.description);
        const robots = seo.index ? "index, follow" : "noindex, nofollow";

        let html = template;
        html = setHeadTag(html, /<title>[\s\S]*?<\/title>/, `<title>${seo.title}</title>`);
        html = setHeadTag(
          html,
          /<meta\s+name="description"[^>]*>/,
          `<meta name="description" content="${description}" />`
        );
        html = setHeadTag(html, /<meta\s+name="robots"[^>]*>/, `<meta name="robots" content="${robots}" />`);
        html = setHeadTag(
          html,
          /<link\s+rel="canonical"[^>]*>/,
          `<link rel="canonical" href="${escapeAttr(canonical)}" />`
        );
        html = setHeadTag(
          html,
          /<meta\s+property="og:title"[^>]*>/,
          `<meta property="og:title" content="${title}" />`
        );
        html = setHeadTag(
          html,
          /<meta\s+property="og:description"[^>]*>/,
          `<meta property="og:description" content="${description}" />`
        );
        html = setHeadTag(
          html,
          /<meta\s+property="og:url"[^>]*>/,
          `<meta property="og:url" content="${escapeAttr(canonical)}" />`
        );
        html = html.replace(/<div id="root">\s*<\/div>/, `<div id="root">${prerenderContent(path)}</div>`);

        const outPath = join(outDir, `${path.replace(/^\//, "")}.html`);
        mkdirSync(dirname(outPath), { recursive: true });
        writeFileSync(outPath, html, "utf8");
      }

      // eslint-disable-next-line no-console
      console.log(`[prerender] ${INDEXABLE_PATHS.length} rotas estaticas geradas em ${outDir}/`);
    }
  };
}

export default defineConfig({
  plugins: [react(), prerenderPlugin()],
  build: {
    assetsDir: "spa-assets"
  },
  server: {
    port: 5174
  },
  test: {
    environment: "jsdom",
    globals: true,
    include: ["tests/**/*.test.ts", "tests/**/*.test.tsx"],
    setupFiles: "./vitest.setup.ts"
  }
});
