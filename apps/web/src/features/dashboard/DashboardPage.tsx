import { useEffect, useMemo, useState } from "react";
import { Link } from "@tanstack/react-router";
import { ApiClient, DashboardResponse } from "../../lib/apiClient";
import { useAppState } from "../../lib/appState";
import "./dashboard.css";

const MODE_CODE = "lotofacil";
const GAME_SIZE = 15;
const BOARD_SIZE = 25;
const EXPECTED_FREQUENCY = (GAME_SIZE / BOARD_SIZE) * 100;

const ADSENSE_CLIENT = import.meta.env.VITE_ADSENSE_CLIENT ?? "";
const ADSENSE_SLOTS = {
  leaderboard: import.meta.env.VITE_ADSENSE_SLOT_LEADERBOARD ?? "",
  responsive: import.meta.env.VITE_ADSENSE_SLOT_RESPONSIVE ?? "",
  rectangle: import.meta.env.VITE_ADSENSE_SLOT_RECTANGLE ?? "",
  sidebarExtra: import.meta.env.VITE_ADSENSE_SLOT_SIDEBAR_EXTRA ?? "",
  halfPage: import.meta.env.VITE_ADSENSE_SLOT_HALF_PAGE ?? ""
};

const pad2 = (value: number) => String(value).padStart(2, "0");
const formatInt = (value: number) => value.toLocaleString("pt-BR");
const formatDecimal = (value: number) => value.toLocaleString("pt-BR", { minimumFractionDigits: 1, maximumFractionDigits: 1 });
const formatPercent = (value: number) => `${formatDecimal(value)}%`;
const formatDate = (value?: string | null) => (value ? new Intl.DateTimeFormat("pt-BR").format(new Date(`${value}T00:00:00`)) : "—");

type CategoryItem = { valor: number; quantidade: number };
type FilterRow = { label: string; count: number; percentage: number };
type FilterBlock = { title: string; subtitle: string; rows: FilterRow[] };

