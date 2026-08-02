import { ChangeEvent, MouseEvent, useEffect, useMemo, useRef, useState } from "react";
import { Link } from "@tanstack/react-router";
import { ArrowLeft } from "lucide-react";
import {
  ApiClient,
  FilterStatisticsResponse,
  GenerateGamesRequest,
  GenerateMegaSenaGamesResponse,
  LatestContestResponse,
  LotteryModeResponse
} from "../../lib/apiClient";
import { buildGamesCsv, buildGamesScript } from "./gameExport";
import { loadMegaSenaPreferences, saveMegaSenaPreferences } from "./megaSenaPreferences";
import { useAppState } from "../../lib/appState";
import {
  ActiveFilterState,
  BOARD_SIZE,
  ChoiceState,
  DEFAULT_ACTIVE_FILTERS,
  DEFAULT_CHOICES,
  FILTER_LABELS,
  FILTER_ORDER,
  FilterKey,
  GAME_SIZE,
  INITIAL_CHOICES,
  OPTION_VALUES,
  PREVIOUS_DRAW,
  STRATEGY_WEIGHTS,
  buildFilterPayload,
  buildLiveFilterStats,
  computeRepetitionAverage
} from "./megaSenaEngine";
import "./generator.css";

type NumberSelection = "include" | "exclude";
type SelectionState = Record<number, NumberSelection>;
type AlertTone = "neutral" | "danger" | "warn" | "ok";

const pad2 = (value: number) => String(value).padStart(2, "0");
const formatPercent = (value: number) => `${value.toFixed(1).replace(".", ",")}%`;
const formatInt = (value: number) => value.toLocaleString("pt-BR");
const formatCurrency = (value: number) => value.toFixed(2).replace(".", ",");
const ADSENSE_CLIENT = import.meta.env.VITE_ADSENSE_CLIENT ?? "";
const ADSENSE_SLOTS = {
  leaderboard: import.meta.env.VITE_ADSENSE_SLOT_LEADERBOARD ?? "",
  rectangle: import.meta.env.VITE_ADSENSE_SLOT_RECTANGLE ?? "",
  halfPage: import.meta.env.VITE_ADSENSE_SLOT_HALF_PAGE ?? "",
  sidebarExtra: import.meta.env.VITE_ADSENSE_SLOT_SIDEBAR_EXTRA ?? "",
  responsive: import.meta.env.VITE_ADSENSE_SLOT_RESPONSIVE ?? ""
};

// Espaco de anuncio AdSense estavel: mantem a caixa mesmo quando o provedor nao carrega.
function AdSpace({
  slot,
  format,
  className
}: {
  slot: string;
  format: "728x90" | "300x250" | "300x600" | "auto";
  className: string;
}) {
  useEffect(() => {
    if (!ADSENSE_CLIENT || !slot) {
      return;
    }
    try {
      const adsWindow = window as Window & { adsbygoogle?: unknown[] };
      (adsWindow.adsbygoogle = adsWindow.adsbygoogle || []).push({});
    } catch {
      // O espaço reservado permanece estável quando o provedor bloqueia o anúncio.
    }
  }, [slot]);

  return (
    <div className={`gen-ad ${className}`}>
      <span>Publicidade</span>
      <ins
        className="adsbygoogle"
        data-ad-client={ADSENSE_CLIENT || undefined}
        data-ad-slot={slot || undefined}
        data-ad-format={format === "auto" ? "auto" : undefined}
        data-full-width-responsive={format === "auto" ? "true" : undefined}
        aria-hidden={!ADSENSE_CLIENT || !slot}
      />
    </div>
  );
}

