// SEO por rota do SPA (camada de runtime).
//
// A logica pura (qual title/description/robots cada rota recebe) vive em
// ./seoRoutes, compartilhada com o prerender do build. Aqui ficam apenas os
// efeitos no <head> e o hook do React. O shell renderiza no cliente, entao o
// Googlebot (que executa JS) le esses valores; alem disso o build ja entrega um
// HTML estatico por rota (prerender) para quem nao executa JS.
import { useEffect } from "react";
import { useRouterState } from "@tanstack/react-router";
import { canonicalUrl, resolveRouteSeo, RouteSeo, SITE_NAME, SITE_URL } from "./seoRoutes";

export { canonicalUrl, SITE_NAME, SITE_URL };
export type { RouteSeo };

/** Alias historico mantido para compatibilidade com chamadas/testes. */
export const resolveSeo = resolveRouteSeo;

function upsertMeta(attr: "name" | "property", key: string, content: string): void {
  let el = document.head.querySelector<HTMLMetaElement>(`meta[${attr}="${key}"]`);
  if (!el) {
    el = document.createElement("meta");
    el.setAttribute(attr, key);
    document.head.appendChild(el);
  }
  el.setAttribute("content", content);
}

function upsertCanonical(href: string): void {
  let el = document.head.querySelector<HTMLLinkElement>('link[rel="canonical"]');
  if (!el) {
    el = document.createElement("link");
    el.setAttribute("rel", "canonical");
    document.head.appendChild(el);
  }
  el.setAttribute("href", href);
}

/** Aplica os metadados da rota no <head>. Retorna o que foi aplicado. */
export function applySeo(pathname: string): RouteSeo {
  const seo = resolveRouteSeo(pathname);
  const canonical = canonicalUrl(pathname);
  document.title = seo.title;
  upsertMeta("name", "description", seo.description);
  upsertMeta("name", "robots", seo.index ? "index, follow" : "noindex, nofollow");
  upsertCanonical(canonical);
  upsertMeta("property", "og:title", seo.title);
  upsertMeta("property", "og:description", seo.description);
  upsertMeta("property", "og:url", canonical);
  return seo;
}

/** Hook que mantem o <head> sincronizado com a rota atual do SPA. */
export function useRouteSeo(): void {
  const pathname = useRouterState({ select: (state) => state.location.pathname });
  useEffect(() => {
    applySeo(pathname);
  }, [pathname]);
}
