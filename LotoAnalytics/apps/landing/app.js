const generatedGames = document.querySelector("#generated-games");
const gameCount = document.querySelector("#game-count");
const evenCount = document.querySelector("#even-count");
const evenCountOutput = document.querySelector("#even-count-output");
const sumMin = document.querySelector("#sum-min");
const generateButton = document.querySelector("#generate-games");
const copyButton = document.querySelector("#copy-games");
const resetButton = document.querySelector("#reset-generator");
const generatorStatus = document.querySelector("#generator-status");
const criteriaSummary = document.querySelector("#criteria-summary");

function setStatus(message, type = "neutral") {
  generatorStatus.textContent = message;
  generatorStatus.classList.toggle("is-error", type === "error");
  generatorStatus.classList.toggle("is-success", type === "success");
}

function formatNumber(value) {
  return String(value).padStart(2, "0");
}

function shuffle(numbers) {
  const copy = [...numbers];

  for (let index = copy.length - 1; index > 0; index -= 1) {
    const randomIndex = Math.floor(Math.random() * (index + 1));
    const current = copy[index];
    copy[index] = copy[randomIndex];
    copy[randomIndex] = current;
  }

  return copy;
}

function sum(numbers) {
  return numbers.reduce((total, number) => total + number, 0);
}

function countEven(numbers) {
  return numbers.filter((number) => number % 2 === 0).length;
}

function updateCriteriaSummary() {
  const total = Number(gameCount.value);
  const targetEvenCount = Number(evenCount.value);
  const minSumValue = Number(sumMin.value);
  const gameLabel = total === 1 ? "1 jogo" : `${total} jogos`;

  evenCountOutput.value = evenCount.value;
  evenCountOutput.textContent = evenCount.value;
  criteriaSummary.textContent = `Critério atual: ${gameLabel}, ${targetEvenCount} pares, soma mínima ${minSumValue}.`;
}

function tryGenerateGame(targetEvenCount, minSum) {
  const pool = Array.from({ length: 25 }, (_, index) => index + 1);
  const maxAttempts = 450;

  for (let attempt = 0; attempt < maxAttempts; attempt += 1) {
    const game = shuffle(pool).slice(0, 15).sort((a, b) => a - b);

    if (countEven(game) === targetEvenCount && sum(game) >= minSum) {
      return game;
    }
  }

  return null;
}

function validateSum(value) {
  const minAllowed = Number(sumMin.min);
  const maxAllowed = Number(sumMin.max);

  if (Number.isNaN(value) || value < minAllowed || value > maxAllowed) {
    setStatus(`Use uma soma entre ${sumMin.min} e ${sumMin.max}.`, "error");
    sumMin.focus();
    return false;
  }

  return true;
}

function renderGames() {
  const total = Number(gameCount.value);
  const targetEvenCount = Number(evenCount.value);
  const minSumValue = Number(sumMin.value);
  const games = new Set();
  let attempts = 0;
  const maxAttempts = total * 700;

  if (!validateSum(minSumValue)) {
    return;
  }

  generateButton.disabled = true;
  setStatus("Gerando amostra com os critérios selecionados...");

  while (games.size < total && attempts < maxAttempts) {
    const game = tryGenerateGame(targetEvenCount, minSumValue);

    if (!game) {
      attempts = maxAttempts;
      break;
    }

    games.add(game.map(formatNumber).join(" "));
    attempts += 1;
  }

  generateButton.disabled = false;

  if (games.size < total) {
    generatedGames.textContent = "Nenhum jogo foi gerado com esses critérios. Tente reduzir a soma mínima ou alterar a quantidade de pares.";
    setStatus("Critérios estreitos demais para esta amostra. Ajuste os filtros e gere novamente.", "error");
    return;
  }

  generatedGames.textContent = Array.from(games)
    .map((game, index) => `Jogo ${index + 1}: ${game}`)
    .join("\n");

  setStatus(`${games.size} jogo(s) gerado(s). A amostra aplica critérios, mas não garante resultado.`, "success");
}

function resetGenerator() {
  gameCount.value = "3";
  evenCount.value = "7";
  sumMin.value = "170";
  generatedGames.textContent = "Clique em gerar amostra para ver jogos com os critérios acima.";
  updateCriteriaSummary();
  setStatus("Amostra gratuita: até 5 jogos com filtros básicos.");
}

[gameCount, evenCount, sumMin].forEach((control) => {
  control.addEventListener("input", updateCriteriaSummary);
  control.addEventListener("change", updateCriteriaSummary);
});

generateButton.addEventListener("click", renderGames);
resetButton.addEventListener("click", resetGenerator);

copyButton.addEventListener("click", async () => {
  const content = generatedGames.textContent.trim();

  if (!content || content.includes("Clique em gerar") || content.includes("Nenhum jogo")) {
    renderGames();
  }

  if (generatedGames.textContent.includes("Nenhum jogo")) {
    return;
  }

  try {
    await navigator.clipboard.writeText(generatedGames.textContent.trim());
    copyButton.textContent = "Copiado";
    setStatus("Jogos copiados para a área de transferência.", "success");
  } catch {
    setStatus("Não foi possível copiar automaticamente. Selecione os jogos e copie manualmente.", "error");
  }

  window.setTimeout(() => {
    copyButton.textContent = "Copiar";
  }, 1400);
});

updateCriteriaSummary();
