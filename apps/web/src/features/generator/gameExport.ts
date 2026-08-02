// Exportadores dos jogos gerados: CSV (mesmo contrato do backend) e script jogos.js
// que preenche o volante automaticamente no site da Caixa.

// Contrato minimo comum a qualquer modalidade para exportacao (Lotofacil, Mega-Sena, ...).
type ExportableGame = { dezenas: string[]; somaDezenas: number };

// Corpo fixo do script de automacao do volante (mesmo comportamento do jogos.js original).
const VOLANTE_SCRIPT_BODY = `function selecionarNumero(numero) {
    const id = \`n\${String(numero).padStart(2, "0")}\`;
    const elemento = document.getElementById(id);

    if (elemento) {
        elemento.click();
        console.log(\`Selecionado: \${id}\`);
        return true;
    }

    console.warn(\`Elemento nao encontrado: \${id}\`);
    return false;
}

function selecionarJogo(jogo, indice) {
    console.log(\`Selecionando jogo \${indice + 1} de \${jogos.length}\`);
    jogo.forEach(selecionarNumero);
}

function clicarColocarNoCarrinho() {
    const botao = document.getElementById("colocarnocarrinho");

    if (botao) {
        botao.click();
        console.log("Clique realizado no botao Colocar no Carrinho.");
        return true;
    }

    console.warn("Botao Colocar no Carrinho nao encontrado: #colocarnocarrinho");
    return false;
}

function aguardar(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function iniciarPreenchimentoDosJogos() {
    for (let indice = 0; indice < jogos.length; indice += 1) {
        selecionarJogo(jogos[indice], indice);
        await aguardar(500);
        clicarColocarNoCarrinho();
        await aguardar(500);

        const continuar = confirm(
            \`Jogo \${indice + 1} enviado para o carrinho. Clique em OK para preencher o proximo jogo.\`
        );

        if (!continuar) {
            console.log("Preenchimento interrompido pelo usuario.");
            return;
        }
    }

    alert("Todos os jogos foram preenchidos.");
}

iniciarPreenchimentoDosJogos();
`;

// Monta o CSV dos jogos no mesmo formato do exportador de geracoes do backend.
export function buildGamesCsv(games: ExportableGame[]): string {
  const lines = ["numero_jogo,dezenas,soma_dezenas"];
  games.forEach((game, index) => {
    lines.push(`${index + 1},"${game.dezenas.join(" ")}",${game.somaDezenas}`);
  });

  return `${lines.join("\n")}\n`;
}

// Monta o jogos.js com o array dos jogos gerados seguido do script de automacao do volante.
export function buildGamesScript(games: ExportableGame[]): string {
  const gamesArray = games
    .map((game) => `    [${game.dezenas.map((dezena) => `"${dezena}"`).join(", ")}]`)
    .join(",\n");

  return `const jogos = [\n${gamesArray}\n];\n\n${VOLANTE_SCRIPT_BODY}`;
}

// Contrato de jogo com trevos, exclusivo da +Milionaria (dezenas principais + trevos 01-06).
type ExportableTrevoGame = { dezenas: string[]; trevos: string[]; somaDezenas: number };

// Corpo do script da +Milionaria: preenche dezenas (n01..n50) e trevos, tolerando variacoes
// de id do trevo no volante da Caixa (t01, t1, trevo01, trevo1) para se manter robusto.
const VOLANTE_TREVOS_SCRIPT_BODY = `function clicarPorId(id) {
    const elemento = document.getElementById(id);
    if (elemento) {
        elemento.click();
        return true;
    }
    return false;
}

function selecionarDezena(numero) {
    const id = \`n\${String(numero).padStart(2, "0")}\`;
    if (clicarPorId(id)) {
        console.log(\`Dezena selecionada: \${id}\`);
        return true;
    }
    console.warn(\`Dezena nao encontrada: \${id}\`);
    return false;
}

function selecionarTrevo(numero) {
    const bruto = String(numero).replace(/^0+/, "") || "0";
    const candidatos = [
        \`t\${bruto.padStart(2, "0")}\`,
        \`t\${bruto}\`,
        \`trevo\${bruto.padStart(2, "0")}\`,
        \`trevo\${bruto}\`,
        \`trevo-\${bruto}\`
    ];
    for (const id of candidatos) {
        if (clicarPorId(id)) {
            console.log(\`Trevo selecionado: \${id}\`);
            return true;
        }
    }
    console.warn(\`Trevo nao encontrado (tentado \${candidatos.join(", ")}). Marque o trevo \${bruto} manualmente.\`);
    return false;
}

function selecionarJogo(jogo, indice) {
    console.log(\`Selecionando jogo \${indice + 1} de \${jogos.length}\`);
    jogo.dezenas.forEach(selecionarDezena);
    jogo.trevos.forEach(selecionarTrevo);
}

function clicarColocarNoCarrinho() {
    const botao = document.getElementById("colocarnocarrinho");
    if (botao) {
        botao.click();
        console.log("Clique realizado no botao Colocar no Carrinho.");
        return true;
    }
    console.warn("Botao Colocar no Carrinho nao encontrado: #colocarnocarrinho");
    return false;
}

function aguardar(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

async function iniciarPreenchimentoDosJogos() {
    for (let indice = 0; indice < jogos.length; indice += 1) {
        selecionarJogo(jogos[indice], indice);
        await aguardar(500);
        clicarColocarNoCarrinho();
        await aguardar(500);

        const continuar = confirm(
            \`Jogo \${indice + 1} enviado para o carrinho. Clique em OK para preencher o proximo jogo.\`
        );

        if (!continuar) {
            console.log("Preenchimento interrompido pelo usuario.");
            return;
        }
    }

    alert("Todos os jogos foram preenchidos.");
}

iniciarPreenchimentoDosJogos();
`;

// Monta o CSV da +Milionaria incluindo a coluna de trevos ao lado das dezenas principais.
export function buildTrevoGamesCsv(games: ExportableTrevoGame[]): string {
  const lines = ["numero_jogo,dezenas,trevos,soma_dezenas"];
  games.forEach((game, index) => {
    lines.push(`${index + 1},"${game.dezenas.join(" ")}","${game.trevos.join(" ")}",${game.somaDezenas}`);
  });

  return `${lines.join("\n")}\n`;
}

// Monta o jogos.js da +Milionaria: cada jogo carrega dezenas e trevos para o script preencher no volante.
export function buildTrevoGamesScript(games: ExportableTrevoGame[]): string {
  const gamesArray = games
    .map((game) => {
      const dezenas = game.dezenas.map((dezena) => `"${dezena}"`).join(", ");
      const trevos = game.trevos.map((trevo) => `"${trevo}"`).join(", ");
      return `    { dezenas: [${dezenas}], trevos: [${trevos}] }`;
    })
    .join(",\n");

  return `const jogos = [\n${gamesArray}\n];\n\n${VOLANTE_TREVOS_SCRIPT_BODY}`;
}
