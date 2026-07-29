// Dados estatisticos e mapeamento de filtros da tela de geracao de jogos.
// A geracao em si acontece no backend (POST /gerador/lotofacil/gerar); este modulo
// guarda os dados historicos exibidos na UI (design "Geracao de Jogos") e converte
// as escolhas do usuario no payload de filtros aceito pela API.
import type { GenerateGamesRequest } from "../../lib/apiClient";

export const GAME_SIZE = 15;
export const BOARD_SIZE = 25;
export const HISTORY_BASE = 3411;
export const PREVIOUS_DRAW = [1, 2, 3, 5, 7, 9, 11, 13, 14, 17, 19, 20, 22, 24, 25];

export type FilterKey =
  | "parity"
  | "repetition"
  | "primes"
  | "frame"
  | "sum"
  | "sumRange"
  | "grid"
  | "sequence";

export const FILTER_ORDER: FilterKey[] = [
  "parity",
  "repetition",
  "primes",
  "frame",
  "sum",
  "sumRange",
  "grid",
  "sequence"
];

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
  repetition: "Repetição do anterior",
  primes: "Números primos",
  frame: "Moldura vs. miolo",
  sum: "Soma das dezenas",
  sumRange: "Soma por faixa da estratégia",
  grid: "Linhas e colunas",
  sequence: "Sequência máxima"
};

