// Dados estatisticos e mapeamento de filtros da tela de geracao de apostas da Lotomania.
// A geracao acontece no backend (POST /gerador/lotomania/gerar); este modulo guarda os
// percentuais de referencia exibidos na UI e converte as escolhas do usuario no payload de filtros.
// A Lotomania usa cartela 00-99, aposta FIXA de 50 dezenas, 20 dezenas sorteadas e nao tem os
// conceitos de moldura nem de grade linha/coluna densa da Lotofacil.
//
// Diferente das demais modalidades, a aposta (50 dezenas) tem tamanho muito maior que o sorteio
// (20 dezenas). Por isso os filtros aqui sao calibrados para a APOSTA de 50 dezenas e usam os
// percentuais teoricos de referencia (fallback), e nao as distribuicoes do banco, que descrevem
// os sorteios de 20 dezenas e teriam escala incompativel.
import type { GenerateGamesRequest } from "../../lib/apiClient";

export const GAME_SIZE = 50;
export const BOARD_SIZE = 100;
export const FIRST_NUMBER = 0;
export const LAST_NUMBER = 99;
export const HISTORY_BASE = 2957;
export const PREVIOUS_DRAW = [
  0, 1, 9, 10, 12, 13, 18, 25, 35, 37, 38, 46, 54, 72, 79, 80, 82, 83, 84, 91
];

export type FilterKey = "parity" | "repetition" | "primes" | "sum" | "sequence";

export const FILTER_ORDER: FilterKey[] = ["parity", "repetition", "primes", "sum", "sequence"];

export type FilterStatsItem = {
  label: string;
  percent: number;
  preferred: boolean;
};

export type FilterStats = {
  title: string;
  base: string;
  subtitle: string;
  items: FilterStatsItem[];
};

// Valor aceito por uma opcao: numero exato, faixa [min, max], uniao de faixas ou null (nao selecionavel).
export type OptionMatcher = number | [number, number] | Array<[number, number]> | null;

export const FILTER_LABELS: Record<FilterKey, string> = {
  parity: "Pares e ímpares",
  repetition: "Repetição do sorteio anterior",
  primes: "Números primos",
  sum: "Soma das dezenas",
  sequence: "Sequência máxima"
};

export const FILTER_STATS: Record<FilterKey, FilterStats> = {
  parity: {
    title: "Resumo de pares e ímpares",
    base: "apostas de referência",
    subtitle: "Como as 50 dezenas de uma aposta se dividem entre pares e ímpares (00 a 99 tem 50 de cada).",
    items: [
      { label: "25 pares / 25 ímpares", percent: 15.8, preferred: true },
      { label: "24 pares / 26 ímpares", percent: 14.6, preferred: true },
      { label: "26 pares / 24 ímpares", percent: 14.6, preferred: true },
      { label: "23 pares / 27 ímpares", percent: 12.2, preferred: false },
      { label: "extremos (≤ 22 ou ≥ 28 pares)", percent: 42.8, preferred: false }
    ]
  },
  repetition: {
    title: "Resumo de repetidas do sorteio anterior",
    base: "referência",
    subtitle: "Quantas das 20 dezenas do sorteio anterior aparecem na aposta de 50 dezenas.",
    items: [
      { label: "8 a 12 repetidas", percent: 68.0, preferred: true },
      { label: "9 a 11 repetidas", percent: 38.0, preferred: true },
      { label: "6 a 14 repetidas", percent: 95.0, preferred: false },
      { label: "sem restrição", percent: 100, preferred: false }
    ]
  },
  primes: {
    title: "Resumo de números primos",
    base: "apostas de referência",
    subtitle: "Primos de 00 a 99: 02 03 05 07 11 13 17 19 23 29 31 37 41 43 47 53 59 61 67 71 73 79 83 89 97 (25 no total).",
    items: [
      { label: "11 a 14 primos", percent: 64.0, preferred: true },
      { label: "12 ou 13 primos", percent: 34.0, preferred: true },
      { label: "10 a 15 primos", percent: 82.0, preferred: false },
      { label: "sem restrição", percent: 100, preferred: false }
    ]
  },
  sum: {
    title: "Resumo de somas das dezenas",
    base: "apostas de referência",
    subtitle: "Soma das 50 dezenas da aposta (média em torno de 2.475), agrupada em faixas.",
    items: [
      { label: "muito equilibrada · 2.400–2.550", percent: 38.0, preferred: true },
      { label: "equilibrada · 2.330–2.620", percent: 68.0, preferred: true },
      { label: "ampla · 2.200–2.750", percent: 91.0, preferred: false },
      { label: "sem restrição", percent: 100, preferred: false }
    ]
  },
  sequence: {
    title: "Resumo de sequências consecutivas",
    base: "apostas de referência",
    subtitle: "Maior sequência de dezenas consecutivas dentro da aposta (marcar 50 de 100 gera sequências longas).",
    items: [
      { label: "máx. 6 seguidas", percent: 34.0, preferred: false },
      { label: "máx. 8 seguidas", percent: 72.0, preferred: false },
      { label: "máx. 10 seguidas", percent: 92.0, preferred: false },
      { label: "sem limite", percent: 100, preferred: false }
    ]
  }
};

