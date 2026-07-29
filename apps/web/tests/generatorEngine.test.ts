import { describe, expect, it } from "vitest";
import {
  ActiveFilterState,
  DEFAULT_ACTIVE_FILTERS,
  DEFAULT_CHOICES,
  FILTER_ORDER,
  FILTER_STATS,
  HISTORY_BASE,
  INITIAL_CHOICES,
  buildFilterPayload,
  buildLiveFilterStats,
  computeRepetitionAverage
} from "../src/features/generator/generatorEngine";

const allFiltersOff = FILTER_ORDER.reduce((state, key) => {
  state[key] = false;
  return state;
}, {} as ActiveFilterState);

describe("buildFilterPayload", () => {
  it("maps the default strategy choices to the API filter contract", () => {
    const payload = buildFilterPayload(DEFAULT_ACTIVE_FILTERS, DEFAULT_CHOICES);

    expect(payload).toEqual({
      quantidadePares: 7,
      repetidasMinima: 9,
      repetidasMaxima: 9,
      primosMinimo: 5,
      primosMaximo: 5,
      molduraMinima: 9,
      molduraMaxima: 9,
      somaMinima: 185,
      somaMaxima: 194,
      faixasSoma: [{ somaMinima: 185, somaMaxima: 210 }],
      linhaColunaMinima: 2,
      linhaColunaMaxima: 4,
      sequenciaMaxima: 5
    });
  });

  it("returns an empty payload when every filter is turned off", () => {
    expect(buildFilterPayload(allFiltersOff, DEFAULT_CHOICES)).toEqual({});
  });

  it("maps range options to minimum and maximum bounds", () => {
    const payload = buildFilterPayload(
      { ...allFiltersOff, repetition: true, sum: true },
      { ...DEFAULT_CHOICES, repetition: 6, sum: 3 }
    );

    // Opcao "12+ repetidas" e a faixa de soma 185-194.
    expect(payload).toEqual({
      repetidasMinima: 12,
      repetidasMaxima: 15,
      somaMinima: 185,
      somaMaxima: 194
    });
  });

  it("expands multi-range strategy sums into multiple API ranges", () => {
    const payload = buildFilterPayload({ ...allFiltersOff, sumRange: true }, { ...DEFAULT_CHOICES, sumRange: 1 });

    expect(payload).toEqual({
      faixasSoma: [
        { somaMinima: 180, somaMaxima: 184 },
        { somaMinima: 211, somaMaxima: 212 }
      ]
    });
  });

  it("maps the flexible grid mode to looser row and column bounds", () => {
    const payload = buildFilterPayload({ ...allFiltersOff, grid: true }, { ...DEFAULT_CHOICES, grid: 1 });

    expect(payload).toEqual({ linhaColunaMinima: 1, linhaColunaMaxima: 4 });
  });

  it("clamps the prime upper bound to the nine primes on the board", () => {
    const payload = buildFilterPayload({ ...allFiltersOff, primes: true }, { ...DEFAULT_CHOICES, primes: 5 });

    // Opcao "8+ primos" tem matcher [8, 15], mas o volante so possui 9 primos.
    expect(payload).toEqual({ primosMinimo: 8, primosMaximo: 9 });
  });
});

