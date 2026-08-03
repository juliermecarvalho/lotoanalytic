import { describe, expect, it } from "vitest";
import {
  headingFromTitle,
  INDEXABLE_PATHS,
  MODALIDADES,
  navLabel,
  prerenderContent,
  resolveRouteSeo,
  SITE_NAME
} from "../src/lib/seoRoutes";

describe("INDEXABLE_PATHS", () => {
  it("cobre gerador + dashboard de todas as modalidades e as paginas de servico", () => {
    // 6 modalidades x 2 (gerador + dashboard) + 5 paginas de servico.
    expect(INDEXABLE_PATHS).toHaveLength(Object.keys(MODALIDADES).length * 2 + 5);
    for (const slug of Object.keys(MODALIDADES)) {
      expect(INDEXABLE_PATHS).toContain(`/gerar-jogos/${slug}`);
      expect(INDEXABLE_PATHS).toContain(`/dashboard/${slug}`);
    }
  });

  it("todas as rotas prerenderizadas sao indexaveis (nunca noindex)", () => {
    for (const path of INDEXABLE_PATHS) {
      expect(resolveRouteSeo(path).index).toBe(true);
    }
  });

  it("nao inclui a home nem rotas privadas", () => {
    expect(INDEXABLE_PATHS).not.toContain("/");
    expect(INDEXABLE_PATHS.some((p) => p.startsWith("/admin") || p.startsWith("/perfil"))).toBe(false);
  });
});

describe("headingFromTitle", () => {
  it("remove o sufixo do site", () => {
    expect(headingFromTitle(`Conferidor de Jogos das Loterias | ${SITE_NAME}`)).toBe(
      "Conferidor de Jogos das Loterias"
    );
  });
});

describe("navLabel", () => {
  it("gera rotulos legiveis por tipo de rota", () => {
    expect(navLabel("/gerar-jogos/mega-sena")).toBe("Gerar jogos da Mega-Sena");
    expect(navLabel("/dashboard/lotofacil")).toBe("Estatísticas da Lotofácil");
    expect(navLabel("/")).toBe("Início");
    expect(navLabel("/conferidor")).toBe("Conferidor de Jogos das Loterias");
  });
});

describe("prerenderContent", () => {
  it("inclui H1, descricao e links internos", () => {
    const html = prerenderContent("/gerar-jogos/mega-sena");
    expect(html).toContain("<h1>Gerar Jogos da Mega-Sena com Estatística</h1>");
    expect(html).toContain("Gere jogos da Mega-Sena");
    // Linka para outras rotas, mas nao para si mesma.
    expect(html).toContain('href="/dashboard/mega-sena"');
    expect(html).not.toContain('href="/gerar-jogos/mega-sena"');
    expect(html).toContain('href="/"');
  });

  it("nao deixa marcadores HTML crus vindos do conteudo", () => {
    // As strings do projeto nao trazem <, > ou aspas; o resultado so deve conter
    // as tags que nos mesmos geramos.
    const html = prerenderContent("/dashboard/quina");
    expect(html.startsWith("<main")).toBe(true);
    expect(html).not.toContain("<script");
  });
});
