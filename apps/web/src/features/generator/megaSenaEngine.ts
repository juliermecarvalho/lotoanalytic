// Dados estatisticos e mapeamento de filtros da tela de geracao de jogos da Mega-Sena.
// A geracao acontece no backend (POST /gerador/mega-sena/gerar); este modulo guarda os
// dados historicos exibidos na UI e converte as escolhas do usuario no payload de filtros.
// A Mega-Sena usa cartela 01-60, aposta de 6 dezenas e nao tem os conceitos de moldura
// nem de grade linha/coluna densa da Lotofacil.
import type { GenerateGamesRequest } from "../../lib/apiClient";

export const GAME_SIZE = 6;
export const BOARD_SIZE = 60;
export const HISTORY_BASE = 2800;
export const PREVIOUS_DRAW = [4, 10, 20, 33, 41, 52];

export type FilterKey = "parity" | "repetition" | "primes" | "sum" | "sumRange" | "sequence";

export const FILTER_ORDER: FilterKey[] = ["parity", "repetition", "primes", "sum", "sumRange", "sequence"];

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
  sum: "Soma das dezenas",
  sumRange: "Soma por faixa da estratégia",
  sequence: "Sequência máxima"
};

export const FILTER_STATS: Record<FilterKey, FilterStats> = {
  parity: {
    title: "Resumo de pares e ímpares",
    base: "sorteios da base",
    subtitle: "Como as 6 dezenas se dividem entre pares e ímpares em toda a base histórica.",
    items: [
      { label: "3 pares / 3 ímpares", percent: 33.2, preferred: true },
      { label: "4 pares / 2 ímpares", percent: 23.4, preferred: true },
      { label: "2 pares / 4 ímpares", percent: 23.1, preferred: true },
      { label: "5 pares / 1 ímpar", percent: 7.9, preferred: false },
      { label: "1 par / 5 ímpares", percent: 8.0, preferred: false },
      { label: "extremos (0 ou 6 pares)", percent: 4.4, preferred: false }
    ]
  },
  repetition: {
    title: "Resumo de repetidos do concurso anterior",
    base: "pares de concursos",
    subtitle: "Quantas dezenas do concurso imediatamente anterior se repetiram.",
    items: [
      { label: "0 repetidas", percent: 55.8, preferred: true },
      { label: "1 repetida", percent: 34.1, preferred: true },
      { label: "2 repetidas", percent: 9.1, preferred: false },
      { label: "3+ repetidas", percent: 1.0, preferred: false }
    ]
  },
  primes: {
    title: "Resumo de números primos",
    base: "sorteios da base",
    subtitle: "Primos de 01 a 60: 02 03 05 07 11 13 17 19 23 29 31 37 41 43 47 53 59.",
    items: [
      { label: "1 primo", percent: 33.4, preferred: true },
      { label: "2 primos", percent: 29.2, preferred: true },
      { label: "3 primos", percent: 15.1, preferred: true },
      { label: "0 primos", percent: 13.6, preferred: false },
      { label: "4+ primos", percent: 8.7, preferred: false }
    ]
  },
  sum: {
    title: "Resumo de somas das dezenas",
    base: "sorteios da base",
    subtitle: "Soma das 6 dezenas de cada sorteio (mínimo 21, máximo 345), agrupada em faixas.",
    items: [
      { label: "≤ 120", percent: 7.1, preferred: false },
      { label: "121–150", percent: 15.2, preferred: false },
      { label: "151–180", percent: 24.0, preferred: true },
      { label: "181–210", percent: 24.6, preferred: true },
      { label: "211–240", percent: 16.8, preferred: true },
      { label: "241–270", percent: 8.3, preferred: false },
      { label: "≥ 271", percent: 4.0, preferred: false }
    ]
  },
  sumRange: {
    title: "Resumo de somas por faixa da estratégia",
    base: "sorteios da base",
    subtitle: "Faixas ponderadas da estratégia, em bandas que não se sobrepõem.",
    items: [
      { label: "150–210 · peso 50%", percent: 48.6, preferred: true },
      { label: "130–149 e 211–230 · peso 30%", percent: 22.7, preferred: true },
      { label: "110–129 e 231–250 · peso 15%", percent: 14.3, preferred: true },
      { label: "90–109 e 251–270 · peso 5%", percent: 7.9, preferred: true },
      { label: "fora de 90–270", percent: 6.5, preferred: false }
    ]
  },
  sequence: {
    title: "Resumo de sequências consecutivas",
    base: "sorteios da base",
    subtitle: "Maior sequência de dezenas consecutivas dentro do sorteio.",
    items: [
      { label: "isoladas · máx. 1", percent: 49.5, preferred: false },
      { label: "máx. 2 seguidas", percent: 86.0, preferred: true },
      { label: "máx. 3 seguidas", percent: 96.8, preferred: false },
      { label: "sem limite", percent: 100, preferred: false }
    ]
  }
};

export const OPTION_VALUES: Record<FilterKey, OptionMatcher[]> = {
  parity: [3, 4, 2, 5, 1, null],
  repetition: [0, 1, 2, [3, 6]],
  primes: [1, 2, 3, 0, [4, 17]],
  sum: [
    [0, 120],
    [121, 150],
    [151, 180],
    [181, 210],
    [211, 240],
    [241, 270],
    [271, 999]
  ],
  sumRange: [
    [150, 210],
    [
      [130, 149],
      [211, 230]
    ],
    [
      [110, 129],
      [231, 250]
    ],
    [
      [90, 109],
      [251, 270]
    ],
    null
  ],
  sequence: [
    [0, 1],
    [0, 2],
    [0, 3],
    [0, 6]
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
  sum: true,
  sumRange: true,
  sequence: true
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
  sumRange: "soma",
  sequence: "sequencia"
};

// Matchers usados apenas para exibir percentuais: cobre as opcoes nao selecionaveis.
const DISPLAY_VALUES: Record<FilterKey, Array<Exclude<OptionMatcher, null>>> = {
  ...OPTION_VALUES,
  parity: [
    3,
    4,
    2,
    5,
    1,
    [
      [0, 0],
      [6, 6]
    ]
  ],
  sumRange: [
    ...OPTION_VALUES.sumRange.slice(0, 4),
    [
      [0, 89],
      [271, 9999]
    ]
  ]
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
    payload.repetidasMaxima = Math.min(repetitionBounds.max, GAME_SIZE);
  }

  const primes = activeMatcher("primes", activeFilters, choices);
  const primesBounds = primes === null ? null : matcherBounds(primes);
  if (primesBounds) {
    payload.primosMinimo = primesBounds.min;
    payload.primosMaximo = Math.min(primesBounds.max, 17);
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
      { label: "3×3", percent: 45 },
      { label: "4×2", percent: 28 },
      { label: "2×4", percent: 22 },
      { label: "outros", percent: 5 }
    ]
  },
  {
    name: "Repetição",
    bands: [
      { label: "0 dez.", percent: 55 },
      { label: "1 dez.", percent: 35 },
      { label: "2 dez.", percent: 10 }
    ]
  },
  {
    name: "Primos",
    bands: [
      { label: "1", percent: 40 },
      { label: "2", percent: 35 },
      { label: "3", percent: 15 },
      { label: "0", percent: 10 }
    ]
  },
  {
    name: "Soma",
    bands: [
      { label: "150–210", percent: 50 },
      { label: "130–230", percent: 30 },
      { label: "110–250", percent: 15 },
      { label: "90–270", percent: 5 }
    ]
  }
];
