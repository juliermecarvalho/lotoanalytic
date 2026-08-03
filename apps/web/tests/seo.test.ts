import { describe, expect, it } from "vitest";
import { canonicalUrl, resolveSeo, SITE_NAME } from "../src/lib/seo";

describe("resolveSeo", () => {
  it("gera titulo e description especificos para o gerador de cada modalidade", () => {
    const seo = resolveSeo("/gerar-jogos/mega-sena");
    expect(seo.title).toContain("Mega-Sena");
    expect(seo.title).toContain(SITE_NAME);
    expect(seo.description).toContain("Mega-Sena");
    expect(seo.index).toBe(true);
  });

  it("gera titulo e description especificos para o dashboard de cada modalidade", () => {
    const seo = resolveSeo("/dashboard/lotofacil");
    expect(seo.title).toContain("Lotofácil");
    expect(seo.description.toLowerCase()).toContain("sorteadas");
    expect(seo.index).toBe(true);
  });

  it("ignora barra final ao resolver a rota", () => {
    expect(resolveSeo("/dashboard/quina/")).toEqual(resolveSeo("/dashboard/quina"));
  });

  it("marca rotas de sistema/privadas como noindex", () => {
    expect(resolveSeo("/perfil").index).toBe(false);
    expect(resolveSeo("/admin/concursos").index).toBe(false);
    expect(resolveSeo("/adm/login").index).toBe(false);
    expect(resolveSeo("/auth/callback").index).toBe(false);
    expect(resolveSeo("/concursos/importar").index).toBe(false);
  });

  it("mantem paginas de servico indexaveis", () => {
    for (const path of ["/", "/conferidor", "/historicos", "/modalidades", "/estatisticas/detalhes", "/privacidade"]) {
      expect(resolveSeo(path).index).toBe(true);
    }
  });

  it("nao trata uma modalidade desconhecida como pagina de modalidade", () => {
    // Cai no fallback generico (ainda indexavel), sem inventar o nome.
    const seo = resolveSeo("/gerar-jogos/inexistente");
    expect(seo.title).toBe(`Estatísticas e Gerador de Jogos de Loteria | ${SITE_NAME}`);
  });
});

describe("canonicalUrl", () => {
  it("usa barra final apenas na raiz", () => {
    expect(canonicalUrl("/")).toBe("https://lotoanalytic.com.br/");
    expect(canonicalUrl("/dashboard/quina")).toBe("https://lotoanalytic.com.br/dashboard/quina");
    expect(canonicalUrl("/dashboard/quina/")).toBe("https://lotoanalytic.com.br/dashboard/quina");
  });
});
