// Fonte unica de verdade do SEO por rota do SPA.
//
// Modulo PURO: sem React, sem acesso ao DOM. Por isso pode ser importado tanto
// pelo runtime (src/lib/seo.ts, que aplica os metadados no <head>) quanto pelo
// build (o plugin de prerender no vite.config.ts, que gera um HTML estatico por
// rota). Manter tudo aqui evita que as duas pontas saiam de sincronia.

export const SITE_NAME = "LotoAnalytics";
export const SITE_URL = "https://lotoanalytic.com.br";

export interface RouteSeo {
  title: string;
  description: string;
  /** false => a rota recebe robots "noindex, nofollow". */
  index: boolean;
}

export const MODALIDADES: Record<string, string> = {
  lotofacil: "Lotofácil",
  "mega-sena": "Mega-Sena",
  quina: "Quina",
  "dupla-sena": "Dupla Sena",
  lotomania: "Lotomania",
  "mais-milionaria": "+Milionária"
};

export const DEFAULT_DESCRIPTION =
  "Análise estatística das loterias da Caixa: números mais sorteados, atrasados e frequência. " +
  "Gere jogos com base nos dados reais de todos os concursos.";

// Rotas de sistema/privadas: nunca indexar (batem com o Disallow do robots.txt).
const NOINDEX_PREFIXES = ["/perfil", "/admin", "/adm", "/concursos", "/auth"];

function titleWith(prefix: string): string {
  return `${prefix} | ${SITE_NAME}`;
}

/** Normaliza um pathname (remove barra final, exceto a raiz). */
export function normalizePath(pathname: string): string {
  return pathname.replace(/\/+$/, "") || "/";
}

/**
 * Resolve os metadados de SEO de um pathname. Funcao pura, sem efeitos, para
 * poder ser testada isoladamente e reutilizada no build.
 */
export function resolveRouteSeo(pathname: string): RouteSeo {
  const path = normalizePath(pathname);

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

/** Titulo curto (H1) sem o sufixo " | LotoAnalytics". */
export function headingFromTitle(title: string): string {
  return title.replace(new RegExp(`\\s*\\|\\s*${SITE_NAME}\\s*$`), "");
}

/**
 * Rotas do SPA que devem ganhar um HTML estatico no build (prerender). Nao
 * inclui "/" (a landing estatica ja cobre o apex) nem rotas noindex. Deve
 * espelhar as URLs do sitemap (menos a home).
 */
export const INDEXABLE_PATHS: string[] = [
  ...Object.keys(MODALIDADES).map((slug) => `/gerar-jogos/${slug}`),
  ...Object.keys(MODALIDADES).map((slug) => `/dashboard/${slug}`),
  "/conferidor",
  "/historicos",
  "/modalidades",
  "/estatisticas/detalhes",
  "/privacidade"
];

/** Rotulo curto para um link interno de uma rota. */
export function navLabel(pathname: string): string {
  const path = normalizePath(pathname);
  const gen = path.match(/^\/gerar-jogos\/([\w-]+)$/);
  if (gen && MODALIDADES[gen[1]]) {
    return `Gerar jogos da ${MODALIDADES[gen[1]]}`;
  }
  const dash = path.match(/^\/dashboard\/([\w-]+)$/);
  if (dash && MODALIDADES[dash[1]]) {
    return `Estatísticas da ${MODALIDADES[dash[1]]}`;
  }
  if (path === "/") {
    return "Início";
  }
  return headingFromTitle(resolveRouteSeo(path).title);
}

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}

/**
 * HTML pre-renderizado injetado no #root de cada rota estatica: da ao crawler
 * conteudo real (H1 + descricao + links internos) antes do JS rodar. O React
 * substitui esse conteudo ao montar (createRoot limpa o container).
 */
export function prerenderContent(pathname: string): string {
  const path = normalizePath(pathname);
  const seo = resolveRouteSeo(path);
  const heading = escapeHtml(headingFromTitle(seo.title));
  const description = escapeHtml(seo.description);

  const links = ["/", ...INDEXABLE_PATHS]
    .filter((href) => normalizePath(href) !== path)
    .map((href) => `<li><a href="${href}">${escapeHtml(navLabel(href))}</a></li>`)
    .join("");

  return (
    `<main class="prerender-seo">` +
    `<h1>${heading}</h1>` +
    `<p>${description}</p>` +
    `<nav aria-label="Páginas do LotoAnalytics"><ul>${links}</ul></nav>` +
    `<p>Carregando a aplicação…</p>` +
    `</main>`
  );
}