describe("buildLiveFilterStats", () => {
  // Distribuicoes de uma base ficticia de 4 concursos (3 pares consecutivos para repeticao).
  const distributions = {
    paridade: [
      { valor: 7, quantidade: 2 },
      { valor: 8, quantidade: 1 },
      { valor: 12, quantidade: 1 }
    ],
    repeticao: [
      { valor: 9, quantidade: 2 },
      { valor: 6, quantidade: 1 }
    ],
    primos: [
      { valor: 5, quantidade: 3 },
      { valor: 8, quantidade: 1 }
    ],
    moldura: [{ valor: 9, quantidade: 4 }],
    soma: [
      { valor: 120, quantidade: 1 },
      { valor: 190, quantidade: 2 },
      { valor: 225, quantidade: 1 }
    ],
    grade: [
      { valor: 0, quantidade: 2 },
      { valor: 2, quantidade: 1 },
      { valor: 3, quantidade: 1 }
    ],
    sequencia: [
      { valor: 3, quantidade: 2 },
      { valor: 6, quantidade: 1 },
      { valor: 15, quantidade: 1 }
    ]
  };

  it("falls back to the design percentages when there is no live data", () => {
    const stats = buildLiveFilterStats(null);

    expect(stats.parity.total).toBe(HISTORY_BASE);
    expect(stats.parity.items[0].percent).toBe(FILTER_STATS.parity.items[0].percent);
    expect(stats.parity.counts[0]).toBe(Math.round((HISTORY_BASE * FILTER_STATS.parity.items[0].percent) / 100));
    expect(stats.repetition.total).toBe(HISTORY_BASE - 1);
  });

  it("computes option percentages and counts from the database distributions", () => {
    const stats = buildLiveFilterStats(distributions);

    // Paridade: 2 de 4 concursos com 7 pares; "outros extremos" cobre o concurso com 12 pares.
    expect(stats.parity.total).toBe(4);
    expect(stats.parity.items[0].percent).toBeCloseTo(50);
    expect(stats.parity.counts[0]).toBe(2);
    expect(stats.parity.items[6].percent).toBeCloseTo(25);

    // Repeticao usa o proprio total de pares consecutivos.
    expect(stats.repetition.total).toBe(3);
    expect(stats.repetition.items[0].percent).toBeCloseTo(66.7, 1);
    expect(stats.repetition.base).toBe("3 pares de concursos");

    // Primos: "8+ primos" agrega a faixa aberta.
    expect(stats.primes.items[5].percent).toBeCloseTo(25);

    // Soma alimenta tanto o resumo por intervalos quanto as faixas da estrategia.
    expect(stats.sum.items[3].percent).toBeCloseTo(50);
    expect(stats.sumRange.items[0].percent).toBeCloseTo(50);
    expect(stats.sumRange.items[4].percent).toBeCloseTo(50);

    // Grade: modo flexivel acumula classes 0 e 1; opcoes nao selecionaveis tambem exibem percentuais.
    expect(stats.grid.items[0].percent).toBeCloseTo(50);
    expect(stats.grid.items[1].percent).toBeCloseTo(50);
    expect(stats.grid.items[2].percent).toBeCloseTo(25);
    expect(stats.grid.items[3].percent).toBeCloseTo(25);

    // Sequencia: cada teto acumula os sorteios com sequencia menor ou igual.
    expect(stats.sequence.items[0].percent).toBeCloseTo(50);
    expect(stats.sequence.items[3].percent).toBeCloseTo(75);
    expect(stats.sequence.items[5].percent).toBeCloseTo(100);

    expect(stats.frame.items[0].percent).toBeCloseTo(100);
    expect(stats.frame.base).toBe("4 sorteios");
  });
});

describe("INITIAL_CHOICES", () => {
  it("starts every filter enabled with its default option chosen", () => {
    for (const key of FILTER_ORDER) {
      expect(DEFAULT_ACTIVE_FILTERS[key]).toBe(true);
      expect(INITIAL_CHOICES[key]).toBe(DEFAULT_CHOICES[key]);
      expect(INITIAL_CHOICES[key]).not.toBeNull();
    }
  });
});

describe("computeRepetitionAverage", () => {
  it("computes the weighted average of repeated numbers between consecutive contests", () => {
    const average = computeRepetitionAverage({
      repeticao: [
        { valor: 9, quantidade: 2 },
        { valor: 6, quantidade: 1 }
      ]
    });

    // (9*2 + 6*1) / 3 = 8
    expect(average).toBe(8);
  });

  it("returns null without live data or with an empty distribution", () => {
    expect(computeRepetitionAverage(null)).toBeNull();
    expect(computeRepetitionAverage({})).toBeNull();
    expect(computeRepetitionAverage({ repeticao: [] })).toBeNull();
  });
});
