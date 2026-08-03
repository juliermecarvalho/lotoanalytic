// SEO por rota do SPA.
//
// O shell React renderiza no cliente, entao o index.html sozinho nao consegue
// dar <title>/description especificos para cada rota. Este modulo resolve os
// metadados a partir do pathname e os aplica no <head> em runtime. O Googlebot
// executa JS e le esses valores; a navegacao interna tambem mantem o titulo da
// aba coerente. Paginas de sistema/privadas recebem robots noindex.
import { useEffect } from "react";
import { useRouterState } from "@tanstack/react-router";

export const SITE_NAME = "LotoAnalytics";
export const SITE_URL = "https://lotoanalytic.com.br";

export interface RouteSeo {
  title: string;
  description: string;
  /** false => a rota recebe robots "noindex, nofollow". */
  index: boolean;
}

const MODALIDADES: Record<string, string> = {
  lotofacil: "Lotofácil",
  "mega-sena": "Mega-Sena",
  quina: "Quina",
  "dupla-sena": "Dupla Sena",
  lotomania: "Lotomania",
  "mais-milionaria": "+Milionária"
};

const DEFAULT_DESCRIPTION =
  "Análise estatística das loterias da Caixa: números mais sorteados, atrasados e frequência. " +
  "Gere jogos com base nos dados reais de todos os concursos.";

// Rotas de sistema/privadas: nunca indexar (batem com o Disallow do robots.txt).
const NOINDEX_PREFIXES = ["/perfil", "/admin", "/adm", "/concursos", "/auth"];

function titleWith(prefix: string): string {
  return `${prefix} | ${SITE_NAME}`;
}

/**
 * Resolve os metadados de SEO de um pathname. Funcao pura, sem efeitos no DOM,
 * para poder ser testada isoladamente.
 */
export function resolveSeo(pathname: string): RouteSeo {
  const path = pathname.replace(/\/+$/, "") || "/";

  if (NOINDEX_PREFIXES.some((prefix) => path === prefix || path.startsWith(prefix + "/"))) {
    return { title: titleWith("Área restrita"), description: DEFAULT_DESCRIPTION, index: false };
  }

  const generator = path.match(/^\/gerar-jogos\/([\w-]+)$/);
  if (generator) {
    const nome = MODALIDADES[generator[1]];
    if (nome) {
      return {
        title: titleWith(`Gerar Jogos da ${nome} com Estatística`),
        description:
          `Gere jogos da ${nome} baseados na análise estatística de todos os concursos: ` +
          "números mais sorteados, atrasados e frequência. Grátis.",
        index: true
      };
    }
  }

  const dashboard = path.match(/^\/dashboard\/([\w-]+)$/);
  if (dashboard) {
    const nome = MODALIDADES[dashboard[1]];
    if (nome) {
      return {
        title: titleWith(`Estatísticas da ${nome}: Números Mais Sorteados e Atrasados`),
        description:
          `Veja as estatísticas da ${nome}: dezenas mais sorteadas, números atrasados, ` +
          "frequência e padrões de todos os concursos.",
        index: true
      };
    }
  }

  switch (path) {
    case "/":
      return {
        title: titleWith("Estatísticas e Gerador de Jogos de Loteria"),
        description: DEFAULT_DESCRIPTION,
        index: true
      };
    case "/conferidor":
      return {
        title: titleWith("Conferidor de Jogos das Loterias"),
        description:
          "Confira seus jogos das loterias da Caixa contra os resultados oficiais dos concursos, " +
          "de forma rápida e gratuita.",
        index: true
      };
    case "/historicos":
      return {
        title: titleWith("Histórico de Concursos das Loterias"),
        description: "Consulte o histórico completo de resultados das loterias da Caixa, concurso a concurso.",
        index: true
      };
    case "/modalidades":
      return {
        title: titleWith("Modalidades de Loteria"),
        description:
          "Conheça as modalidades analisadas: Lotofácil, Mega-Sena, Quina, Dupla Sena, Lotomania e +Milionária.",
        index: true
      };
    case "/estatisticas/detalhes":
      return {
        title: titleWith("Estatísticas Detalhadas das Loterias"),
        description:
          "Estatísticas detalhadas das loterias: frequência, atraso, pares/ímpares, soma das dezenas e mais.",
        index: true
      };
    case "/privacidade":
      return {
        title: titleWith("Política de Privacidade"),
        description: "Política de privacidade do LotoAnalytics: como tratamos os seus dados.",
        index: true
      };
    default:
      return {
        title: titleWith("Estatísticas e Gerador de Jogos de Loteria"),
        description: DEFAULT_DESCRIPTION,
        index: true
      };
  }
}

/** URL canonica absoluta para um pathname (sem barra final, exceto a raiz). */
export function canonicalUrl(pathname: string): string {
  const clean = pathname.replace(/\/+$/, "");
  return clean === "" ? `${SITE_URL}/` : `${SITE_URL}${clean}`;
}

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
  const seo = resolveSeo(pathname);
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
