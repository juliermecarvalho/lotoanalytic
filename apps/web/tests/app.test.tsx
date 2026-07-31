import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { App } from "../src/App";
import { AuthService } from "../src/lib/auth";
import { mockApiRequests } from "./mocks/api";

const noopAuthService: AuthService = {
  getSession: async () => null,
  login: async () => undefined,
  logout: async () => undefined,
  completeLogin: async () => null
};

const premiumUser = {
  id: "77777777-7777-7777-7777-777777777777",
  subject: "keycloak-subject",
  username: "usuario.teste",
  email: "usuario@teste.local",
  roles: ["usuario_premium"],
  planoAtual: {
    codigo: "premium",
    nome: "Plano Premium",
    limiteJogosPorGeracao: 100,
    permiteExportarCsv: true,
    permiteExportarPdf: true
  }
};

const adminUser = {
  ...premiumUser,
  roles: ["administrador", "usuario_premium"]
};

// Navega para uma rota fora do menu simulando o historico do navegador.
function navigateTo(path: string) {
  window.history.pushState({}, "", path);
  window.dispatchEvent(new PopStateEvent("popstate"));
}

// Le o conteudo de um Blob no jsdom, que nao implementa Blob.text().
function readBlobText(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(String(reader.result));
    reader.onerror = () => reject(reader.error);
    reader.readAsText(blob);
  });
}

