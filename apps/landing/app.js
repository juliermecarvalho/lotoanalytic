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
const volanteScript = document.querySelector("#volante-script");
const scriptGame = document.querySelector("#script-game");
const buildScriptButton = document.querySelector("#build-script");
const copyScriptButton = document.querySelector("#copy-script");
const scriptOutput = document.querySelector("#script-output");

// Guarda os jogos gerados como arrays de numeros para montar o script do volante.
let lastGames = [];

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
  const seen = new Set();
  const games = [];
  let attempts = 0;
  const maxAttempts = total * 700;

  if (!validateSum(minSumValue)) {
    return;
  }

  hideScriptUi();
  generateButton.disabled = true;
  setStatus("Gerando jogos com os critérios selecionados...");

  while (games.length < total && attempts < maxAttempts) {
    const game = tryGenerateGame(targetEvenCount, minSumValue);

    if (!game) {
      attempts = maxAttempts;
      break;
    }

    const key = game.map(formatNumber).join(" ");
    if (!seen.has(key)) {
      seen.add(key);
      games.push(game);
    }

    attempts += 1;
  }

  generateButton.disabled = false;

  if (games.length < total) {
    lastGames = [];
    generatedGames.textContent = "Nenhum jogo foi gerado com esses critérios. Tente reduzir a soma mínima ou alterar a quantidade de pares.";
    setStatus("Critérios estreitos demais. Ajuste os filtros e gere novamente.", "error");
    return;
  }

  lastGames = games;

  generatedGames.textContent = games
    .map((game, index) => `Jogo ${index + 1}: ${game.map(formatNumber).join(" ")}`)
    .join("\n");

  populateScriptGames(games.length);
  setStatus(`${games.length} jogo(s) gerado(s). Aplicam critérios, mas não garantem resultado.`, "success");
}

function resetGenerator() {
  gameCount.value = "3";
  evenCount.value = "7";
  sumMin.value = "170";
  lastGames = [];
  generatedGames.textContent = "Clique em gerar jogos para ver as combinações com os critérios acima.";
  hideScriptUi();
  updateCriteriaSummary();
  setStatus("Ferramenta livre e sem custo: monte quantos jogos quiser.");
}

// Esconde e limpa a area do script do volante quando nao ha jogos validos.
function hideScriptUi() {
  volanteScript.hidden = true;
  scriptGame.innerHTML = "";
  scriptOutput.value = "";
}

// Preenche o seletor de jogos e revela a area do script do volante.
function populateScriptGames(total) {
  scriptGame.innerHTML = "";

  for (let index = 0; index < total; index += 1) {
    const option = document.createElement("option");
    option.value = String(index);
    option.textContent = `Jogo ${index + 1}`;
    scriptGame.appendChild(option);
  }

  scriptOutput.value = "";
  volanteScript.hidden = false;
}

// Monta um bookmarklet que marca as dezenas do jogo no volante da Caixa.
// Ele apenas clica nos numeros; nunca confirma nem paga.
// Prioriza elementos clicaveis (botao > role=button > link > li > td > label)
// e apenas folhas visiveis, para evitar clicar em numeros decorativos da pagina.
function buildBookmarklet(dezenas) {
  const alvo = dezenas.map((numero) => formatNumber(numero));
  const payload =
    "(function(){" +
    "var alvo=" +
    JSON.stringify(alvo) +
    ";var marcados=0,usados=[];" +
    "function score(el){var r=el.getAttribute&&el.getAttribute('role');var t=el.tagName;" +
    "if(t==='BUTTON')return 0;if(r==='button')return 1;if(t==='A')return 2;" +
    "if(t==='LI')return 3;if(t==='TD')return 4;if(t==='LABEL')return 5;return 6;}" +
    "var todos=[].slice.call(document.querySelectorAll('button,a,li,td,label,span,div,[role=button]'));" +
    "var folhas=todos.filter(function(el){return el.childElementCount===0&&el.offsetParent!==null;});" +
    "alvo.forEach(function(p){" +
    "var alt=String(parseInt(p,10));var achados=[];" +
    "for(var i=0;i<folhas.length;i++){if(usados.indexOf(i)!==-1)continue;" +
    "var t=(folhas[i].textContent||'').trim();if(t===p||t===alt)achados.push(i);}" +
    "if(!achados.length)return;" +
    "achados.sort(function(a,b){return score(folhas[a])-score(folhas[b]);});" +
    "var alvoEl=folhas[achados[0]];alvoEl.click();usados.push(achados[0]);marcados++;});" +
    "alert('LotoAnalytics: marcou '+marcados+' de '+alvo.length+' dezenas. Revise o volante antes de confirmar e pagar.');" +
    "})();";

  return "javascript:" + payload;
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

buildScriptButton.addEventListener("click", () => {
  const index = Number(scriptGame.value);
  const game = lastGames[index];

  if (!game) {
    setStatus("Gere os jogos antes de criar o script do volante.", "error");
    return;
  }

  scriptOutput.value = buildBookmarklet(game);
  setStatus(`Script do jogo ${index + 1} pronto. Copie e salve como favorito no navegador.`, "success");
});

copyScriptButton.addEventListener("click", async () => {
  if (!scriptOutput.value) {
    setStatus("Gere o script do volante antes de copiar.", "error");
    return;
  }

  try {
    await navigator.clipboard.writeText(scriptOutput.value);
    copyScriptButton.textContent = "Copiado";
    setStatus("Script copiado. Cole no campo de URL de um novo favorito.", "success");
  } catch {
    scriptOutput.select();
    setStatus("Não foi possível copiar automaticamente. Selecione o script e copie manualmente.", "error");
  }

  window.setTimeout(() => {
    copyScriptButton.textContent = "Copiar script";
  }, 1400);
});

updateCriteriaSummary();
