import {
  createRootRoute,
  createRoute,
  createRouter,
  Link,
  Outlet,
  redirect,
  RouterProvider,
  useNavigate
} from "@tanstack/react-router";
import {
  ChevronDown,
  Dices,
  LayoutDashboard,
  LogOut,
  RefreshCw,
  ShieldCheck,
  UserCircle
} from "lucide-react";
import { FormEvent, useEffect, useRef, useState } from "react";
import {
  ApiClient,
  CheckGamesResponse,
  CheckingHistoryResponse,
  ContestBulkUpdateResponse,
  ContestBulkUpdateStreamEvent,
  ContestImportResponse,
  CurrentUserResponse,
  GenerationHistoryResponse,
  LotteryModeResponse,
  LotofacilStatisticsResponse,
  parseGames,
  parseNumbers
} from "./lib/apiClient";
import { AppState, AppStateContext, useAppState } from "./lib/appState";
import { AuthService, AuthSession, keycloakAuthService } from "./lib/auth";
import { GeneratorLotofacil } from "./features/generator/GeneratorLotofacil";
import { GeneratorMegaSena } from "./features/generator/GeneratorMegaSena";
import { PrivacyPolicyPage } from "./features/legal/PrivacyPolicyPage";
import { DashboardLotofacil } from "./features/dashboard/DashboardLotofacil";
import { DashboardMegaSena } from "./features/dashboard/DashboardMegaSena";
import { AdBlockGate } from "./features/adblock/AdBlockGate";
import "./styles.css";

const defaultState: AppState = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5291",
  auth: null,
  currentUser: null
};

export function App({
  authService = keycloakAuthService,
  initialState = defaultState
}: {
  authService?: AuthService;
  initialState?: AppState;
}) {
  const [state, setState] = useState(initialState);
  // O roteador e criado por instancia do App para nao vazar estado entre montagens.
  const [router] = useState(() => createAppRouter());

  useEffect(() => {
    let active = true;

    authService.getSession().then((session) => {
      if (active && session) {
        setState((current) => ({ ...current, auth: session }));
        void syncCurrentUser(initialState.apiBaseUrl, session)
          .then((currentUser) => {
            if (active) {
              setState((current) => ({ ...current, currentUser }));
            }
          })
          .catch(() => undefined);
      }
    });

    return () => {
      active = false;
    };
  }, [authService]);

  return (
    <AppStateContext.Provider value={{ state, setState, authService }}>
      <RouterProvider router={router} />
      <AdBlockGate />
    </AppStateContext.Provider>
  );
}

// Cria as rotas type-safe da aplicacao usando TanStack Router.
function createAppRouter() {
  const rootRoute = createRootRoute({
    component: Shell
  });
  const dashboardLotofacilRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/dashboard/lotofacil",
    component: DashboardLotofacil
  });
  const dashboardMegaSenaRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/dashboard/mega-sena",
    component: DashboardMegaSena
  });
  const generatorRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/gerar-jogos/lotofacil",
    component: GeneratorLotofacil
  });
  const generatorMegaSenaRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/gerar-jogos/mega-sena",
    component: GeneratorMegaSena
  });
  const legacyGeneratorRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/gerador",
    beforeLoad: () => {
      throw redirect({ to: "/gerar-jogos/lotofacil", replace: true });
    }
  });
  const checkerRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/conferidor",
    component: CheckerPage
  });
  const historyRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/historicos",
    component: HistoryPage
  });
  const profileRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/perfil",
    component: ProfilePage
  });
  const modesRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/modalidades",
    component: LotteryModesPage
  });
  const contestImportRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/concursos/importar",
    component: ContestImportPage
  });
  const adminContestUpdateRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/admin/concursos",
    component: AdminContestUpdatePage
  });
  const adminLoginRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/adm/login",
    component: AdminLoginPage
  });
  const detailedStatisticsRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/estatisticas/detalhes",
    component: DetailedStatisticsPage
  });
  const callbackRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/auth/callback",
    component: AuthCallbackPage
  });
  const privacyRoute = createRoute({
    getParentRoute: () => rootRoute,
    path: "/privacidade",
    component: PrivacyPolicyPage
  });
  const routeTree = rootRoute.addChildren([
    dashboardLotofacilRoute,
    dashboardMegaSenaRoute,
    generatorRoute,
    generatorMegaSenaRoute,
    legacyGeneratorRoute,
    checkerRoute,
    historyRoute,
    profileRoute,
    modesRoute,
    contestImportRoute,
    adminContestUpdateRoute,
    adminLoginRoute,
    detailedStatisticsRoute,
    callbackRoute,
    privacyRoute
  ]);

  // Qualquer rota nao mapeada (incluindo "/" e "/dashboard" sem modalidade) cai no 404.
  return createRouter({ routeTree, defaultNotFoundComponent: NotFoundPage });
}