export const FILTER_STATS: Record<FilterKey, FilterStats> = {
  parity: {
    title: "Resumo de pares e ímpares",
    base: "3.411 sorteios",
    subtitle: "Quantas vezes cada divisão saiu em toda a base histórica.",
    items: [
      { label: "7 pares / 8 ímpares", percent: 29.1, preferred: true },
      { label: "8 pares / 7 ímpares", percent: 26.8, preferred: true },
      { label: "6 pares / 9 ímpares", percent: 17.4, preferred: true },
      { label: "9 pares / 6 ímpares", percent: 14.9, preferred: true },
      { label: "5 pares / 10 ímpares", percent: 5.2, preferred: false },
      { label: "10 pares / 5 ímpares", percent: 4.6, preferred: false },
      { label: "outros extremos", percent: 2.0, preferred: false }
    ]
  },
  repetition: {
    title: "Resumo de repetidos do concurso anterior",
    base: "3.410 pares de concursos",
    subtitle: "Em quantos sorteios N dezenas do concurso imediatamente anterior se repetiram.",
    items: [
      { label: "9 repetidas", percent: 23.9, preferred: true },
      { label: "8 repetidas", percent: 21.2, preferred: true },
      { label: "10 repetidas", percent: 18.7, preferred: true },
      { label: "7 repetidas", percent: 13.1, preferred: false },
      { label: "11 repetidas", percent: 10.8, preferred: false },
      { label: "6 repetidas", percent: 5.4, preferred: false },
      { label: "12+ repetidas", percent: 4.3, preferred: false },
      { label: "≤5 repetidas", percent: 2.6, preferred: false }
    ]
  },
  primes: {
    title: "Resumo de números primos",
    base: "3.411 sorteios",
    subtitle: "Primos do volante: 02 03 05 07 11 13 17 19 23.",
    items: [
      { label: "5 primos", percent: 29.6, preferred: true },
      { label: "6 primos", percent: 25.4, preferred: true },
      { label: "4 primos", percent: 20.1, preferred: true },
      { label: "7 primos", percent: 14.8, preferred: true },
      { label: "3 primos", percent: 6.3, preferred: false },
      { label: "8+ primos", percent: 3.8, preferred: false }
    ]
  },
  frame: {
    title: "Resumo de moldura e miolo",
    base: "3.411 sorteios",
    subtitle: "Moldura: 16 dezenas das bordas. Miolo: 9 dezenas centrais.",
    items: [
      { label: "9 na moldura", percent: 27.8, preferred: true },
      { label: "10 na moldura", percent: 26.1, preferred: true },
      { label: "11 na moldura", percent: 17.9, preferred: true },
      { label: "8 na moldura", percent: 15.6, preferred: true },
      { label: "12 na moldura", percent: 7.4, preferred: false },
      { label: "≤7 na moldura", percent: 5.2, preferred: false }
    ]
  },
  sum: {
    title: "Resumo de somas dos valores sorteados",
    base: "3.411 sorteios",
    subtitle: "Soma das 15 dezenas de cada sorteio, agrupada em intervalos de 10.",
    items: [
      { label: "≤ 164", percent: 4.1, preferred: false },
      { label: "165–174", percent: 7.8, preferred: false },
      { label: "175–184", percent: 15.2, preferred: true },
      { label: "185–194", percent: 22.6, preferred: true },
      { label: "195–204", percent: 22.1, preferred: true },
      { label: "205–214", percent: 15.4, preferred: true },
      { label: "215–224", percent: 8.3, preferred: false },
      { label: "≥ 225", percent: 4.5, preferred: false }
    ]
  },
  sumRange: {
    title: "Resumo de somas por faixa da estratégia",
    base: "3.411 sorteios",
    subtitle: "Faixas ponderadas da estratégia, em bandas que não se sobrepõem.",
    items: [
      { label: "185–210 · peso 50%", percent: 47.9, preferred: true },
      { label: "180–184 e 211–212 · peso 30%", percent: 11.7, preferred: true },
      { label: "175–179 e 213–215 · peso 15%", percent: 11.8, preferred: true },
      { label: "170–174 e 216–220 · peso 5%", percent: 9.4, preferred: true },
      { label: "fora de 170–220", percent: 19.2, preferred: false }
    ]
  },
  grid: {
    title: "Resumo de linhas e colunas",
    base: "3.411 sorteios",
    subtitle: "Distribuição das dezenas pelas 5 linhas e 5 colunas do volante.",
    items: [
      { label: "modo forte · 2 a 4 em todas", percent: 41.3, preferred: true },
      { label: "modo flexível · aceita alguma com 1", percent: 80.0, preferred: false },
      { label: "alguma com 5", percent: 16.2, preferred: false },
      { label: "alguma zerada", percent: 3.8, preferred: false }
    ]
  },
  sequence: {
    title: "Resumo de sequências consecutivas",
    base: "3.411 sorteios",
    subtitle: "Maior sequência de dezenas consecutivas dentro do sorteio.",
    items: [
      { label: "máx. 3 seguidas", percent: 21.7, preferred: false },
      { label: "máx. 4 seguidas", percent: 49.6, preferred: false },
      { label: "máx. 5 seguidas", percent: 74.0, preferred: true },
      { label: "máx. 6 seguidas", percent: 89.8, preferred: false },
      { label: "máx. 7 seguidas", percent: 96.7, preferred: false },
      { label: "sem limite", percent: 100, preferred: false }
    ]
  }
};

export const OPTION_VALUES: Record<FilterKey, OptionMatcher[]> = {
  parity: [7, 8, 6, 9, 5, 10, null],
  repetition: [9, 8, 10, 7, 11, 6, [12, 15], [0, 5]],
  primes: [5, 6, 4, 7, 3, [8, 15]],
  frame: [9, 10, 11, 8, 12, [0, 7]],
  sum: [
    [0, 164],
    [165, 174],
    [175, 184],
    [185, 194],
    [195, 204],
    [205, 214],
    [215, 224],
    [225, 999]
  ],
  sumRange: [
    [185, 210],
    [
      [180, 184],
      [211, 212]
    ],
    [
      [175, 179],
      [213, 215]
    ],
    [
      [170, 174],
      [216, 220]
    ],
    null
  ],
  grid: [0, [0, 1], null, null],
  sequence: [
    [0, 3],
    [0, 4],
    [0, 5],
    [0, 6],
    [0, 7],
    [0, 15]
  ]
};