export const OPTION_VALUES: Record<FilterKey, OptionMatcher[]> = {
  parity: [25, 24, 26, 23, null],
  repetition: [
    [8, 12],
    [9, 11],
    [6, 14],
    null
  ],
  primes: [
    [11, 14],
    [12, 13],
    [10, 15],
    null
  ],
  sum: [
    [2400, 2550],
    [2330, 2620],
    [2200, 2750],
    null
  ],
  sequence: [
    [0, 6],
    [0, 8],
    [0, 10],
    null
  ]
};

export type ChoiceState = Record<FilterKey, number | null>;
export type ActiveFilterState = Record<FilterKey, boolean>;

// Escolha padrao de cada filtro: primeiro item preferido com matcher definido; a soma usa a faixa equilibrada.
function buildDefaultChoices(): ChoiceState {
  const choices = {} as ChoiceState;
  for (const key of FILTER_ORDER) {
    const index = FILTER_STATS[key].items.findIndex(
      (item, itemIndex) => item.preferred && OPTION_VALUES[key][itemIndex] !== null
    );
    choices[key] = index >= 0 ? index : null;
  }
  choices.parity = 0;
  choices.sum = 1;
  return choices;
}

export const DEFAULT_CHOICES: ChoiceState = buildDefaultChoices();

// So paridade e soma iniciam ligados; primos, repeticao e sequencia ficam desligados para
// nao sufocar a geracao de apostas de 50 dezenas logo na abertura da tela.
export const DEFAULT_ACTIVE_FILTERS: ActiveFilterState = {
  parity: true,
  repetition: false,
  primes: false,
  sum: true,
  sequence: false
};

// Escolhas iniciais coerentes com os filtros ativos: filtro desligado comeca sem opcao marcada.
export const INITIAL_CHOICES: ChoiceState = FILTER_ORDER.reduce((choices, key) => {
  choices[key] = DEFAULT_ACTIVE_FILTERS[key] ? DEFAULT_CHOICES[key] : null;
  return choices;
}, {} as ChoiceState);

// Categoria persistida no banco que alimenta cada filtro da tela.
const CATEGORY_BY_FILTER: Record<FilterKey, string> = {
  parity: "paridade",
  repetition: "repeticao",
  primes: "primos",
  sum: "soma",
  sequence: "sequencia"
};

// Verifica se um valor de metrica atende ao matcher informado.
export function matchesOption(value: number, matcher: Exclude<OptionMatcher, null>): boolean {
  if (typeof matcher === "number") {
    return value === matcher;
  }

  if (Array.isArray(matcher[0])) {
    return (matcher as Array<[number, number]>).some(([min, max]) => value >= min && value <= max);
  }

  const [min, max] = matcher as [number, number];
  return value >= min && value <= max;
}

export type FilterDistributionItem = { valor: number; quantidade: number };
export type FilterDistributions = Partial<Record<string, FilterDistributionItem[]>>;
export type LiveFilterStats = Record<FilterKey, FilterStats & { total: number; counts: number[] }>;

const formatTotal = (value: number) => value.toLocaleString("pt-BR");

// Monta as estatisticas exibidas na tela. Para a Lotomania usamos sempre os percentuais de
// referencia (fallback), porque a aposta tem 50 dezenas e as distribuicoes do banco descrevem
// sorteios de 20 dezenas — escalas incompativeis. As distribuicoes ficam disponiveis para
// manter a assinatura da funcao alinhada com as demais modalidades.
export function buildLiveFilterStats(distributions: FilterDistributions | null): LiveFilterStats {
  const result = {} as LiveFilterStats;

  for (const key of FILTER_ORDER) {
    const base = FILTER_STATS[key];
    // Mantemos a leitura da categoria apenas para eventual uso futuro; hoje o fallback prevalece.
    void distributions?.[CATEGORY_BY_FILTER[key]];
    const fallbackTotal = HISTORY_BASE;
    result[key] = {
      ...base,
      total: fallbackTotal,
      counts: base.items.map((item) => Math.round((fallbackTotal * item.percent) / 100))
    };
  }

  return result;
}

