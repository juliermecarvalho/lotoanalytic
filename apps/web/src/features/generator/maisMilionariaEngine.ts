// Dados estatisticos e mapeamento de filtros da tela de geracao de jogos da +Milionaria.
// A geracao acontece no backend (POST /gerador/mais-milionaria/gerar); este modulo guarda os
// dados historicos exibidos na UI e converte as escolhas do usuario no payload de filtros.
// A +Milionaria usa cartela principal 01-50, aposta minima de 6 dezenas mais 2 trevos (01-06)
// e nao tem os conceitos de moldura nem de grade linha/coluna densa da Lotofacil. Os filtros
// abaixo tratam apenas as dezenas principais; os trevos sao sorteados a parte, sem filtro.
import type { GenerateGamesRequest } from "../../lib/apiClient";

export const GAME_SIZE = 6;
export const BOARD_SIZE = 50;
export const HISTORY_BASE = 250;
export const PREVIOUS_DRAW = [3, 17, 22, 31, 44, 48];

// Faixa oficial dos trevos e a quantidade minima por aposta da +Milionaria.
export const TREVO_MIN = 1;
export const TREVO_MAX = 6;
export const MIN_TREVOS = 2;
export const MAX_TREVOS = 6;

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
      { label: "3 pares / 3 ímpares", percent: 31.3, preferred: true },
      { label: "2 pares / 4 ímpares", percent: 23.4, preferred: true },
      { label: "4 pares / 2 ímpares", percent: 23.4, preferred: true },
      { label: "1 par / 5 ímpares", percent: 9.4, preferred: false },
      { label: "extremos (0, 5 ou 6 pares)", percent: 12.5, preferred: false }
    ]
  },
  repetition: {
    title: "Resumo de repetidos do concurso anterior",
    base: "pares de concursos",
    subtitle: "Quantas dezenas do concurso imediatamente anterior se repetiram.",
    items: [
      { label: "0 repetidas", percent: 44.4, preferred: true },
      { label: "1 repetida", percent: 41.0, preferred: true },
      { label: "2 repetidas", percent: 12.8, preferred: false },
      { label: "3+ repetidas", percent: 1.8, preferred: false }
    ]
  },
  primes: {
    title: "Resumo de números primos",
    base: "sorteios da base",
    subtitle: "Primos de 01 a 50: 02 03 05 07 11 13 17 19 23 29 31 37 41 43 47.",
    items: [
      { label: "2 primos", percent: 34.6, preferred: true },
      { label: "1 primo", percent: 30.6, preferred: true },
      { label: "3 primos", percent: 18.7, preferred: true },
      { label: "0 primos", percent: 10.2, preferred: false },
      { label: "4+ primos", percent: 5.8, preferred: false }
    ]
  },
  sum: {
    title: "Resumo de somas das dezenas",
    base: "sorteios da base",
    subtitle: "Soma das 6 dezenas de cada sorteio (mínimo 21, máximo 285), agrupada em faixas.",
    items: [
      { label: "≤ 110", percent: 7.5, preferred: false },
      { label: "111–135", percent: 17.0, preferred: false },
      { label: "136–160", percent: 27.0, preferred: true },
      { label: "161–185", percent: 26.0, preferred: true },
      { label: "186–210", percent: 15.0, preferred: true },
      { label: "211–235", percent: 5.5, preferred: false },
      { label: "≥ 236", percent: 2.0, preferred: false }
    ]
  },
  sumRange: {
    title: "Resumo de somas por faixa da estratégia",
    base: "sorteios da base",
    subtitle: "Faixas ponderadas da estratégia, em bandas que não se sobrepõem.",
    items: [
      { label: "130–176 · peso 50%", percent: 49.5, preferred: true },
      { label: "106–129 e 177–200 · peso 30%", percent: 29.0, preferred: true },
      { label: "82–105 e 201–224 · peso 15%", percent: 14.0, preferred: true },
      { label: "58–81 e 225–248 · peso 5%", percent: 5.5, preferred: true },
      { label: "fora de 58–248", percent: 2.0, preferred: false }
    ]
  },
  sequence: {
    title: "Resumo de sequências consecutivas",
    base: "sorteios da base",
    subtitle: "Maior sequência de dezenas consecutivas dentro do sorteio.",
    items: [
      { label: "isoladas · máx. 1", percent: 51.3, preferred: true },
      { label: "máx. 2 seguidas", percent: 88.0, preferred: true },
      { label: "máx. 3 seguidas", percent: 98.5, preferred: false },
      { label: "sem limite", percent: 100, preferred: false }
    ]
  }
};

export const OPTION_VALUES: Record<FilterKey, OptionMatcher[]> = {
  parity: [3, 2, 4, 1, null],
  repetition: [0, 1, 2, [3, 15]],
  primes: [2, 1, 3, 0, [4, 15]],
  sum: [
    [0, 110],
    [111, 135],
    [136, 160],
    [161, 185],
    [186, 210],
    [211, 235],
    [236, 999]
  ],
  sumRange: [
    [130, 176],
    [
      [106, 129],
      [177, 200]
    ],
    [
      [82, 105],
      [201, 224]
    ],
    [
      [58, 81],
      [225, 248]
    ],
    null
  ],
  sequence: [
    [0, 1],
    [0, 2],
    [0, 3],
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
  choices.sum = 2;
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
    2,
    4,
    1,
    [
      [0, 0],
      [5, 5],
      [6, 6]
    ]
  ],
  sumRange: [
    ...OPTION_VALUES.sumRange.slice(0, 4),
    [
      [0, 57],
      [249, 9999]
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
    payload.primosMaximo = Math.min(primesBounds.max, 15);
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
      { label: "3×3", percent: 31 },
      { label: "2×4", percent: 23 },
      { label: "4×2", percent: 23 },
      { label: "outros", percent: 23 }
    ]
  },
  {
    name: "Repetição",
    bands: [
      { label: "0 dez.", percent: 44 },
      { label: "1 dez.", percent: 41 },
      { label: "2 dez.", percent: 13 }
    ]
  },
  {
    name: "Primos",
    bands: [
      { label: "2", percent: 35 },
      { label: "1", percent: 31 },
      { label: "3", percent: 19 },
      { label: "0", percent: 10 }
    ]
  },
  {
    name: "Soma",
    bands: [
      { label: "130–176", percent: 50 },
      { label: "106–200", percent: 29 },
      { label: "82–224", percent: 14 },
      { label: "58–248", percent: 6 }
    ]
  }
];