export type ChoiceState = Record<FilterKey, number | null>;
export type ActiveFilterState = Record<FilterKey, boolean>;

// Calcula a opcao padrao de cada filtro: primeiro item preferido com matcher definido.
function buildDefaultChoices(): ChoiceState {
  const choices = {} as ChoiceState;
  for (const key of FILTER_ORDER) {
    const index = FILTER_STATS[key].items.findIndex(
      (item, itemIndex) => item.preferred && OPTION_VALUES[key][itemIndex] !== null
    );
    choices[key] = index >= 0 ? index : null;
  }
  choices.sum = 3;
  return choices;
}

export const DEFAULT_CHOICES: ChoiceState = buildDefaultChoices();

// Todos os filtros iniciam ligados com a opcao padrao marcada.
export const DEFAULT_ACTIVE_FILTERS: ActiveFilterState = {
  parity: true,
  repetition: true,
  primes: true,
  frame: true,
  sum: true,
  sumRange: true,
  grid: true,
  sequence: true
};

// Escolhas iniciais coerentes com os filtros ativos: filtro desligado comeca sem opcao marcada.
// DEFAULT_CHOICES segue sendo o alvo restaurado ao ligar o interruptor ou clicar em "Padrao".
export const INITIAL_CHOICES: ChoiceState = FILTER_ORDER.reduce((choices, key) => {
  choices[key] = DEFAULT_ACTIVE_FILTERS[key] ? DEFAULT_CHOICES[key] : null;
  return choices;
}, {} as ChoiceState);

// Categoria persistida no banco que alimenta cada filtro da tela.
const CATEGORY_BY_FILTER: Record<FilterKey, string> = {
  parity: "paridade",
  repetition: "repeticao",
  primes: "primos",
  frame: "moldura",
  sum: "soma",
  sumRange: "soma",
  grid: "grade",
  sequence: "sequencia"
};

// Matchers usados apenas para exibir percentuais: cobre as opcoes nao selecionaveis.
const DISPLAY_VALUES: Record<FilterKey, Array<Exclude<OptionMatcher, null>>> = {
  ...OPTION_VALUES,
  parity: [
    ...OPTION_VALUES.parity.slice(0, 6),
    [
      [0, 4],
      [11, 15]
    ]
  ],
  sumRange: [
    ...OPTION_VALUES.sumRange.slice(0, 4),
    [
      [0, 169],
      [221, 9999]
    ]
  ],
  grid: [0, [0, 1], 2, 3]
} as Record<FilterKey, Array<Exclude<OptionMatcher, null>>>;

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

// Monta as estatisticas exibidas na tela: usa as distribuicoes do banco quando disponiveis
// e cai para os percentuais calibrados do design quando a base ainda nao foi calculada.
export function buildLiveFilterStats(distributions: FilterDistributions | null): LiveFilterStats {
  const result = {} as LiveFilterStats;

  for (const key of FILTER_ORDER) {
    const base = FILTER_STATS[key];
    const buckets = distributions?.[CATEGORY_BY_FILTER[key]];

    if (!buckets || buckets.length === 0) {
      const fallbackTotal = key === "repetition" ? HISTORY_BASE - 1 : HISTORY_BASE;
      result[key] = {
        ...base,
        total: fallbackTotal,
        counts: base.items.map((item) => Math.round((fallbackTotal * item.percent) / 100))
      };
      continue;
    }

    const total = buckets.reduce((sum, bucket) => sum + bucket.quantidade, 0);
    const counts = base.items.map((_, index) => {
      const matcher = DISPLAY_VALUES[key][index];
      return buckets
        .filter((bucket) => matchesOption(bucket.valor, matcher))
        .reduce((sum, bucket) => sum + bucket.quantidade, 0);
    });

    result[key] = {
      ...base,
      base: key === "repetition" ? `${formatTotal(total)} pares de concursos` : `${formatTotal(total)} sorteios`,
      total,
      counts,
      items: base.items.map((item, index) => ({
        ...item,
        percent: total > 0 ? (counts[index] / total) * 100 : 0
      }))
    };
  }

  return result;
}

