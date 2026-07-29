import { describe, expect, it } from "vitest";
import { GeneratedGame } from "../src/lib/apiClient";
import { buildGamesCsv, buildGamesScript } from "../src/features/generator/gameExport";

const games: GeneratedGame[] = [
  {
    dezenas: ["01", "02", "04", "06", "08", "10", "11", "12", "14", "15", "17", "19", "21", "23", "24"],
    quantidadePares: 7,
    quantidadeImpares: 8,
    somaDezenas: 187,
    quantidadeRepetidas: 9,
    quantidadePrimos: 5,
    quantidadeMoldura: 9,
    maiorSequencia: 2
  },
  {
    dezenas: ["02", "03", "05", "07", "09", "10", "12", "13", "14", "16", "18", "20", "22", "24", "25"],
    quantidadePares: 8,
    quantidadeImpares: 7,
    somaDezenas: 200,
    quantidadeRepetidas: 8,
    quantidadePrimos: 5,
    quantidadeMoldura: 10,
    maiorSequencia: 3
  }
];

describe("buildGamesCsv", () => {
  it("builds the same csv contract used by the backend exporter", () => {
    const csv = buildGamesCsv(games);
    const lines = csv.trimEnd().split("\n");

    expect(lines[0]).toBe("numero_jogo,dezenas,soma_dezenas");
    expect(lines[1]).toBe('1,"01 02 04 06 08 10 11 12 14 15 17 19 21 23 24",187');
    expect(lines[2]).toBe('2,"02 03 05 07 09 10 12 13 14 16 18 20 22 24 25",200');
    expect(lines).toHaveLength(3);
  });
});

describe("buildGamesScript", () => {
  it("builds the jogos.js script with the generated games array", () => {
    const script = buildGamesScript(games);

    expect(script).toContain('const jogos = [\n    ["01", "02", "04", "06", "08", "10", "11", "12", "14", "15", "17", "19", "21", "23", "24"],');
    expect(script).toContain('    ["02", "03", "05", "07", "09", "10", "12", "13", "14", "16", "18", "20", "22", "24", "25"]\n];');
  });

  it("keeps the volante automation functions from the original script", () => {
    const script = buildGamesScript(games);

    expect(script).toContain("function selecionarNumero(numero)");
    expect(script).toContain('document.getElementById("colocarnocarrinho")');
    expect(script).toContain("iniciarPreenchimentoDosJogos();");
  });
});