// Tela de geracao de jogos da Mega-Sena: monta os filtros estatisticos e delega a geracao a API.
export function GeneratorMegaSena() {
  const { state } = useAppState();
  // Preferencias persistidas localmente sao carregadas uma unica vez na montagem.
  const [storedPreferences] = useState(() => loadMegaSenaPreferences());
  const [selection, setSelection] = useState<SelectionState>(() => storedPreferences?.selection ?? {});
  const [openFilter, setOpenFilter] = useState<FilterKey>("parity");
  const [choices, setChoices] = useState<ChoiceState>(() => storedPreferences?.choices ?? { ...INITIAL_CHOICES });
  const [activeFilters, setActiveFilters] = useState<ActiveFilterState>(
    () => storedPreferences?.activeFilters ?? { ...DEFAULT_ACTIVE_FILTERS }
  );
  const [count, setCount] = useState(() => storedPreferences?.count ?? 10);
  const [result, setResult] = useState<GenerateMegaSenaGamesResponse | null>(null);
  const [generating, setGenerating] = useState(false);
  const [stale, setStale] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [scriptVisible, setScriptVisible] = useState(false);
  const [scriptCopied, setScriptCopied] = useState(false);
  const [latestContest, setLatestContest] = useState<LatestContestResponse | null>(null);
  const [filterStatistics, setFilterStatistics] = useState<FilterStatisticsResponse | null>(null);
  // Preco da aposta simples vem da modalidade (fonte de verdade); 6.0 e o fallback da Mega-Sena.
  const [betPrice, setBetPrice] = useState(6);
  const requestSequence = useRef(0);
  const selectionGenerationReady = useRef(false);

  // Carrega o concurso mais recente e as distribuicoes das estatisticas calculadas no banco.
  useEffect(() => {
    let active = true;
    const client = new ApiClient({ baseUrl: state.apiBaseUrl });
    client
      .getJson<LatestContestResponse>("/concursos/mega_sena/ultimo")
      .then((contest) => {
        if (active) {
          setLatestContest(contest);
        }
      })
      .catch(() => undefined);
    client
      .getJson<FilterStatisticsResponse>("/estatisticas/mega_sena/filtros")
      .then((statistics) => {
        if (active) {
          setFilterStatistics(statistics);
        }
      })
      .catch(() => undefined);
    client
      .getJson<LotteryModeResponse[]>("/modalidades")
      .then((modes) => {
        if (!active) {
          return;
        }
        const megaSena = modes.find((mode) => mode.codigo === "mega_sena");
        if (megaSena?.valorApostaSimples != null) {
          setBetPrice(megaSena.valorApostaSimples);
        }
      })
      .catch(() => undefined);
    return () => {
      active = false;
    };
  }, [state.apiBaseUrl]);

  const liveStats = useMemo(
    () => buildLiveFilterStats(filterStatistics?.categorias ?? null),
    [filterStatistics]
  );
  const repetitionAverage = useMemo(
    () => computeRepetitionAverage(filterStatistics?.categorias ?? null),
    [filterStatistics]
  );
  const contestLabel = latestContest ? formatInt(latestContest.numeroConcurso) : "—";
  const calibrationLabel = filterStatistics?.totalConcursos
    ? formatInt(filterStatistics.totalConcursos)
    : latestContest
      ? formatInt(latestContest.totalConcursos)
      : "milhares de";

  const previousDraw = useMemo(
    () => (latestContest ? latestContest.dezenas.map(Number) : PREVIOUS_DRAW),
    [latestContest]
  );

  const included = useMemo(
    () =>
      Object.keys(selection)
        .filter((key) => selection[Number(key)] === "include")
        .map(Number)
        .sort((a, b) => a - b),
    [selection]
  );
  const excluded = useMemo(
    () =>
      Object.keys(selection)
        .filter((key) => selection[Number(key)] === "exclude")
        .map(Number)
        .sort((a, b) => a - b),
    [selection]
  );

  // Marca o ultimo resultado como desatualizado quando filtros ou dezenas mudam.
  useEffect(() => {
    setStale(true);
  }, [selection, choices, activeFilters, count]);

  // Persiste localmente cada mudanca de filtros, quantidade ou dezenas marcadas.
  useEffect(() => {
    saveMegaSenaPreferences({ choices, activeFilters, count, selection });
  }, [choices, activeFilters, count, selection]);

  // Toda alteração no volante dispara uma nova geração, exceto na montagem inicial.
  useEffect(() => {
    if (!selectionGenerationReady.current) {
      selectionGenerationReady.current = true;
      return;
    }
    void generate();
  }, [selection]);

  // Envia os filtros da tela para a API gerar os jogos no backend.
  async function generate() {
    const sequence = ++requestSequence.current;
    setGenerating(true);
    setError(null);
    try {
      const client = new ApiClient({ baseUrl: state.apiBaseUrl, token: state.auth?.accessToken });
      const request: GenerateGamesRequest = {
        quantidadeJogos: count,
        dezenasPorJogo: GAME_SIZE,
        dezenasObrigatorias: included.map(pad2),
        dezenasExcluidas: excluded.map(pad2),
        dezenasAnteriores: previousDraw.map(pad2),
        apenasIneditos: true,
        ...buildFilterPayload(activeFilters, choices)
      };
      const response = await client.postJson<GenerateGamesRequest, GenerateMegaSenaGamesResponse>(
        "/gerador/mega-sena/gerar",
        request
      );
      if (sequence === requestSequence.current) {
        setResult(response);
        setStale(false);
      }
    } catch (error) {
      if (sequence === requestSequence.current) {
        setError(describeGenerationError(error));
      }
    } finally {
      if (sequence === requestSequence.current) {
        setGenerating(false);
      }
    }
  }

  // Alterna o estado de uma dezena no ciclo livre -> incluir -> excluir.
  function cycleNumber(value: number) {
    setSelection((current) => {
      const currentState = current[value];
      const next: SelectionState = { ...current };
      if (!currentState) {
        next[value] = "include";
      } else if (currentState === "include") {
        next[value] = "exclude";
      } else {
        delete next[value];
      }
      return next;
    });
  }

  function clearSelection() {
    setSelection({});
  }

  // Fixa as 3 primeiras dezenas do concurso anterior como ponto de partida da estratégia.
  function suggestFixed() {
    const next: SelectionState = {};
    previousDraw.slice(0, 3).forEach((value) => {
      next[value] = "include";
    });
    setSelection(next);
  }

  // Liga ou desliga um filtro, restaurando ou limpando a opcao escolhida.
  function toggleFilter(key: FilterKey, event: MouseEvent<HTMLButtonElement>) {
    event.stopPropagation();
    setOpenFilter(key);
    setActiveFilters((current) => {
      const enabled = !current[key];
      setChoices((currentChoices) => ({
        ...currentChoices,
        [key]: enabled ? (currentChoices[key] ?? DEFAULT_CHOICES[key]) : null
      }));
      return { ...current, [key]: enabled };
    });
  }

  // Seleciona ou desmarca uma opcao do filtro aberto.
  function selectOption(key: FilterKey, index: number) {
    setChoices((current) => {
      const next = current[key] === index ? null : index;
      setActiveFilters((currentFilters) => ({ ...currentFilters, [key]: next !== null }));
      return { ...current, [key]: next };
    });
  }

  function resetChoice(key: FilterKey) {
    setChoices((current) => ({ ...current, [key]: DEFAULT_CHOICES[key] }));
    setActiveFilters((current) => ({ ...current, [key]: DEFAULT_CHOICES[key] !== null }));
  }

  function changeCount(event: ChangeEvent<HTMLInputElement>) {
    setCount(Math.min(30, Math.max(1, parseInt(event.target.value, 10) || 1)));
  }

  // Copia o script jogos.js para a area de transferencia com feedback temporario.
  async function copyScript(script: string) {
    try {
      await navigator.clipboard.writeText(script);
      setScriptCopied(true);
      setTimeout(() => setScriptCopied(false), 2000);
    } catch {
      setError("Não foi possível copiar automaticamente. Selecione o código e copie manualmente.");
    }
  }

  const games = result?.jogos ?? [];
  const missing = count - games.length;
  const tooManyIncluded = included.length > GAME_SIZE;
  const poolTooSmall = BOARD_SIZE - excluded.length < GAME_SIZE;
  const includedSet = useMemo(() => new Set(included.map(pad2)), [included]);
  const previousSet = useMemo(() => new Set(previousDraw.map(pad2)), [previousDraw]);

  let alertText = `Combinação livre: ${BOARD_SIZE - excluded.length - included.length} dezenas disponíveis para completar cada cartão.`;
  let alertTone: AlertTone = "neutral";
  if (tooManyIncluded || poolTooSmall) {
    alertText = tooManyIncluded
      ? `Mais de ${GAME_SIZE} dezenas incluídas — remova ${included.length - GAME_SIZE}.`
      : `Excluídas demais: sobram menos de ${GAME_SIZE} dezenas no volante.`;
    alertTone = "danger";
  } else if (result && !stale && missing > 0) {
    alertText = `Só ${games.length} de ${count} jogos passaram nos filtros. Afrouxe um filtro ou reduza as fixas.`;
    alertTone = "warn";
  } else if (included.length >= 1) {
    alertText = `${included.length} ${included.length === 1 ? "dezena fixa" : "dezenas fixas"} em todos os cartões. As demais rodam pelos filtros.`;
    alertTone = "ok";
  }

  const summaryText = generating
    ? "Gerando jogos no servidor…"
    : error
      ? error
      : result
        ? `${games.length} jogos válidos · ${formatInt(result.combinacoesTestadas)} combinações testadas${stale ? " · filtros alterados, regere" : ""}`
        : "clique em Gerar jogos para consultar o servidor";

  const stats = liveStats[openFilter];
  const optionValues = OPTION_VALUES[openFilter];
  const currentChoice = choices[openFilter];
  const selectedItem = currentChoice === null ? null : stats.items[currentChoice];
  const coverage = selectedItem ? selectedItem.percent : 0;
  const maxPercent = Math.max(...stats.items.map((item) => item.percent));
  const sealOk = coverage >= 20;
  const note = selectedItem
    ? `Todos os jogos desta geração usarão: ${selectedItem.label}.`
    : "Nenhuma opção escolhida: este filtro não restringe a geração.";
  const seal = selectedItem ? `cobre ${formatPercent(coverage)} da base` : "sem restrição";

  return (
    <section className="page gen-page">
      <Link to="/dashboard/mega-sena" className="gen-back-link">
        <ArrowLeft size={16} /> Voltar para o painel
      </Link>

      <AdSpace slot={ADSENSE_SLOTS.leaderboard} format="728x90" className="gen-ad--leaderboard" />

      <section className="gen-filters" aria-label="Filtros matemáticos">
        <div className="gen-filters-head">
          <div>
            <h2>Filtros matemáticos · Mega-Sena</h2>
            <p>Cada filtro é calibrado sobre {calibrationLabel} sorteios reais. Selecione um para ver a estatística histórica.</p>
          </div>
        </div>

        <div className="gen-filters-body">
          <div className="gen-filter-list">
            {FILTER_ORDER.map((key) => {
              const enabled = activeFilters[key];
              const choiceIndex = choices[key];
              const chosen = choiceIndex === null ? null : liveStats[key].items[choiceIndex];
              const detail = chosen
                ? `${chosen.label} · ${chosen.percent.toFixed(0)}% da base`
                : "nenhuma opção — filtro sem restrição";
              return (
                <div
                  key={key}
                  className={`gen-filter-row${openFilter === key ? " gen-filter-row--open" : ""}`}
                  onClick={() => setOpenFilter(key)}
                >
                  <div className="gen-filter-info">
                    <span className="gen-filter-name">{FILTER_LABELS[key]}</span>
                    <span className="gen-filter-detail">{detail}</span>
                  </div>
                  <button
                    type="button"
                    className={`gen-switch${enabled ? " gen-switch--on" : ""}`}
                    aria-pressed={enabled}
                    aria-label={`Ativar ou desativar o filtro ${FILTER_LABELS[key]}`}
                    onClick={(event) => toggleFilter(key, event)}
                  >
                    <span></span>
                  </button>
                </div>
              );
            })}
          </div>

          <div className="gen-stats">
            <div className="gen-stats-head">
              <h3>{stats.title}</h3>
              <span className="gen-stats-base">{stats.base}</span>
            </div>
            <p className="gen-stats-sub">{stats.subtitle}</p>

            <div className="gen-stats-instruction">
              <span>Escolha uma opção — vale para todos os jogos desta geração</span>
              <button type="button" className="gen-btn-reset" onClick={() => resetChoice(openFilter)}>
                Padrão
              </button>
            </div>

            <div className="gen-option-list">
              {stats.items.map((item, index) => {
                const canSelect = optionValues[index] !== null;
                const isSelected = currentChoice === index;
                return (
                  <button
                    type="button"
                    key={item.label}
                    className={`gen-option${isSelected ? " gen-option--selected" : ""}`}
                    disabled={!canSelect}
                    onClick={() => selectOption(openFilter, index)}
                  >
                    <span className="gen-option-check" style={canSelect ? undefined : { visibility: "hidden" }}>
                      {isSelected ? "✓" : ""}
                    </span>
                    <span className="gen-option-label">{item.label}</span>
                    <span className="gen-option-track">
                      <span
                        className="gen-option-bar"
                        style={{ width: `${Math.round((item.percent / maxPercent) * 100)}%` }}
                      ></span>
                    </span>
                    <span className="gen-option-pct">{formatPercent(item.percent)}</span>
                    <span className="gen-option-qty">{formatInt(stats.counts[index])} sort.</span>
                  </button>
                );
              })}
            </div>

            <div className="gen-stats-foot">
              <span className="gen-stats-note">{note}</span>
              <span className={`gen-seal gen-seal--${sealOk ? "ok" : "warn"}`}>{seal}</span>
            </div>
          </div>
        </div>

        <div className="gen-weights">
          <div className="gen-weights-head">
            <h3>Distribuição por peso da estratégia</h3>
            <span>como os jogos se dividem quando um filtro não é travado em uma única opção</span>
          </div>
          <div className="gen-weights-grid">
            {STRATEGY_WEIGHTS.map((weight) => (
              <div className="gen-weight" key={weight.name}>
                <span className="gen-weight-name">{weight.name}</span>
                <div className="gen-weight-track">
                  {weight.bands.map((band) => (
                    <div key={band.label} style={{ width: `${band.percent}%` }}></div>
                  ))}
                </div>
                <div className="gen-weight-labels">
                  {weight.bands.map((band) => (
                    <span key={band.label}>
                      {band.label} · {band.percent}%
                    </span>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <div className="gen-row">
        <section className="gen-numbers-card" aria-label="Dezenas incluídas ou excluídas">
          <div className="gen-card-head">
            <h2>Dezenas incluídas ou excluídas</h2>
            <span className="gen-card-hint">clique: livre → incluir → excluir</span>
            <button type="button" className="gen-btn-reset" onClick={clearSelection}>
              Limpar
            </button>
          </div>

          <div className="gen-numbers-body">
            <div className="gen-board gen-board--mega">
              {Array.from({ length: BOARD_SIZE }, (_, index) => index + 1).map((value) => {
                const cellState = selection[value];
                const modifier =
                  cellState === "include" ? " gen-cell--include" : cellState === "exclude" ? " gen-cell--exclude" : "";
                return (
                  <button type="button" key={value} className={`gen-cell${modifier}`} onClick={() => cycleNumber(value)}>
                    {pad2(value)}
                  </button>
                );
              })}
            </div>

            <div className="gen-tags">
              <span className="gen-tag">
                Incluídas <strong className="gen-tag-include">{included.length}</strong>
              </span>
              <span className="gen-tag">
                Excluídas <strong className="gen-tag-exclude">{excluded.length}</strong>
              </span>
              <span className={`gen-alert gen-alert--${alertTone}`}>{alertText}</span>
            </div>
          </div>
        </section>

        <section className="gen-previous-card" aria-label="Concurso anterior">
          <div className="gen-card-head">
            <h2>Concurso anterior · {contestLabel}</h2>
            <span className="gen-card-hint">
              repetição média: {repetitionAverage !== null ? repetitionAverage.toFixed(1).replace(".", ",") : "0,7"}
            </span>
          </div>
          <div className="gen-previous-grid">
            {previousDraw.map((value) => (
              <span key={value}>{pad2(value)}</span>
            ))}
          </div>
          <button type="button" className="gen-btn-suggest" onClick={suggestFixed}>
            Sugerir 3 fixas do anterior
          </button>
        </section>
      </div>

      <section className="gen-games" aria-label="Jogos gerados">
        <div className="gen-games-head">
          <h2>Jogos gerados</h2>
          <span className="gen-summary-pill">{summaryText}</span>
          <div className="gen-games-controls">
            <label>
              Quantidade
              <input type="number" min={1} max={30} value={count} onChange={changeCount} />
            </label>
            <button type="button" className="gen-btn-regen" onClick={generate} disabled={generating}>
              {generating && <span className="gen-spinner gen-spinner--dark" aria-hidden="true"></span>}
              {generating ? "Gerando…" : "Gerar jogos"}
            </button>
          </div>
        </div>

        <div className="gen-legend">
          <div>
            <i className="gen-legend-fixed"></i> dezena fixa
          </div>
          <div>
            <i className="gen-legend-repeated"></i> repetida do {contestLabel}
          </div>
          <div>
            <i className="gen-legend-fresh"></i> nova
          </div>
        </div>

        <div className="gen-games-list">
          {games.map((game, index) => {
            const sequenceOk = game.maiorSequencia <= 2;
            return (
              <div className="gen-game" key={game.dezenas.join("-")}>
                <div className="gen-game-head">
                  <span className="gen-game-title">Jogo {pad2(index + 1)}</span>
                  <span className="gen-game-meta">
                    soma {game.somaDezenas} · {game.quantidadePares} pares · {game.quantidadeRepetidas} repetidas ·{" "}
                    {game.quantidadePrimos} primos
                  </span>
                  <div className="gen-game-badges">
                    <span className={`gen-badge gen-badge--${sequenceOk ? "ok" : "warn"}`}>
                      máx. {game.maiorSequencia} seguidas
                    </span>
                    <span className="gen-badge gen-badge--ok">inédito na base</span>
                  </div>
                </div>
                <div className="gen-game-numbers">
                  {game.dezenas.map((value) => {
                    const modifier = includedSet.has(value)
                      ? " gen-chip--fixed"
                      : previousSet.has(value)
                        ? " gen-chip--repeated"
                        : "";
                    return (
                      <span className={`gen-chip${modifier}`} key={value}>
                        {value}
                      </span>
                    );
                  })}
                </div>
              </div>
            );
          })}

          {result && games.length === 0 && !generating && (
            <div className="gen-empty">
              <strong>Nenhum jogo passou nos filtros</strong>
              <span>Afrouxe uma opção (ex.: soma, primos ou sequência) ou reduza as dezenas fixas.</span>
            </div>
          )}

          {!result && !generating && (
            <div className="gen-empty">
              <strong className="gen-empty-neutral">Nenhum jogo gerado ainda</strong>
              <span>Ajuste os filtros e clique em Gerar jogos — a geração roda no servidor com o seu plano.</span>
            </div>
          )}
        </div>

        <div className="gen-games-foot">
          <span className="gen-cost">
            {`Custo estimado: R$ ${formatCurrency(games.length * betPrice)}`}{" "}
            · nenhum jogo repete sorteio já registrado na base.
          </span>
          <div className="gen-games-foot-actions">
            <button
              type="button"
              className="gen-btn-secondary"
              disabled={games.length === 0}
              onClick={() => downloadTextFile("jogos.csv", buildGamesCsv(games), "text/csv;charset=utf-8")}
            >
              Exportar CSV
            </button>
            <button
              type="button"
              className="gen-btn-primary"
              disabled={games.length === 0}
              onClick={() => setScriptVisible((visible) => !visible)}
            >
              Salvar cartela
            </button>
          </div>
        </div>

        {scriptVisible && games.length > 0 && (
          <div className="gen-script-box">
            <div className="gen-script-head">
              <span>
                Abra o volante da Mega-Sena no site da Caixa, pressione F12 → Console e cole este código para preencher
                os jogos automaticamente.
              </span>
              <button type="button" className="gen-btn-regen" onClick={() => copyScript(buildGamesScript(games))}>
                {scriptCopied ? "Copiado!" : "Copiar código"}
              </button>
            </div>
            <pre className="gen-script-output" aria-label="Código jogos.js">
              {buildGamesScript(games)}
            </pre>
          </div>
        )}
      </section>

      <AdSpace slot={ADSENSE_SLOTS.responsive} format="auto" className="gen-ad--responsive" />

      <aside className="gen-ad-rail" aria-label="Publicidade e assinatura">
        <AdSpace slot={ADSENSE_SLOTS.rectangle} format="300x250" className="gen-ad--rectangle" />
        <AdSpace slot={ADSENSE_SLOTS.halfPage} format="300x600" className="gen-ad--half-page" />
        <AdSpace slot={ADSENSE_SLOTS.sidebarExtra} format="300x250" className="gen-ad--rectangle" />
      </aside>
    </section>
  );
}

// Dispara o download de um arquivo de texto gerado no navegador.
function downloadTextFile(filename: string, content: string, mimeType: string) {
  const blob = new Blob([content], { type: mimeType });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(url);
}

// Converte falhas HTTP da API em mensagens amigaveis para a tela.
function describeGenerationError(error: unknown): string {
  const message = error instanceof Error ? error.message : "Erro desconhecido";
  if (message.includes("401")) {
    return "Entre com Keycloak para gerar jogos.";
  }
  if (message.includes("403")) {
    return "Quantidade acima do limite do plano atual.";
  }
  if (error instanceof TypeError) {
    return "Não foi possível conectar à API. Verifique a URL configurada.";
  }
  return message;
}
