import { http, HttpResponse } from "msw";

export type MockApiRequest = {
  authorization: string | null;
  method: string;
  url: string;
};

export const mockApiRequests: MockApiRequest[] = [];

export function clearMockApiRequests() {
  mockApiRequests.length = 0;
}

function track(request: Request) {
  mockApiRequests.push({
    authorization: request.headers.get("Authorization"),
    method: request.method,
    url: request.url
  });
}

export const handlers = [
  http.post("https://localhost:7101/estatisticas/lotofacil/calcular", ({ request }) => {
    track(request);

    return HttpResponse.json({
      quantidadePares: 7,
      quantidadeImpares: 8,
      somaDezenas: 120,
      repetidasAnterior: ["01", "02"],
      quantidadePrimos: 6,
      quantidadeMoldura: 10,
      quantidadeMiolo: 5,
      maiorSequencia: 5,
      distribuicaoLinhas: [3, 3, 3, 3, 3],
      distribuicaoColunas: [3, 3, 3, 3, 3]
    });
  }),
  http.post("https://localhost:7101/gerador/lotofacil/gerar", ({ request }) => {
    track(request);

    return HttpResponse.json({
      jogos: [
        {
          dezenas: ["01", "02", "04", "06", "08", "10", "11", "12", "14", "15", "17", "19", "21", "23", "24"],
          quantidadePares: 7,
          quantidadeImpares: 8,
          somaDezenas: 187,
          quantidadeRepetidas: 9,
          quantidadePrimos: 5,
          quantidadeMoldura: 9,
          maiorSequencia: 2
        }
      ],
      combinacoesTestadas: 4210
    });
  }),
  http.post("https://localhost:7101/conferencias/lotofacil/conferir", ({ request }) => {
    track(request);

    return HttpResponse.json({
      jogos: [
        {
          numeroJogo: 1,
          quantidadeAcertos: 15,
          dezenasAcertadas: ["01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13", "14", "15"],
          premiado: true
        }
      ],
      resumoPremiacao: { "15": 1 }
    });
  }),
  http.get("*/concursos/lotofacil/ultimo", ({ request }) => {
    track(request);

    return HttpResponse.json({
      codigoModalidade: "lotofacil",
      numeroConcurso: 3412,
      dataApuracao: "2026-07-25",
      dezenas: ["01", "02", "03", "05", "07", "09", "11", "13", "14", "17", "19", "20", "22", "24", "25"],
      totalConcursos: 3412
    });
  }),
  http.get("*/estatisticas/lotofacil/filtros", ({ request }) => {
    track(request);

    return HttpResponse.json({
      codigoModalidade: "lotofacil",
      totalConcursos: 4,
      atualizadoEm: "2026-07-26T09:00:00Z",
      categorias: {
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
      }
    });
  }),
  http.get("https://localhost:7101/usuarios/me", ({ request }) => {
    track(request);

    return HttpResponse.json({
      id: "77777777-7777-7777-7777-777777777777",
      subject: "keycloak-subject",
      username: "usuario.teste",
      email: "usuario@teste.local",
      roles: ["usuario_premium"],
      planoAtual: {
        codigo: "premium",
        nome: "Plano Premium",
        limiteJogosPorGeracao: 100,
        permiteExportarCsv: true,
        permiteExportarPdf: true
      }
    });
  }),
  http.get("https://localhost:7101/modalidades", ({ request }) => {
    track(request);

    return HttpResponse.json([
      {
        codigo: "lotofacil",
        nome: "Lotofacil",
        tipoJogoCaixa: "LOTOFACIL",
        quantidadeDezenasPrincipal: 15,
        valorApostaSimples: 3.5,
        ativa: true
      }
    ]);
  }),
  http.post("https://localhost:7101/concursos/lotofacil/1/importar", ({ request }) => {
    track(request);

    return HttpResponse.json({
      codigoModalidade: "lotofacil",
      numeroConcurso: 1,
      quantidadeDezenasPrincipal: 15,
      quantidadeFaixasPremio: 5
    });
  }),
  http.post("https://localhost:7101/admin/concursos/atualizar-todos/progresso", ({ request }) => {
    track(request);

    const encoder = new TextEncoder();
    const dezenas = ["01", "02", "03", "05", "07", "09", "11", "13", "14", "17", "19", "20", "22", "24", "25"];
    const lines = [
      {
        evento: "modalidade_iniciada",
        codigoModalidade: "lotofacil",
        nomeModalidade: "Lotofacil",
        indiceModalidade: 1,
        totalModalidades: 9,
        quantidadeImportada: 0,
        retomarDoConcurso: 3744,
        ultimoConcursoSalvo: 3743
      },
      {
        evento: "concurso_importado",
        codigoModalidade: "lotofacil",
        nomeModalidade: "Lotofacil",
        indiceModalidade: 1,
        totalModalidades: 9,
        numeroConcurso: 3744,
        dezenas,
        quantidadeImportada: 1
      },
      {
        evento: "concurso_importado",
        codigoModalidade: "lotofacil",
        nomeModalidade: "Lotofacil",
        indiceModalidade: 1,
        totalModalidades: 9,
        numeroConcurso: 3745,
        dezenas,
        quantidadeImportada: 2
      },
      {
        evento: "tentativa_falhou",
        codigoModalidade: "lotofacil",
        nomeModalidade: "Lotofacil",
        indiceModalidade: 1,
        totalModalidades: 9,
        numeroConcurso: 3746,
        quantidadeImportada: 2,
        tentativa: 1,
        aguardarMs: 5000
      },
      {
        // Evento futuro que este front nao conhece: deve ser ignorado sem poluir o log.
        evento: "evento_futuro_desconhecido",
        codigoModalidade: "lotofacil",
        nomeModalidade: "Lotofacil",
        indiceModalidade: 1,
        totalModalidades: 9,
        quantidadeImportada: 0
      },
      {
        evento: "modalidade_concluida",
        codigoModalidade: "lotofacil",
        nomeModalidade: "Lotofacil",
        indiceModalidade: 1,
        totalModalidades: 9,
        quantidadeImportada: 2,
        status: "atualizado",
        proximoConcurso: 3746,
        totalNoBanco: 3745
      },
      {
        evento: "concluido",
        resultado: {
          inicioEm: "2026-07-25T16:00:00Z",
          finalizadoEm: "2026-07-25T16:01:00Z",
          totalImportado: 2,
          modalidades: [
            {
              codigoModalidade: "lotofacil",
              nomeModalidade: "Lotofacil",
              concursoInicial: 3744,
              proximoConcurso: 3746,
              concursosImportados: [3744, 3745],
              quantidadeImportada: 2,
              status: "atualizado"
            }
          ]
        }
      }
    ];
    const stream = new ReadableStream({
      start(controller) {
        for (const line of lines) {
          controller.enqueue(encoder.encode(`${JSON.stringify(line)}\n`));
        }
        controller.close();
      }
    });

    return new HttpResponse(stream, { headers: { "Content-Type": "application/x-ndjson" } });
  }),
  http.get("https://localhost:7101/usuarios/me/geracoes", ({ request }) => {
    track(request);

    return HttpResponse.json({
      geracoes: [
        {
          id: "11111111-1111-1111-1111-111111111111",
          quantidadeJogos: 1,
          dezenasPorJogo: 15,
          criadoEm: "2026-07-25T12:00:00Z",
          jogos: [{ numeroJogo: 1, dezenas: ["01", "02", "03", "04", "05"], somaDezenas: 15 }]
        }
      ]
    });
  }),
  http.get("https://localhost:7101/usuarios/me/conferencias", ({ request }) => {
    track(request);

    return HttpResponse.json({
      conferencias: [
        {
          id: "21212121-2121-2121-2121-212121212121",
          quantidadeJogos: 1,
          criadoEm: "2026-07-25T12:00:00Z",
          resumoPremiacao: { "11": 0, "12": 0, "13": 0, "14": 0, "15": 1 },
          jogos: [{ numeroJogo: 1, quantidadeAcertos: 15, dezenasAcertadas: ["01", "02", "03"] }]
        }
      ]
    });
  }),
  http.get("https://localhost:7101/usuarios/me/geracoes/11111111-1111-1111-1111-111111111111/exportar-csv", ({ request }) => {
    track(request);

    return HttpResponse.text("numero_jogo,dezenas,soma_dezenas\n1,\"01 02 03 04 05\",15\n");
  })
];