// Espaco de anuncio AdSense estavel: mantem a caixa mesmo quando o provedor nao carrega.
function AdSpace({ slot, format, className }: { slot: string; format: "728x90" | "300x250" | "300x600" | "auto"; className: string }) {
  useEffect(() => {
    if (!ADSENSE_CLIENT || !slot) {
      return;
    }
    try {
      const adsWindow = window as Window & { adsbygoogle?: unknown[] };
      (adsWindow.adsbygoogle = adsWindow.adsbygoogle || []).push({});
    } catch {
      // O espaco reservado permanece estavel quando o provedor bloqueia o anuncio.
    }
  }, [slot]);

  return (
    <div className={`dash-ad ${className}`}>
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

// Painel estatistico da Lotofacil: consome o resumo consolidado do backend e monta os cartoes.
export function DashboardPage() {
  const { state } = useAppState();
  const [data, setData] = useState<DashboardResponse | null>(null);
  const [status, setStatus] = useState<"carregando" | "pronto" | "erro">("carregando");

  useEffect(() => {
    let active = true;
    setStatus("carregando");
    const client = new ApiClient({ baseUrl: state.apiBaseUrl, token: state.auth?.accessToken });
    client
      .getJson<DashboardResponse>(`/estatisticas/${MODE_CODE}/painel`)
      .then((result) => {
        if (active) {
          setData(result);
          setStatus("pronto");
        }
      })
      .catch(() => {
        if (active) {
          setStatus("erro");
        }
      });
    return () => {
      active = false;
    };
  }, [state.apiBaseUrl, state.auth?.accessToken]);

  const total = data?.totalConcursos ?? 0;
  const latest = data?.ultimoConcurso ?? null;
  const frequencies = useMemo(() => [...(data?.frequencias ?? [])].sort((a, b) => a.dezena - b.dezena), [data]);
  const filterBlocks = useMemo(() => (data ? buildFilterBlocks(data.categorias, total, data.resumo.repeticaoMedia) : []), [data, total]);

  const byFrequency = useMemo(() => [...frequencies].sort((a, b) => b.quantidade - a.quantidade), [frequencies]);
  const topDrawn = byFrequency.slice(0, 5);
  const bottomDrawn = [...byFrequency].reverse().slice(0, 5);
  const byDelay = useMemo(() => [...frequencies].sort((a, b) => b.atraso - a.atraso).slice(0, 5), [frequencies]);

  return (
    <div className="dash-root">
      <div className="dash-main">
        <header className="dash-header">
          <div>
            <h1>Painel estatístico Lotofácil</h1>
            <p>
              Base completa: {formatInt(total)} sorteios
              {latest ? ` · último concurso ${formatInt(latest.numero)} · ${formatDate(latest.dataApuracao)}` : ""}
            </p>
          </div>
          <Link to="/gerar-jogos/lotofacil" className="dash-btn-primary">
            Gerar jogos
          </Link>
        </header>

        <div className="dash-body">
          <div className="dash-content">
            <AdSpace slot={ADSENSE_SLOTS.leaderboard} format="728x90" className="dash-ad--leaderboard" />

            {status === "erro" && <p className="dash-alert">Não foi possível carregar o painel a partir da API.</p>}

            <section className="dash-kpis" aria-label="Indicadores">
              <KpiCard label="Sorteios na base" value={formatInt(total)} context="atualizado a partir do banco" />
              <KpiCard
                label="Soma média"
                value={data ? formatDecimal(data.resumo.somaMedia) : "—"}
                context={data ? `faixa 185–210 em ${formatPercent(data.resumo.faixaSomaPreferencialPercentual)} dos sorteios` : ""}
              />
              <KpiCard
                label="Repetição média"
                value={data ? formatDecimal(data.resumo.repeticaoMedia) : "—"}
                context="do concurso imediatamente anterior"
              />
              <KpiCard
                label="Combinações inéditas"
                value={data ? formatPercent(data.resumo.combinacoesIneditasPercentual) : "—"}
                context="proporção de sorteios sem repetição na história"
              />
            </section>

            <section className="dash-card">
              <div className="dash-card-head">
                <div>
                  <h2>Números que mais saem</h2>
                  <p>
                    frequência em {formatInt(total)} sorteios · esperado {formatDecimal(EXPECTED_FREQUENCY)}%
                  </p>
                </div>
                <div className="dash-legend">
                  <span>
                    <i className="dash-swatch dash-swatch--up" /> acima da média
                  </span>
                  <span>
                    <i className="dash-swatch dash-swatch--down" /> abaixo da média
                  </span>
                </div>
              </div>

              <div className="dash-bars" role="img" aria-label="Frequência das 25 dezenas">
                {frequencies.map((item) => {
                  const above = item.percentual >= EXPECTED_FREQUENCY;
                  const height = Math.max(4, Math.min(100, (item.percentual / 80) * 100));
                  return (
                    <div className="dash-bar-col" key={item.dezena}>
                      <span className="dash-bar-value">{formatDecimal(item.percentual)}%</span>
                      <div className="dash-bar-track">
                        <div
                          className={`dash-bar ${above ? "dash-bar--up" : "dash-bar--down"}`}
                          style={{ height: `${height}%` }}
                          title={`Dezena ${pad2(item.dezena)}: ${formatInt(item.quantidade)} sorteios (${formatDecimal(item.percentual)}%)`}
                        />
                      </div>
                      <span className="dash-bar-label">{pad2(item.dezena)}</span>
                    </div>
                  );
                })}
              </div>

              <div className="dash-card-foot">
                <div className="dash-foot-block">
                  <p className="dash-foot-title">Mais sorteadas</p>
                  <div className="dash-balls">
                    {topDrawn.map((item) => (
                      <span
                        key={item.dezena}
                        className="dash-ball dash-ball--filled"
                        title={`${formatInt(item.quantidade)} sorteios (${formatDecimal(item.percentual)}%)`}
                      >
                        {pad2(item.dezena)}
                      </span>
                    ))}
                  </div>
                </div>
                <div className="dash-foot-block">
                  <p className="dash-foot-title">Menos sorteadas</p>
                  <div className="dash-balls">
                    {bottomDrawn.map((item) => (
                      <span
                        key={item.dezena}
                        className="dash-ball dash-ball--hollow"
                        title={`${formatInt(item.quantidade)} sorteios (${formatDecimal(item.percentual)}%)`}
                      >
                        {pad2(item.dezena)}
                      </span>
                    ))}
                  </div>
                </div>
                <div className="dash-foot-block">
                  <p className="dash-foot-title">
                    Maior atraso
                    <span
                      className="dash-help"
                      title="Dezenas que estão há mais concursos sem ser sorteadas. O número dentro do chip é a dezena; ao lado, quantos concursos seguidos ela não sai."
                    >
                      ?
                    </span>
                  </p>
                  <div className="dash-chips">
                    {byDelay.map((item) => (
                      <span
                        key={item.dezena}
                        className="dash-chip"
                        title={`Dezena ${pad2(item.dezena)}: não é sorteada há ${item.atraso} concursos seguidos${
                          item.ultimoConcurso ? ` (último sorteio no concurso ${formatInt(item.ultimoConcurso)})` : ""
                        }.`}
                      >
                        <strong>{pad2(item.dezena)}</strong> {item.atraso} conc.
                      </span>
                    ))}
                  </div>
                </div>
              </div>
            </section>

            <section className="dash-card dash-filter-card">
              <div className="dash-filter-head">
                <div>
                  <h2>Quantos jogos saíram por filtro</h2>
                  <p>Contagem histórica de cada opção dos filtros matemáticos, sobre {formatInt(total)} sorteios</p>
                </div>
                <span className="dash-badge">Diferencial</span>
              </div>
              <div className="dash-filter-grid">
                {filterBlocks.map((block) => {
                  const maxCount = block.rows.reduce((max, row) => Math.max(max, row.count), 0);
                  const common = block.rows.find((row) => row.count === maxCount);
                  return (
                    <div className="dash-filter-block" key={block.title}>
                      <div className="dash-filter-block-head">
                        <div>
                          <p className="dash-filter-block-title">{block.title}</p>
                          <p className="dash-filter-block-sub">{block.subtitle}</p>
                        </div>
                        {common && <span className="dash-filter-common">mais comum: {common.label}</span>}
                      </div>
                      <div className="dash-filter-rows">
                        {block.rows.map((row) => (
                          <div className="dash-filter-row" key={row.label}>
                            <span className="dash-filter-label">{row.label}</span>
                            <span className="dash-filter-bar-track">
                              <span
                                className={`dash-filter-bar ${row.count === maxCount ? "dash-filter-bar--primary" : ""}`}
                                style={{ width: `${maxCount > 0 ? (row.count / maxCount) * 100 : 0}%` }}
                              />
                            </span>
                            <span className="dash-filter-count">{formatInt(row.count)}</span>
                            <span className="dash-filter-percent">{formatPercent(row.percentage)}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  );
                })}
              </div>
            </section>

            <AdSpace slot={ADSENSE_SLOTS.responsive} format="auto" className="dash-ad--responsive" />
          </div>

          <aside className="dash-rail" aria-label="Concurso e anúncios">
            <section className="dash-card dash-latest">
              <h2>Último concurso · {latest ? formatInt(latest.numero) : "—"}</h2>
              <p className="dash-latest-date">{formatDate(latest?.dataApuracao)}</p>
              <div className="dash-latest-numbers">
                {(latest?.dezenas ?? []).map((dezena) => (
                  <span className="dash-ball dash-ball--filled" key={dezena}>
                    {dezena}
                  </span>
                ))}
              </div>
              {latest && (
                <div className="dash-latest-chips">
                  <span className="dash-badge-ok">
                    {latest.pares} pares / {latest.impares} ímpares
                  </span>
                  <span className="dash-badge-ok">soma {latest.soma}</span>
                  <span className="dash-badge-ok">{latest.primos} primos</span>
                  <span className="dash-badge-ok">{latest.moldura} na moldura</span>
                </div>
              )}
            </section>
            <AdSpace slot={ADSENSE_SLOTS.rectangle} format="300x250" className="dash-ad--rectangle" />
            <AdSpace slot={ADSENSE_SLOTS.sidebarExtra} format="300x250" className="dash-ad--rectangle" />
            <AdSpace slot={ADSENSE_SLOTS.halfPage} format="300x600" className="dash-ad--half-page" />
          </aside>
        </div>
      </div>
    </div>
  );
}

function KpiCard({ label, value, context }: { label: string; value: string; context: string }) {
  return (
    <div className="dash-kpi">
      <span className="dash-kpi-label">{label}</span>
      <strong className="dash-kpi-value">{value}</strong>
      <span className="dash-kpi-context">{context}</span>
    </div>
  );
}

// Monta os oito blocos do cartao de filtros a partir das categorias cruas do backend.
function buildFilterBlocks(
  categories: Record<string, CategoryItem[]>,
  total: number,
  repetitionAverage: number
): FilterBlock[] {
  const percent = (count: number) => (total > 0 ? (count / total) * 100 : 0);
  const parity = categories.paridade ?? [];
  const repetition = categories.repeticao ?? [];
  const primes = categories.primos ?? [];
  const border = categories.moldura ?? [];
  const sum = categories.soma ?? [];
  const grid = categories.grade ?? [];
  const sequence = categories.sequencia ?? [];

  const sumInRange = (items: CategoryItem[], predicate: (value: number) => boolean) =>
    items.filter((item) => predicate(item.valor)).reduce((acc, item) => acc + item.quantidade, 0);

  const toRows = (items: { label: string; count: number }[]) =>
    items
      .filter((item) => item.count > 0)
      .map((item) => ({ label: item.label, count: item.count, percentage: percent(item.count) }));

  const parityRows = toRows(
    [...parity]
      .sort((a, b) => b.quantidade - a.quantidade)
      .map((item) => ({ label: `${item.valor}p / ${GAME_SIZE - item.valor}í`, count: item.quantidade }))
  );

  const repetitionRows = toRows(
    [...repetition]
      .sort((a, b) => b.quantidade - a.quantidade)
      .map((item) => ({ label: `${item.valor} rep.`, count: item.quantidade }))
  );

  const primeRows = toRows(
    [...primes]
      .sort((a, b) => b.quantidade - a.quantidade)
      .map((item) => ({ label: `${item.valor} primos`, count: item.quantidade }))
  );

  const borderRows = toRows(
    [...border]
      .sort((a, b) => b.quantidade - a.quantidade)
      .map((item) => ({ label: `${item.valor} na moldura`, count: item.quantidade }))
  );

  const sumBands: { label: string; predicate: (value: number) => boolean }[] = [
    { label: "≤ 164", predicate: (value) => value <= 164 },
    { label: "165–174", predicate: (value) => value >= 165 && value <= 174 },
    { label: "175–184", predicate: (value) => value >= 175 && value <= 184 },
    { label: "185–194", predicate: (value) => value >= 185 && value <= 194 },
    { label: "195–204", predicate: (value) => value >= 195 && value <= 204 },
    { label: "205–214", predicate: (value) => value >= 205 && value <= 214 },
    { label: "215–224", predicate: (value) => value >= 215 && value <= 224 },
    { label: "≥ 225", predicate: (value) => value >= 225 }
  ];
  const sumRows = toRows(sumBands.map((band) => ({ label: band.label, count: sumInRange(sum, band.predicate) })));

  const strategyBands: { label: string; predicate: (value: number) => boolean }[] = [
    { label: "185–210", predicate: (value) => value >= 185 && value <= 210 },
    { label: "180–184 / 211–212", predicate: (value) => (value >= 180 && value <= 184) || (value >= 211 && value <= 212) },
    { label: "175–179 / 213–215", predicate: (value) => (value >= 175 && value <= 179) || (value >= 213 && value <= 215) },
    { label: "170–174 / 216–220", predicate: (value) => (value >= 170 && value <= 174) || (value >= 216 && value <= 220) },
    { label: "fora de 170–220", predicate: (value) => value < 170 || value > 220 }
  ];
  const strategyRows = toRows(strategyBands.map((band) => ({ label: band.label, count: sumInRange(sum, band.predicate) })));

  const gridLabels: Record<number, string> = {
    0: "2 a 4 em todas (forte)",
    1: "alguma com 1",
    2: "alguma com 5",
    3: "alguma zerada"
  };
  const gridRows = toRows(
    [0, 1, 2, 3].map((value) => ({
      label: gridLabels[value],
      count: grid.find((item) => item.valor === value)?.quantidade ?? 0
    }))
  );

  const sequenceBands: { label: string; predicate: (value: number) => boolean }[] = [
    { label: "até 3 seguidas", predicate: (value) => value <= 3 },
    { label: "4 seguidas", predicate: (value) => value === 4 },
    { label: "5 seguidas", predicate: (value) => value === 5 },
    { label: "6 seguidas", predicate: (value) => value === 6 },
    { label: "7 seguidas", predicate: (value) => value === 7 },
    { label: "8+ seguidas", predicate: (value) => value >= 8 }
  ];
  const sequenceRows = toRows(sequenceBands.map((band) => ({ label: band.label, count: sumInRange(sequence, band.predicate) })));

  return [
    { title: "Pares e ímpares", subtitle: "equilíbrio entre pares e ímpares", rows: parityRows },
    { title: "Repetidas do concurso anterior", subtitle: `média ${formatDecimal(repetitionAverage)}`, rows: repetitionRows },
    { title: "Números primos", subtitle: "02 03 05 07 11 13 17 19 23", rows: primeRows },
    { title: "Moldura e miolo", subtitle: "16 bordas · 9 centrais", rows: borderRows },
    { title: "Soma das dezenas", subtitle: "faixas de 10 em 10", rows: sumRows },
    { title: "Soma por faixa da estratégia", subtitle: "bandas que não se sobrepõem", rows: strategyRows },
    { title: "Linhas e colunas", subtitle: "distribuição no volante 5x5", rows: gridRows },
    { title: "Sequências consecutivas", subtitle: "maior sequência do sorteio", rows: sequenceRows }
  ];
}