describe("LotoAnalytics web", () => {
  beforeEach(() => {
    window.history.pushState({}, "", "/");
    localStorage.clear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("renders the statistical dashboard at the root route", async () => {
    render(<App authService={noopAuthService} />);

    // A raiz entrega o painel estatistico dentro do chrome padrao, com a barra lateral unica e atalho para a geracao.
    expect(await screen.findByRole("heading", { name: "Painel estatístico Lotofácil" })).toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: "Principal" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Lotofácil" })).toBeInTheDocument();
    expect(screen.queryByRole("navigation", { name: "Navegacao principal" })).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Gerar jogos" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Entrar com Keycloak/ })).not.toBeInTheDocument();
  });

  it("shows the user dropdown with admin entry only for administrators", async () => {
    // O menu do usuario vive no chrome padrao; o painel em tela cheia nao o exibe.
    window.history.pushState({}, "", "/perfil");
    const { unmount } = render(
      <App
        authService={noopAuthService}
        initialState={{
          apiBaseUrl: "https://localhost:7101",
          auth: { accessToken: "token-admin", username: "admin.teste" },
          currentUser: adminUser
        }}
      />
    );

    fireEvent.click(await screen.findByRole("button", { name: /admin.teste/ }));
    expect(await screen.findByRole("menuitem", { name: /Perfil/ })).toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: /Admin/ })).toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: /Sair/ })).toBeInTheDocument();

    unmount();
    window.history.pushState({}, "", "/perfil");

    render(
      <App
        authService={noopAuthService}
        initialState={{
          apiBaseUrl: "https://localhost:7101",
          auth: { accessToken: "token-premium", username: "usuario.teste" },
          currentUser: premiumUser
        }}
      />
    );

    fireEvent.click(await screen.findByRole("button", { name: /usuario.teste/ }));
    expect(await screen.findByRole("menuitem", { name: /Perfil/ })).toBeInTheDocument();
    expect(screen.queryByRole("menuitem", { name: /Admin/ })).not.toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: /Sair/ })).toBeInTheDocument();
  });

  it("generates games through the backend with the statistical filters", async () => {
    render(
      <App
        authService={noopAuthService}
        initialState={{
          apiBaseUrl: "https://localhost:7101",
          auth: { accessToken: "token-teste", username: "usuario.teste" },
          currentUser: premiumUser
        }}
      />
    );

    navigateTo("/gerar-jogos/lotofacil");
    expect(await screen.findByRole("heading", { name: "Filtros matemáticos" })).toBeInTheDocument();
    const quantityInput = screen.getByLabelText("Quantidade");
    expect(quantityInput).toHaveAttribute("max", "30");
    fireEvent.change(quantityInput, { target: { value: "150" } });
    expect(quantityInput).toHaveValue(30);

    // O concurso mais recente vem da API e alimenta o card do concurso anterior.
    expect(await screen.findByRole("heading", { name: "Concurso anterior · 3.411" })).toBeInTheDocument();

    // Todos os filtros comecam ligados com a opcao padrao marcada, inclusive a soma das dezenas.
    expect(screen.queryByText("nenhuma opção — filtro sem restrição")).not.toBeInTheDocument();
    expect(await screen.findByText("185–194 · 50% da base")).toBeInTheDocument();

    // As estatisticas dos filtros vem da tabela pre-calculada no banco: 2 de 4 concursos com 7 pares.
    expect(await screen.findByText("7 pares / 8 ímpares · 50% da base")).toBeInTheDocument();
    expect(screen.getByText("4 sorteios")).toBeInTheDocument();
    expect(screen.getByText("2 sort.")).toBeInTheDocument();

    // Os resumos derivados tambem vem do banco: total calibrado e media de repetidas.
    expect(screen.getByText(/calibrado sobre 3\.411 sorteios reais/)).toBeInTheDocument();
    expect(screen.getByText("repetição média: 8,0")).toBeInTheDocument();

    // A geração fica disponível no bloco de jogos, sem cabeçalho duplicado.
    expect(screen.queryByRole("button", { name: "Salvar preset" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Gerar jogos" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Salvar cartela" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Exportar CSV" })).toBeDisabled();

    fireEvent.click(screen.getByRole("button", { name: "Gerar jogos" }));

    expect(await screen.findByText("Jogo 01")).toBeInTheDocument();
    expect(screen.getByText(/1 jogos válidos · 4.210 combinações testadas/)).toBeInTheDocument();
    expect(screen.getByText(/Custo estimado: R\$ 3,00/)).toBeInTheDocument();
    expect(screen.getByText(/soma 187/)).toBeInTheDocument();
    expect(mockApiRequests).toContainEqual(
      expect.objectContaining({
        authorization: "Bearer token-teste",
        method: "POST",
        url: "https://localhost:7101/gerador/lotofacil/gerar"
      })
    );

    // Com jogos gerados, "Salvar cartela" exibe o codigo do jogos.js na caixa de saida.
    const showScriptButton = screen.getByRole("button", { name: "Salvar cartela" });
    expect(showScriptButton).toBeEnabled();
    fireEvent.click(showScriptButton);

    const scriptOutput = await screen.findByLabelText("Código jogos.js");
    expect(scriptOutput.textContent).toContain("const jogos = [");
    expect(scriptOutput.textContent).toContain(
      '["01", "02", "04", "06", "08", "10", "11", "12", "14", "15", "17", "19", "21", "23", "24"]'
    );
    expect(scriptOutput.textContent).toContain("iniciarPreenchimentoDosJogos();");

    // O botao de copiar envia o codigo para a area de transferencia.
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.assign(navigator, { clipboard: { writeText } });
    fireEvent.click(screen.getByRole("button", { name: "Copiar código" }));
    await waitFor(() => expect(writeText).toHaveBeenCalledTimes(1));
    expect(writeText.mock.calls[0][0]).toContain("const jogos = [");
    expect(await screen.findByRole("button", { name: "Copiado!" })).toBeInTheDocument();

    // O Exportar CSV continua baixando o arquivo com o contrato do backend.
    const createObjectUrl = vi.fn((_blob: Blob) => "blob:jogos");
    const revokeObjectUrl = vi.fn();
    Object.assign(URL, { createObjectURL: createObjectUrl, revokeObjectURL: revokeObjectUrl });
    const anchorClick = vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(() => undefined);

    fireEvent.click(screen.getByRole("button", { name: "Exportar CSV" }));
    expect(anchorClick).toHaveBeenCalled();
    const csvBlob = createObjectUrl.mock.calls[0][0] as Blob;
    const csv = await readBlobText(csvBlob);
    expect(csv).toContain("numero_jogo,dezenas,soma_dezenas");
    expect(csv).toContain('1,"01 02 04 06 08 10 11 12 14 15 17 19 21 23 24",187');

    anchorClick.mockRestore();
  });

  it("restores generator filters from localStorage and clears them on demand", async () => {
    localStorage.setItem(
      "lotoanalytics.gerador.filtros",
      JSON.stringify({
        choices: { primes: null },
        activeFilters: { primes: false },
        count: 20,
        selection: { 1: "include" }
      })
    );

    render(<App authService={noopAuthService} />);
    navigateTo("/gerar-jogos/lotofacil");

    // Preferencias persistidas: quantidade, filtro de primos desligado e dezena incluida.
    expect(await screen.findByLabelText("Quantidade")).toHaveValue(20);
    const primesSwitch = screen.getByRole("button", { name: /Ativar ou desativar o filtro Números primos/ });
    expect(primesSwitch).toHaveAttribute("aria-pressed", "false");
    expect(screen.getByText("nenhuma opção — filtro sem restrição")).toBeInTheDocument();

    // Limpar filtros restaura o padrao e persiste o estado limpo.
    fireEvent.click(screen.getByRole("button", { name: "Limpar filtros" }));
    expect(await screen.findByLabelText("Quantidade")).toHaveValue(10);
    expect(primesSwitch).toHaveAttribute("aria-pressed", "true");
    expect(screen.queryByText("nenhuma opção — filtro sem restrição")).not.toBeInTheDocument();
  });

  it("marks fixed numbers from the previous contest suggestion", async () => {
    render(<App authService={noopAuthService} />);

    navigateTo("/gerar-jogos/lotofacil");
    fireEvent.click(await screen.findByRole("button", { name: "Sugerir 9 fixas (Grupo A)" }));

    expect(await screen.findByText(/9 dezenas fixas em todos os cartões/)).toBeInTheDocument();
    expect(screen.getByText("Nenhum jogo gerado ainda")).toBeInTheDocument();
  });

  it("synchronizes an existing authenticated session with the backend", async () => {
    const authService: AuthService = {
      getSession: async () => ({ accessToken: "token-sessao", username: "usuario.teste" }),
      login: async () => undefined,
      logout: async () => undefined,
      completeLogin: async () => null
    };

    render(
      <App
        authService={authService}
        initialState={{
          apiBaseUrl: "https://localhost:7101",
          auth: null,
          currentUser: null
        }}
      />
    );

    await waitFor(() =>
      expect(mockApiRequests).toContainEqual(
        expect.objectContaining({
          authorization: "Bearer token-sessao",
          method: "GET",
          url: "https://localhost:7101/usuarios/me"
        })
      )
    );
  });

  it("synchronizes the user after the OIDC callback completes", async () => {
    window.history.pushState({}, "", "/auth/callback");
    const authService: AuthService = {
      getSession: async () => null,
      login: async () => undefined,
      logout: async () => undefined,
      completeLogin: async () => ({ accessToken: "token-callback", username: "usuario.callback" })
    };

    render(
      <App
        authService={authService}
        initialState={{
          apiBaseUrl: "https://localhost:7101",
          auth: null,
          currentUser: null
        }}
      />
    );

    expect(await screen.findByRole("heading", { name: "Filtros matemáticos" })).toBeInTheDocument();
    expect(window.location.pathname).toBe("/gerar-jogos/lotofacil");
    expect(mockApiRequests).toContainEqual(
      expect.objectContaining({
        authorization: "Bearer token-callback",
        method: "GET",
        url: "https://localhost:7101/usuarios/me"
      })
    );
  });

  it("loads profile, lottery modes and contest import screens through backend contracts", async () => {
    render(
      <App
        authService={noopAuthService}
        initialState={{
          apiBaseUrl: "https://localhost:7101",
          auth: { accessToken: "token-teste", username: "usuario.teste" },
          currentUser: adminUser
        }}
      />
    );

    // A rota inicial entrega o painel estatistico em tela cheia.
    expect(await screen.findByRole("heading", { name: "Painel estatístico Lotofácil" })).toBeInTheDocument();

    // Perfil continua acessivel por URL direta, fora do painel.
    navigateTo("/perfil");
    fireEvent.click(await screen.findByRole("button", { name: "Carregar perfil" }));
    expect(await screen.findByText("Plano Premium")).toBeInTheDocument();

    // Paginas removidas do menu continuam acessiveis por URL direta.
    navigateTo("/modalidades");
    fireEvent.click(await screen.findByRole("button", { name: "Carregar modalidades" }));
    expect(await screen.findByText("Lotofacil")).toBeInTheDocument();

    navigateTo("/concursos/importar");
    fireEvent.click(await screen.findByRole("button", { name: "Importar concurso" }));
    expect(await screen.findByText("Faixas de premio")).toBeInTheDocument();

    // Admin tambem entra pelo dropdown do usuario.
    fireEvent.click(screen.getByRole("button", { name: /usuario.teste/ }));
    fireEvent.click(await screen.findByRole("menuitem", { name: /Admin/ }));
    fireEvent.change(await screen.findByLabelText("Limite por modalidade"), { target: { value: "2" } });
    fireEvent.change(await screen.findByLabelText("Pausa erro em ms"), { target: { value: "0" } });
    fireEvent.click(screen.getByRole("button", { name: /Atualizar todos/ }));

    // O endpoint de progresso alimenta o log estilo console, modalidade a modalidade.
    const updateLog = await screen.findByLabelText("Log da atualizacao");
    await waitFor(() => expect(updateLog.textContent).toContain("[1/9] Atualizando Lotofacil..."));
    expect(updateLog.textContent).toContain("Atualizando base de todas as loterias...");
    expect(updateLog.textContent).toContain("Retomando do concurso 3744. Ultimo salvo: 3743.");
    expect(updateLog.textContent).toContain(
      "Lotofacil concurso 3744 salvo: 01 02 03 05 07 09 11 13 14 17 19 20 22 24 25"
    );
    expect(updateLog.textContent).toContain("Aguardando 0.2s antes da proxima requisicao...");
    expect(updateLog.textContent).toContain(
      "erro temporario da Caixa no concurso 3746 (tentativa 1). Aguardando 5.0s para tentar novamente..."
    );
    expect(updateLog.textContent).toContain("fim dos sorteios encontrado no concurso 3746. Importacao concluida.");
    expect(updateLog.textContent).toContain(
      "Importacao finalizada. Salvos nesta execucao: 2. Erros: 0. Total no banco: 3745."
    );

    // Eventos desconhecidos (versoes futuras do backend) sao ignorados sem virar linhas invalidas.
    expect(updateLog.textContent).not.toContain("null");
    expect(updateLog.textContent).not.toContain("undefined");
    expect(await screen.findByText("Total importado")).toBeInTheDocument();
    expect(await screen.findByText("3744, 3745")).toBeInTheDocument();
    expect(mockApiRequests).toContainEqual(
      expect.objectContaining({
        method: "POST",
        url: "https://localhost:7101/admin/concursos/atualizar-todos/progresso"
      })
    );
  });

  it("loads detailed statistics and history details with MSW API mocks", async () => {
    render(
      <App
        authService={noopAuthService}
        initialState={{
          apiBaseUrl: "https://localhost:7101",
          auth: { accessToken: "token-teste", username: "usuario.teste" },
          currentUser: premiumUser
        }}
      />
    );

    navigateTo("/estatisticas/detalhes");
    fireEvent.click(await screen.findByRole("button", { name: "Calcular detalhes" }));
    expect(await screen.findByText("Maior sequencia")).toBeInTheDocument();
    expect(screen.getByText("Linhas")).toBeInTheDocument();

    navigateTo("/historicos");
    fireEvent.click(await screen.findByRole("button", { name: "Carregar historicos" }));
    expect(await screen.findByText("#11111111-1111-1111-1111-111111111111")).toBeInTheDocument();
    fireEvent.click(screen.getAllByRole("button", { name: "Detalhes" })[0]);
    expect(await screen.findByText("Detalhe da geracao #11111111-1111-1111-1111-111111111111")).toBeInTheDocument();

    fireEvent.click(screen.getAllByRole("button", { name: "Detalhes" })[1]);
    expect(await screen.findByText("Detalhe da conferencia #21212121-2121-2121-2121-212121212121")).toBeInTheDocument();
  });
});