// Pagina 404 exibida para caminhos sem rota, como "/" e "/dashboard" sem modalidade.
function NotFoundPage() {
  return (
    <section className="page">
      <PageHeader title="Página não encontrada" description="O endereço acessado não existe ou ainda não foi implementado." />
      <div className="panel actions">
        <Link to="/dashboard/lotofacil">Ir para o painel da Lotofácil</Link>
      </div>
    </section>
  );
}

// Marca do LotoAnalytics: mesmo logo do favicon (barras de analise + bola da sorte).
function BrandLogo() {
  return (
    <svg width={22} height={22} viewBox="0 0 48 48" aria-hidden="true" focusable="false">
      <rect x="2" y="2" width="44" height="44" rx="11" fill="#1F6F8B" />
      <rect x="11" y="26" width="6" height="11" rx="2" fill="#ffffff" />
      <rect x="21" y="21" width="6" height="16" rx="2" fill="#ffffff" />
      <rect x="31" y="16" width="6" height="21" rx="2" fill="#ffffff" opacity="0.92" />
      <circle cx="34" cy="13" r="5" fill="#F6C445" />
      <circle cx="32.3" cy="11.3" r="1.5" fill="#ffffff" opacity="0.85" />
    </svg>
  );
}

function Shell() {
  const { state, setState, authService } = useAppState();
  const isAdmin = hasRole(state.currentUser, "administrador");
  const [userMenuOpen, setUserMenuOpen] = useState(false);
  const userMenuRef = useRef<HTMLDivElement | null>(null);

  // Fecha o dropdown do usuario ao clicar fora dele.
  useEffect(() => {
    if (!userMenuOpen) {
      return;
    }

    function handleOutsideClick(event: MouseEvent) {
      if (userMenuRef.current && !userMenuRef.current.contains(event.target as Node)) {
        setUserMenuOpen(false);
      }
    }

    document.addEventListener("mousedown", handleOutsideClick);
    return () => document.removeEventListener("mousedown", handleOutsideClick);
  }, [userMenuOpen]);

  async function logout() {
    setUserMenuOpen(false);
    await authService.logout();
    setState({ ...state, auth: null, currentUser: null });
  }

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <BrandLogo />
          <strong>LotoAnalytics</strong>
        </div>
        <nav aria-label="Principal">
          <Link to="/dashboard/lotofacil" activeProps={{ className: "active" }}>
            <LayoutDashboard size={18} /> Lotofácil
          </Link>
          <Link to="/dashboard/mega-sena" activeProps={{ className: "active" }}>
            <Dices size={18} /> Mega-Sena
          </Link>
        </nav>
      </aside>
      <main>
        <section className="toolbar" aria-label="Sessao do usuario">
          <div className="auth-status">
            {state.auth ? (
              <div className="user-menu" ref={userMenuRef}>
                <button
                  type="button"
                  className="user-menu-trigger"
                  aria-haspopup="menu"
                  aria-expanded={userMenuOpen}
                  onClick={() => setUserMenuOpen((open) => !open)}
                >
                  <UserCircle size={16} /> {state.auth.username ?? "Conta"} <ChevronDown size={14} />
                </button>
                {userMenuOpen && (
                  <div className="user-menu-dropdown" role="menu" aria-label="Menu do usuario">
                    <Link to="/perfil" role="menuitem" onClick={() => setUserMenuOpen(false)}>
                      <UserCircle size={16} /> Perfil
                    </Link>
                    {isAdmin && (
                      <Link to="/admin/concursos" role="menuitem" onClick={() => setUserMenuOpen(false)}>
                        <ShieldCheck size={16} /> Admin
                      </Link>
                    )}
                    <button type="button" role="menuitem" onClick={logout}>
                      <LogOut size={16} /> Sair
                    </button>
                  </div>
                )}
              </div>
            ) : null}
          </div>
        </section>
        <Outlet />
        <footer className="app-footer">
          <Link to="/privacidade">Política de Privacidade</Link>
        </footer>
      </main>
    </div>
  );
}

