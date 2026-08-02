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
