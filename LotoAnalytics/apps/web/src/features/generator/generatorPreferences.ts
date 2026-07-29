// Persistencia local das preferencias do gerador (filtros, quantidade e dezenas marcadas).
import {
  ActiveFilterState,
  BOARD_SIZE,
  ChoiceState,
  DEFAULT_ACTIVE_FILTERS,
  FILTER_ORDER,
  FILTER_STATS,
  INITIAL_CHOICES
} from "./generatorEngine";

export const GENERATOR_PREFERENCES_KEY = "lotoanalytics.gerador.filtros";

export type GeneratorSelection = Record<number, "include" | "exclude">;

export type GeneratorPreferences = {
  choices: ChoiceState;
  activeFilters: ActiveFilterState;
  count: number;
  selection: GeneratorSelection;
};

// Salva as preferencias atuais do gerador no localStorage.
export function saveGeneratorPreferences(preferences: GeneratorPreferences, storage: Storage = localStorage): void {
  try {
    storage.setItem(GENERATOR_PREFERENCES_KEY, JSON.stringify(preferences));
  } catch {
    // Sem localStorage disponivel (modo privado, cota cheia): a tela segue funcionando sem persistir.
  }
}

// Remove as preferencias persistidas do gerador.
export function clearGeneratorPreferences(storage: Storage = localStorage): void {
  try {
    storage.removeItem(GENERATOR_PREFERENCES_KEY);
  } catch {
    // Ignorado: sem storage nao ha o que limpar.
  }
}

// Carrega e sanitiza as preferencias persistidas; retorna null quando nao ha nada valido salvo.
export function loadGeneratorPreferences(storage: Storage = localStorage): GeneratorPreferences | null {
  let raw: string | null;
  try {
    raw = storage.getItem(GENERATOR_PREFERENCES_KEY);
  } catch {
    return null;
  }

  if (!raw) {
    return null;
  }

  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch {
    return null;
  }

  if (typeof parsed !== "object" || parsed === null) {
    return null;
  }

  const candidate = parsed as {
    choices?: unknown;
    activeFilters?: unknown;
    count?: unknown;
    selection?: unknown;
  };

  return {
    choices: sanitizeChoices(candidate.choices),
    activeFilters: sanitizeActiveFilters(candidate.activeFilters),
    count: sanitizeCount(candidate.count),
    selection: sanitizeSelection(candidate.selection)
  };
}

// Valida cada escolha por filtro: indice dentro da faixa de opcoes, null explicito, ou o padrao.
function sanitizeChoices(value: unknown): ChoiceState {
  const source = (typeof value === "object" && value !== null ? value : {}) as Record<string, unknown>;
  const choices = {} as ChoiceState;

  for (const key of FILTER_ORDER) {
    const rawChoice = source[key];
    if (rawChoice === null) {
      choices[key] = null;
    } else if (
      typeof rawChoice === "number" &&
      Number.isInteger(rawChoice) &&
      rawChoice >= 0 &&
      rawChoice < FILTER_STATS[key].items.length
    ) {
      choices[key] = rawChoice;
    } else {
      choices[key] = INITIAL_CHOICES[key];
    }
  }

  return choices;
}

// Valida os interruptores por filtro, mantendo o padrao para valores invalidos.
function sanitizeActiveFilters(value: unknown): ActiveFilterState {
  const source = (typeof value === "object" && value !== null ? value : {}) as Record<string, unknown>;
  const activeFilters = {} as ActiveFilterState;

  for (const key of FILTER_ORDER) {
    const rawActive = source[key];
    activeFilters[key] = typeof rawActive === "boolean" ? rawActive : DEFAULT_ACTIVE_FILTERS[key];
  }

  return activeFilters;
}

// Mantem apenas quantidades positivas inteiras; o limite contratado e validado pela API.
function sanitizeCount(value: unknown): number {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    return 6;
  }

  return Math.max(1, Math.round(value));
}

// Aceita apenas dezenas validas do volante com estado incluir/excluir.
function sanitizeSelection(value: unknown): GeneratorSelection {
  const source = (typeof value === "object" && value !== null ? value : {}) as Record<string, unknown>;
  const selection: GeneratorSelection = {};

  for (const [key, state] of Object.entries(source)) {
    const numberValue = Number(key);
    if (
      Number.isInteger(numberValue) &&
      numberValue >= 1 &&
      numberValue <= BOARD_SIZE &&
      (state === "include" || state === "exclude")
    ) {
      selection[numberValue] = state;
    }
  }

  return selection;
}