// Converte um matcher exato ou de faixa simples em limites minimo e maximo.
function matcherBounds(matcher: Exclude<OptionMatcher, null>): { min: number; max: number } | null {
  if (typeof matcher === "number") {
    return { min: matcher, max: matcher };
  }

  if (Array.isArray(matcher[0])) {
    return null;
  }

  const [min, max] = matcher as [number, number];
  return { min, max };
}

// Retorna o matcher da opcao escolhida quando o filtro esta ativo.
function activeMatcher(key: FilterKey, activeFilters: ActiveFilterState, choices: ChoiceState): OptionMatcher {
  if (!activeFilters[key]) {
    return null;
  }

  const index = choices[key];
  if (index === null || index === undefined) {
    return null;
  }

  return OPTION_VALUES[key][index];
}

export type GenerationFilterPayload = Pick<
  GenerateGamesRequest,
  | "quantidadePares"
  | "somaMinima"
  | "somaMaxima"
  | "repetidasMinima"
  | "repetidasMaxima"
  | "primosMinimo"
  | "primosMaximo"
  | "sequenciaMaxima"
>;

// Converte os filtros ativos e opcoes escolhidas na UI para o payload aceito pela API.
export function buildFilterPayload(activeFilters: ActiveFilterState, choices: ChoiceState): GenerationFilterPayload {
  const payload: GenerationFilterPayload = {};

  const parity = activeMatcher("parity", activeFilters, choices);
  if (typeof parity === "number") {
    payload.quantidadePares = parity;
  }

  const repetition = activeMatcher("repetition", activeFilters, choices);
  const repetitionBounds = repetition === null ? null : matcherBounds(repetition);
  if (repetitionBounds) {
    payload.repetidasMinima = repetitionBounds.min;
    payload.repetidasMaxima = Math.min(repetitionBounds.max, 20);
  }

  const primes = activeMatcher("primes", activeFilters, choices);
  const primesBounds = primes === null ? null : matcherBounds(primes);
  if (primesBounds) {
    payload.primosMinimo = primesBounds.min;
    payload.primosMaximo = Math.min(primesBounds.max, 25);
  }

  const sum = activeMatcher("sum", activeFilters, choices);
  const sumBounds = sum === null ? null : matcherBounds(sum);
  if (sumBounds) {
    payload.somaMinima = sumBounds.min;
    payload.somaMaxima = sumBounds.max;
  }

  const sequence = activeMatcher("sequence", activeFilters, choices);
  const sequenceBounds = sequence === null ? null : matcherBounds(sequence);
  if (sequenceBounds) {
    payload.sequenciaMaxima = Math.max(1, sequenceBounds.max);
  }

  return payload;
}

export type StrategyWeight = {
  name: string;
  bands: Array<{ label: string; percent: number }>;
};

// Distribuicao de pesos exibida quando um filtro nao esta travado em uma unica opcao.
export const STRATEGY_WEIGHTS: StrategyWeight[] = [
  {
    name: "Pares / ímpares",
    bands: [
      { label: "25×25", percent: 32 },
      { label: "24/26", percent: 30 },
      { label: "23/27", percent: 24 },
      { label: "extremos", percent: 14 }
    ]
  },
  {
    name: "Primos",
    bands: [
      { label: "12–13", percent: 34 },
      { label: "11 ou 14", percent: 30 },
      { label: "10 ou 15", percent: 22 },
      { label: "outros", percent: 14 }
    ]
  },
  {
    name: "Soma",
    bands: [
      { label: "2400–2550", percent: 38 },
      { label: "2330–2620", percent: 30 },
      { label: "2200–2750", percent: 23 },
      { label: "extremos", percent: 9 }
    ]
  },
  {
    name: "Repetição",
    bands: [
      { label: "9–11", percent: 38 },
      { label: "8 ou 12", percent: 30 },
      { label: "6–14", percent: 27 },
      { label: "outros", percent: 5 }
    ]
  }
];