function AdminLoginPage() {
  const { state, authService } = useAppState();

  useEffect(() => {
    if (!state.auth) {
      void authService.login();
    }
  }, [authService, state.auth]);

  return (
    <section className="page">
      <PageHeader title="Acesso administrativo" description="Redirecionando para a autenticação segura…" />
    </section>
  );
}

function AuthCallbackPage() {
  const { state, setState, authService } = useAppState();
  const [status, setStatus] = useState("Finalizando login");
  const navigate = useNavigate();

  useEffect(() => {
    authService
      .completeLogin()
      .then((session) => {
        setState({ ...state, auth: session });
        if (!session) {
          setStatus("Login concluido");
          return;
        }

        return syncCurrentUser(state.apiBaseUrl, session).then((currentUser) => {
          setState({ ...state, auth: session, currentUser });
          setStatus("Login concluido");
          return navigate({ to: "/gerar-jogos/lotofacil", replace: true });
        });
      })
      .catch((error: unknown) => setStatus(error instanceof Error ? error.message : "Falha ao concluir login"));
  }, []);

  return (
    <section className="page">
      <PageHeader title="Login" description={status} />
    </section>
  );
}

// Sincroniza o usuario autenticado no backend da aplicacao.
async function syncCurrentUser(apiBaseUrl: string, session: AuthSession) {
  const client = new ApiClient({ baseUrl: apiBaseUrl, token: session.accessToken });
  return await client.getJson<CurrentUserResponse>("/usuarios/me");
}

function DetailedStatisticsPage() {
  const { state } = useAppState();
  const [numbers, setNumbers] = useState("01 02 03 04 05 06 07 08 09 10 11 12 13 14 15");
  const [previousNumbers, setPreviousNumbers] = useState("01 02 04 06 08 10 12 14 16 18 20 21 22 23 24");
  const [statistics, setStatistics] = useState<LotofacilStatisticsResponse | null>(null);
  const [status, setStatus] = useState("Pronto");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setStatus("Calculando");
    try {
      const client = new ApiClient({ baseUrl: state.apiBaseUrl, token: state.auth?.accessToken });
      const result = await client.postJson("/estatisticas/lotofacil/calcular", {
        dezenas: parseNumbers(numbers),
        dezenasAnteriores: parseNumbers(previousNumbers)
      });
      setStatistics(result as LotofacilStatisticsResponse);
      setStatus("Calculado");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Erro desconhecido");
    }
  }

  return (
    <section className="page">
      <PageHeader title="Estatisticas detalhadas" description="Analise paridade, soma, repetidas, linhas, colunas, moldura e miolo." />
      <form className="panel grid-two" onSubmit={submit}>
        <TextArea label="Dezenas sorteadas" value={numbers} onChange={setNumbers} />
        <TextArea label="Concurso anterior" value={previousNumbers} onChange={setPreviousNumbers} />
        <button type="submit">Calcular detalhes</button>
        <Status text={status} />
      </form>
      {statistics && (
        <>
          <div className="metric-grid">
            <Metric label="Pares" value={statistics.quantidadePares} />
            <Metric label="Impares" value={statistics.quantidadeImpares} />
            <Metric label="Soma" value={statistics.somaDezenas} />
            <Metric label="Repetidas" value={statistics.repetidasAnterior.join(" ")} />
            <Metric label="Primos" value={statistics.quantidadePrimos} />
            <Metric label="Moldura" value={statistics.quantidadeMoldura} />
            <Metric label="Miolo" value={statistics.quantidadeMiolo} />
            <Metric label="Maior sequencia" value={statistics.maiorSequencia} />
          </div>
          <div className="split">
            <DistributionPanel title="Linhas" values={statistics.distribuicaoLinhas} />
            <DistributionPanel title="Colunas" values={statistics.distribuicaoColunas} />
          </div>
        </>
      )}
    </section>
  );
}