// Media ponderada de dezenas repetidas entre concursos consecutivos, direto da distribuicao do banco.
export function computeRepetitionAverage(distributions: FilterDistributions | null): number | null {
  const buckets = distributions?.repeticao;
  if (!buckets || buckets.length === 0) {
    return null;
  }

  const total = buckets.reduce((sum, bucket) => sum + bucket.quantidade, 0);
  if (total === 0) {
    return null;
  }

  return buckets.reduce((sum, bucket) => sum + bucket.valor * bucket.quantidade, 0) / total;
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
  | "faixasSoma"
  | "repetidasMinima"
  | "repetidasMaxima"
  | "primosMinimo"
  | "primosMaximo"
  | "molduraMinima"
  | "molduraMaxima"
  | "linhaColunaMinima"
  | "linhaColunaMaxima"
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
    payload.repetidasMaxima = repetitionBounds.max;
  }

  const primes = activeMatcher("primes", activeFilters, choices);
  const primesBounds = primes === null ? null : matcherBounds(primes);
  if (primesBounds) {
    payload.primosMinimo = primesBounds.min;
    payload.primosMaximo = Math.min(primesBounds.max, 9);
  }

  const frame = activeMatcher("frame", activeFilters, choices);
  const frameBounds = frame === null ? null : matcherBounds(frame);
  if (frameBounds) {
    payload.molduraMinima = frameBounds.min;
    payload.molduraMaxima = frameBounds.max;
  }

  const sum = activeMatcher("sum", activeFilters, choices);
  const sumBounds = sum === null ? null : matcherBounds(sum);
  if (sumBounds) {
    payload.somaMinima = sumBounds.min;
    payload.somaMaxima = sumBounds.max;
  }

  const sumRange = activeMatcher("sumRange", activeFilters, choices);
  if (sumRange !== null && typeof sumRange !== "number") {
    const ranges = Array.isArray(sumRange[0]) ? (sumRange as Array<[number, number]>) : [sumRange as [number, number]];
    payload.faixasSoma = ranges.map(([min, max]) => ({ somaMinima: min, somaMaxima: max }));
  }

  if (activeFilters.grid && choices.grid !== null && choices.grid !== undefined) {
    payload.linhaColunaMinima = choices.grid === 0 ? 2 : 1;
    payload.linhaColunaMaxima = 4;
  }

  const sequence = activeMatcher("sequence", activeFilters, choices);
  const sequenceBounds = sequence === null ? null : matcherBounds(sequence);
  if (sequenceBounds) {
    payload.sequenciaMaxima = sequenceBounds.max;
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
      { label: "7×8", percent: 50 },
      { label: "8×7", percent: 30 },
      { label: "6×9", percent: 15 },
      { label: "9×6", percent: 5 }
    ]
  },
  {
    name: "Repetição",
    bands: [
      { label: "9 dez.", percent: 50 },
      { label: "8 dez.", percent: 30 },
      { label: "10 dez.", percent: 20 }
    ]
  },
  {
    name: "Primos",
    bands: [
      { label: "5", percent: 45 },
      { label: "6", percent: 40 },
      { label: "4", percent: 8 },
      { label: "7", percent: 7 }
    ]
  },
  {
    name: "Moldura",
    bands: [
      { label: "9", percent: 45 },
      { label: "10", percent: 40 },
      { label: "8", percent: 8 },
      { label: "11", percent: 7 }
    ]
  },
  {
    name: "Soma",
    bands: [
      { label: "185–210", percent: 50 },
      { label: "180–212", percent: 30 },
      { label: "175–215", percent: 15 },
      { label: "170–220", percent: 5 }
    ]
  }
];
