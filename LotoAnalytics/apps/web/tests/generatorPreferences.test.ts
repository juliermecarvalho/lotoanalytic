import { beforeEach, describe, expect, it } from "vitest";
import {
  GENERATOR_PREFERENCES_KEY,
  clearGeneratorPreferences,
  loadGeneratorPreferences,
  saveGeneratorPreferences
} from "../src/features/generator/generatorPreferences";
import { DEFAULT_ACTIVE_FILTERS, INITIAL_CHOICES } from "../src/features/generator/generatorEngine";

describe("generatorPreferences", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("round-trips the generator preferences through localStorage", () => {
    saveGeneratorPreferences({
      choices: { ...INITIAL_CHOICES, primes: null },
      activeFilters: { ...DEFAULT_ACTIVE_FILTERS, primes: false },
      count: 20,
      selection: { 1: "include", 25: "exclude" }
    });

    const loaded = loadGeneratorPreferences();

    expect(loaded).not.toBeNull();
    expect(loaded?.count).toBe(20);
    expect(loaded?.activeFilters.primes).toBe(false);
    expect(loaded?.choices.primes).toBeNull();
    expect(loaded?.choices.parity).toBe(INITIAL_CHOICES.parity);
    expect(loaded?.selection).toEqual({ 1: "include", 25: "exclude" });
  });

  it("returns null when nothing was saved or the payload is invalid", () => {
    expect(loadGeneratorPreferences()).toBeNull();

    localStorage.setItem(GENERATOR_PREFERENCES_KEY, "nao-e-json{");
    expect(loadGeneratorPreferences()).toBeNull();
  });

  it("sanitizes invalid values back to safe defaults", () => {
    localStorage.setItem(
      GENERATOR_PREFERENCES_KEY,
      JSON.stringify({
        choices: { parity: 99, repetition: "abc" },
        activeFilters: { parity: "sim" },
        count: 500,
        selection: { 30: "include", 5: "excluir", 7: "exclude" }
      })
    );

    const loaded = loadGeneratorPreferences();

    expect(loaded).not.toBeNull();
    // Indice fora da faixa e tipos invalidos voltam ao padrao do filtro.
    expect(loaded?.choices.parity).toBe(INITIAL_CHOICES.parity);
    expect(loaded?.choices.repetition).toBe(INITIAL_CHOICES.repetition);
    expect(loaded?.activeFilters.parity).toBe(DEFAULT_ACTIVE_FILTERS.parity);
    // Quantidades positivas nao recebem um teto local; o plano e validado pela API.
    expect(loaded?.count).toBe(500);
    expect(loaded?.selection).toEqual({ 7: "exclude" });
  });

  it("clears the stored preferences", () => {
    saveGeneratorPreferences({
      choices: { ...INITIAL_CHOICES },
      activeFilters: { ...DEFAULT_ACTIVE_FILTERS },
      count: 10,
      selection: {}
    });

    clearGeneratorPreferences();

    expect(localStorage.getItem(GENERATOR_PREFERENCES_KEY)).toBeNull();
  });
});