function CheckerPage() {
  const { state } = useAppState();
  const [drawn, setDrawn] = useState("01 02 03 04 05 06 07 08 09 10 11 12 13 14 15");
  const [gamesInput, setGamesInput] = useState("01 02 03 04 05 06 07 08 09 10 11 12 13 14 15\n01 02 03 04 05 06 07 08 09 10 11 16 17 18 19");
  const [result, setResult] = useState<CheckGamesResponse | null>(null);
  const [status, setStatus] = useState("Pronto para conferir");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setStatus("Conferindo");
    try {
      const client = new ApiClient({ baseUrl: state.apiBaseUrl, token: state.auth?.accessToken });
      const response = await client.postJson("/conferencias/lotofacil/conferir", {
        dezenasSorteadas: parseNumbers(drawn),
        jogos: parseGames(gamesInput)
      });
      setResult(response as CheckGamesResponse);
      setStatus("Conferencia salva");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Erro desconhecido");
    }
  }

  return (
    <section className="page">
      <PageHeader title="Conferidor" description="Confira jogos, veja acertos e salve historico do usuario." />
      <form className="panel grid-two" onSubmit={submit}>
        <TextArea label="Dezenas sorteadas" value={drawn} onChange={setDrawn} />
        <TextArea label="Jogos" value={gamesInput} onChange={setGamesInput} rows={5} />
        <button type="submit">Conferir jogos</button>
        <Status text={status} />
      </form>
      {result && (
        <div className="panel">
          <h2>Resultado</h2>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Jogo</th>
                  <th>Acertos</th>
                  <th>Dezenas acertadas</th>
                  <th>Premiado</th>
                </tr>
              </thead>
              <tbody>
                {result.jogos.map((game) => (
                  <tr key={game.numeroJogo}>
                    <td>{game.numeroJogo}</td>
                    <td>{game.quantidadeAcertos}</td>
                    <td>{game.dezenasAcertadas.join(" ")}</td>
                    <td>{game.premiado ? "Sim" : "Nao"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </section>
  );
}

function HistoryPage() {
  const { state } = useAppState();
  const [generations, setGenerations] = useState<GenerationHistoryResponse | null>(null);
  const [checkings, setCheckings] = useState<CheckingHistoryResponse | null>(null);
  const [selectedGenerationId, setSelectedGenerationId] = useState<string | null>(null);
  const [selectedCheckingId, setSelectedCheckingId] = useState<string | null>(null);
  const [csv, setCsv] = useState("");
  const [status, setStatus] = useState("Historicos nao carregados");

  async function load() {
    setStatus("Carregando");
    try {
      const client = new ApiClient({ baseUrl: state.apiBaseUrl, token: state.auth?.accessToken });
      const [generationResult, checkingResult] = await Promise.all([
        client.getJson<GenerationHistoryResponse>("/usuarios/me/geracoes"),
        client.getJson<CheckingHistoryResponse>("/usuarios/me/conferencias")
      ]);
      setGenerations(generationResult);
      setCheckings(checkingResult);
      setStatus("Historicos carregados");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Erro desconhecido");
    }
  }

  async function exportCsv(id: string) {
    try {
      const client = new ApiClient({ baseUrl: state.apiBaseUrl, token: state.auth?.accessToken });
      setCsv(await client.getText(`/usuarios/me/geracoes/${id}/exportar-csv`));
    } catch (error) {
      setCsv(error instanceof Error ? error.message : "Erro desconhecido");
    }
  }

  return (
    <section className="page">
      <PageHeader title="Historicos" description="Acompanhe geracoes e conferencias salvas no backend." />
      <div className="panel actions">
        <button type="button" onClick={load}>
          Carregar historicos
        </button>
        <Status text={status} />
      </div>
      <div className="split">
        <HistoryList
          title="Geracoes"
          items={generations?.geracoes ?? []}
          onExportCsv={exportCsv}
          onSelect={(id) => setSelectedGenerationId(id)}
        />
        <CheckingList items={checkings?.conferencias ?? []} onSelect={(id) => setSelectedCheckingId(id)} />
      </div>
      <GenerationDetails generation={(generations?.geracoes ?? []).find((item) => item.id === selectedGenerationId) ?? null} />
      <CheckingDetails checking={(checkings?.conferencias ?? []).find((item) => item.id === selectedCheckingId) ?? null} />
      {csv && (
        <pre className="csv-preview" aria-label="CSV exportado">
          {csv}
        </pre>
      )}
    </section>
  );
}

function ProfilePage() {
  const { state } = useAppState();
  const [profile, setProfile] = useState<CurrentUserResponse | null>(null);
  const [status, setStatus] = useState("Perfil nao carregado");

  async function load() {
    setStatus("Carregando");
    try {
      const client = new ApiClient({ baseUrl: state.apiBaseUrl, token: state.auth?.accessToken });
      setProfile(await client.getJson<CurrentUserResponse>("/usuarios/me"));
      setStatus("Perfil carregado");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Erro desconhecido");
    }
  }

  return (
    <section className="page">
      <PageHeader title="Perfil" description="Dados sincronizados pelo Keycloak e plano atual do usuario." />
      <div className="panel actions">
        <button type="button" onClick={load}>
          Carregar perfil
        </button>
        <Status text={status} />
      </div>
      {profile && (
        <div className="metric-grid">
          <Metric label="Usuario" value={profile.username ?? "Sem usuario"} />
          <Metric label="Email" value={profile.email ?? "Sem email"} />
          <Metric label="Subject" value={profile.subject} />
          <Metric label="Roles" value={profile.roles.length > 0 ? profile.roles.join(", ") : "Sem roles"} />
          <Metric label="Plano" value={profile.planoAtual?.nome ?? "Sem plano"} />
          <Metric label="Limite por geracao" value={profile.planoAtual?.limiteJogosPorGeracao ?? 0} />
          <Metric label="CSV" value={profile.planoAtual?.permiteExportarCsv ? "Liberado" : "Bloqueado"} />
          <Metric label="PDF" value={profile.planoAtual?.permiteExportarPdf ? "Liberado" : "Bloqueado"} />
        </div>
      )}
    </section>
  );
}

function LotteryModesPage() {
  const { state } = useAppState();
  const [modes, setModes] = useState<LotteryModeResponse[]>([]);
  const [status, setStatus] = useState("Modalidades nao carregadas");

  async function load() {
    setStatus("Carregando");
    try {
      const client = new ApiClient({ baseUrl: state.apiBaseUrl, token: state.auth?.accessToken });
      setModes(await client.getJson<LotteryModeResponse[]>("/modalidades"));
      setStatus("Modalidades carregadas");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Erro desconhecido");
    }
  }

  return (
    <section className="page">
      <PageHeader title="Modalidades" description="Lista as modalidades ativas cadastradas no banco." />
      <div className="panel actions">
        <button type="button" onClick={load}>
          Carregar modalidades
        </button>
        <Status text={status} />
      </div>
      <div className="panel table-wrap">
        <table>
          <thead>
            <tr>
              <th>Codigo</th>
              <th>Nome</th>
              <th>Tipo Caixa</th>
              <th>Dezenas</th>
              <th>Ativa</th>
            </tr>
          </thead>
          <tbody>
            {modes.map((mode) => (
              <tr key={mode.codigo}>
                <td>{mode.codigo}</td>
                <td>{mode.nome}</td>
                <td>{mode.tipoJogoCaixa}</td>
                <td>{mode.quantidadeDezenasPrincipal}</td>
                <td>{mode.ativa ? "Sim" : "Nao"}</td>
              </tr>
            ))}
          </tbody>
        </table>
        {modes.length === 0 && <p className="muted">Nenhuma modalidade carregada.</p>}
      </div>
    </section>
  );
}

function ContestImportPage() {
  const { state } = useAppState();
  const [modeCode, setModeCode] = useState("lotofacil");
  const [contestNumber, setContestNumber] = useState(1);
  const [result, setResult] = useState<ContestImportResponse | null>(null);
  const [status, setStatus] = useState("Informe o concurso para importar");

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setStatus("Importando");
    try {
      const client = new ApiClient({ baseUrl: state.apiBaseUrl, token: state.auth?.accessToken });
      const response = await client.postJson<null, ContestImportResponse>(`/concursos/${modeCode}/${contestNumber}/importar`, null);
      setResult(response);
      setStatus("Concurso importado");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Erro desconhecido");
    }
  }

  return (
    <section className="page">
      <PageHeader title="Importar concurso" description="Busca o resultado oficial e salva o concurso no PostgreSQL." />
      <form className="panel grid-two" onSubmit={submit}>
        <label>
          Codigo da modalidade
          <input value={modeCode} onChange={(event) => setModeCode(event.target.value)} />
        </label>
        <label>
          Numero do concurso
          <input type="number" min={1} value={contestNumber} onChange={(event) => setContestNumber(Number(event.target.value))} />
        </label>
        <button type="submit">Importar concurso</button>
        <Status text={status} />
      </form>
      {result && (
        <div className="metric-grid">
          <Metric label="Modalidade" value={result.codigoModalidade} />
          <Metric label="Concurso" value={result.numeroConcurso} />
          <Metric label="Dezenas" value={result.quantidadeDezenasPrincipal} />
          <Metric label="Faixas de premio" value={result.quantidadeFaixasPremio} />
        </div>
      )}
    </section>
  );
}

function AdminContestUpdatePage() {
  const { state } = useAppState();
  const [startAt, setStartAt] = useState("");
  const [limitPerMode, setLimitPerMode] = useState("");
  const [delayMilliseconds, setDelayMilliseconds] = useState(200);
  // Padrao interativo curto: a espera longa de 5 minutos fica para o atualizador em background.
  const [errorDelayMilliseconds, setErrorDelayMilliseconds] = useState(5000);
  const [maxErrorAttempts, setMaxErrorAttempts] = useState("3");
  const [result, setResult] = useState<ContestBulkUpdateResponse | null>(null);
  const [status, setStatus] = useState("Pronto para atualizar concursos");
  const [progressLog, setProgressLog] = useState<string[]>([]);
  const logRef = useRef<HTMLPreElement | null>(null);

  // Mantem o log rolado para a linha mais recente a cada evento.
  useEffect(() => {
    if (logRef.current) {
      logRef.current.scrollTop = logRef.current.scrollHeight;
    }
  }, [progressLog]);

  if (!hasRole(state.currentUser, "administrador")) {
    return (
      <section className="page">
        <PageHeader title="Admin concursos" description="Acesso restrito a administradores." />
      </section>
    );
  }

  // Registra linhas no log de progresso mantendo apenas as ultimas 500 entradas.
  function appendProgress(...lines: string[]) {
    setProgressLog((log) => [...log, ...lines].slice(-500));
  }

  // Trata cada evento NDJSON emitido pelo backend, montando o log estilo console.
  function handleUpdateEvent(event: ContestBulkUpdateStreamEvent) {
    if (event.evento === "concluido") {
      setResult(event.resultado);
      setStatus("Atualizacao concluida");
      return;
    }

    if (event.evento === "modalidade_iniciada") {
      appendProgress(
        "",
        `[${event.indiceModalidade}/${event.totalModalidades}] Atualizando ${event.nomeModalidade}...`,
        `Retomando do concurso ${event.retomarDoConcurso}. Ultimo salvo: ${event.ultimoConcursoSalvo ?? "nenhum"}.`
      );
      setStatus(`[${event.indiceModalidade}/${event.totalModalidades}] Atualizando ${event.nomeModalidade}`);
      return;
    }

    if (event.evento === "tentativa_falhou") {
      appendProgress(
        `erro temporario da Caixa no concurso ${event.numeroConcurso} (tentativa ${event.tentativa}). Aguardando ${((event.aguardarMs ?? 0) / 1000).toFixed(1)}s para tentar novamente...`
      );
      return;
    }

    if (event.evento === "concurso_importado") {
      const lines = [`${event.nomeModalidade} concurso ${event.numeroConcurso} salvo: ${(event.dezenas ?? []).join(" ")}`];
      if (delayMilliseconds > 0) {
        lines.push(`Aguardando ${(delayMilliseconds / 1000).toFixed(1)}s antes da proxima requisicao...`);
      }
      appendProgress(...lines);
      setStatus(`Importando ${event.nomeModalidade}: concurso ${event.numeroConcurso} (${event.quantidadeImportada} na modalidade)`);
      return;
    }

    if (event.evento === "modalidade_concluida") {
      const lines: string[] = [];
      if (event.status === "falhou") {
        lines.push(`erro na importacao: ${event.erro ?? "desconhecido"}`);
      } else {
        lines.push(`fim dos sorteios encontrado no concurso ${event.proximoConcurso}. Importacao concluida.`);
      }
      lines.push(
        `Importacao finalizada. Salvos nesta execucao: ${event.quantidadeImportada}. Erros: ${event.erro ? 1 : 0}. Total no banco: ${event.totalNoBanco}.`
      );
      appendProgress(...lines);
    }

    // Eventos desconhecidos (backend mais novo que o front) sao ignorados de proposito.
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setStatus("Atualizando concursos");
    setResult(null);
    setProgressLog(["Atualizando base de todas as loterias..."]);

    try {
      const client = new ApiClient({ baseUrl: state.apiBaseUrl, token: state.auth?.accessToken });
      await client.postJsonStream<unknown, ContestBulkUpdateStreamEvent>(
        "/admin/concursos/atualizar-todos/progresso",
        {
          inicio: startAt ? Number(startAt) : undefined,
          limitePorModalidade: limitPerMode ? Number(limitPerMode) : undefined,
          pausaMs: delayMilliseconds,
          pausaErroMs: errorDelayMilliseconds,
          maxTentativasErro: maxErrorAttempts ? Number(maxErrorAttempts) : undefined
        },
        handleUpdateEvent
      );
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Erro desconhecido");
    }
  }

  return (
    <section className="page">
      <PageHeader title="Admin concursos" description="Atualize todas as modalidades buscando novos resultados oficiais da Caixa." />
      <form className="panel grid-three" onSubmit={submit}>
        <label>
          Inicio opcional
          <input
            type="number"
            min={1}
            value={startAt}
            placeholder="Retomar automaticamente"
            onChange={(event) => setStartAt(event.target.value)}
          />
        </label>
        <label>
          Limite por modalidade
          <input
            type="number"
            min={1}
            max={1000}
            value={limitPerMode}
            placeholder="Sem limite"
            onChange={(event) => setLimitPerMode(event.target.value)}
          />
        </label>
        <label>
          Pausa em ms
          <input
            type="number"
            min={0}
            max={10000}
            value={delayMilliseconds}
            onChange={(event) => setDelayMilliseconds(Number(event.target.value))}
          />
        </label>
        <label>
          Pausa erro em ms
          <input
            type="number"
            min={0}
            max={3600000}
            value={errorDelayMilliseconds}
            onChange={(event) => setErrorDelayMilliseconds(Number(event.target.value))}
          />
        </label>
        <label>
          Tentativas por erro
          <input
            type="number"
            min={1}
            max={100}
            value={maxErrorAttempts}
            placeholder="Sem limite"
            onChange={(event) => setMaxErrorAttempts(event.target.value)}
          />
        </label>
        <button type="submit">
          <RefreshCw size={16} /> Atualizar todos
        </button>
        <Status text={status} />
      </form>
      {progressLog.length > 0 && (
        <div className="panel">
          <h2>Progresso da atualizacao</h2>
          <pre className="update-log" aria-label="Log da atualizacao" ref={logRef}>
            {progressLog.join("\n")}
          </pre>
        </div>
      )}
      {result && (
        <>
          <div className="metric-grid">
            <Metric label="Total importado" value={result.totalImportado} />
            <Metric label="Modalidades" value={result.modalidades.length} />
            <Metric label="Inicio" value={formatDateTime(result.inicioEm)} />
            <Metric label="Fim" value={formatDateTime(result.finalizadoEm)} />
          </div>
          <div className="panel table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Modalidade</th>
                  <th>Status</th>
                  <th>Importados</th>
                  <th>Concurso inicial</th>
                  <th>Proximo concurso</th>
                  <th>Concursos</th>
                  <th>Erro</th>
                </tr>
              </thead>
              <tbody>
                {result.modalidades.map((mode) => (
                  <tr key={mode.codigoModalidade}>
                    <td>{mode.nomeModalidade}</td>
                    <td>
                      <span className={`badge badge-${mode.status}`}>{formatStatus(mode.status)}</span>
                    </td>
                    <td>{mode.quantidadeImportada}</td>
                    <td>{mode.concursoInicial}</td>
                    <td>{mode.proximoConcurso}</td>
                    <td>{summarizeContests(mode.concursosImportados)}</td>
                    <td>{mode.erro ?? ""}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </section>
  );
}

function PageHeader({ title, description }: { title: string; description: string }) {
  return (
    <header className="page-header">
      <h1>{title}</h1>
      <p>{description}</p>
    </header>
  );
}

function TextArea({ label, value, onChange, rows = 3 }: { label: string; value: string; onChange: (value: string) => void; rows?: number }) {
  return (
    <label>
      {label}
      <textarea rows={rows} value={value} onChange={(event) => onChange(event.target.value)} />
    </label>
  );
}

function Status({ text }: { text: string }) {
  return <span className="status">{text}</span>;
}

function Metric({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="metric">
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short"
  }).format(new Date(value));
}

function formatStatus(value: string) {
  return value.replaceAll("_", " ");
}

function summarizeContests(values: number[]) {
  if (values.length === 0) {
    return "";
  }

  if (values.length <= 8) {
    return values.join(", ");
  }

  return `${values[0]} - ${values[values.length - 1]}`;
}

function hasRole(currentUser: CurrentUserResponse | null, role: string) {
  return currentUser?.roles.includes(role) ?? false;
}

function DistributionPanel({ title, values }: { title: string; values: number[] }) {
  return (
    <div className="panel">
      <h2>{title}</h2>
      <div className="distribution">
        {values.map((value, index) => (
          <div className="distribution-row" key={`${title}-${index}`}>
            <span>{index + 1}</span>
            <strong>{value}</strong>
          </div>
        ))}
      </div>
    </div>
  );
}

function HistoryList({
  title,
  items,
  onExportCsv,
  onSelect
}: {
  title: string;
  items: GenerationHistoryResponse["geracoes"];
  onExportCsv: (id: string) => void;
  onSelect: (id: string) => void;
}) {
  return (
    <div className="panel">
      <h2>{title}</h2>
      {items.length === 0 ? (
        <p className="muted">Sem geracoes.</p>
      ) : (
        items.map((item) => (
          <div className="history-item" key={item.id}>
            <strong>#{item.id}</strong>
            <span>{item.quantidadeJogos} jogos</span>
            <span className="button-group">
              <button type="button" onClick={() => onSelect(item.id)}>
                Detalhes
              </button>
              <button type="button" onClick={() => onExportCsv(item.id)}>
                CSV
              </button>
            </span>
          </div>
        ))
      )}
    </div>
  );
}

function CheckingList({ items, onSelect }: { items: CheckingHistoryResponse["conferencias"]; onSelect: (id: string) => void }) {
  return (
    <div className="panel">
      <h2>Conferencias</h2>
      {items.length === 0 ? (
        <p className="muted">Sem conferencias.</p>
      ) : (
        items.map((item) => (
          <div className="history-item" key={item.id}>
            <strong>#{item.id}</strong>
            <span>{item.quantidadeJogos} jogos</span>
            <button type="button" onClick={() => onSelect(item.id)}>
              Detalhes
            </button>
          </div>
        ))
      )}
    </div>
  );
}

function GenerationDetails({ generation }: { generation: GenerationHistoryResponse["geracoes"][number] | null }) {
  if (!generation) {
    return null;
  }

  return (
    <div className="panel table-wrap">
      <h2>Detalhe da geracao #{generation.id}</h2>
      <table>
        <thead>
          <tr>
            <th>Jogo</th>
            <th>Dezenas</th>
            <th>Soma</th>
          </tr>
        </thead>
        <tbody>
          {generation.jogos.map((game) => (
            <tr key={game.numeroJogo}>
              <td>{game.numeroJogo}</td>
              <td>{game.dezenas.join(" ")}</td>
              <td>{game.somaDezenas}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function CheckingDetails({ checking }: { checking: CheckingHistoryResponse["conferencias"][number] | null }) {
  if (!checking) {
    return null;
  }

  return (
    <div className="panel table-wrap">
      <h2>Detalhe da conferencia #{checking.id}</h2>
      <div className="award-summary">
        {[11, 12, 13, 14, 15].map((hits) => (
          <Metric key={hits} label={`${hits} acertos`} value={checking.resumoPremiacao[String(hits)] ?? 0} />
        ))}
      </div>
      <table>
        <thead>
          <tr>
            <th>Jogo</th>
            <th>Acertos</th>
            <th>Dezenas acertadas</th>
          </tr>
        </thead>
        <tbody>
          {checking.jogos.map((game) => (
            <tr key={game.numeroJogo}>
              <td>{game.numeroJogo}</td>
              <td>{game.quantidadeAcertos}</td>
              <td>{game.dezenasAcertadas.join(" ")}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
