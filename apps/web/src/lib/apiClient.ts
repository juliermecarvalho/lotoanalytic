export type ApiClientOptions = {
  baseUrl: string;
  token?: string;
};

export type LotofacilStatisticsRequest = {
  dezenas: string[];
  dezenasAnteriores?: string[];
};

export type LotofacilStatisticsResponse = {
  quantidadePares: number;
  quantidadeImpares: number;
  somaDezenas: number;
  repetidasAnterior: string[];
  quantidadePrimos: number;
  quantidadeMoldura: number;
  quantidadeMiolo: number;
  maiorSequencia: number;
  distribuicaoLinhas: number[];
  distribuicaoColunas: number[];
};

export type SumRangeFilter = {
  somaMinima: number;
  somaMaxima: number;
};

export type GenerateGamesRequest = {
  quantidadeJogos: number;
  dezenasPorJogo: number;
  dezenasObrigatorias: string[];
  dezenasExcluidas: string[];
  dezenasAnteriores?: string[];
  quantidadePares?: number;
  quantidadeImpares?: number;
  somaMinima?: number;
  somaMaxima?: number;
  faixasSoma?: SumRangeFilter[];
  repetidasMinima?: number;
  repetidasMaxima?: number;
  primosMinimo?: number;
  primosMaximo?: number;
  molduraMinima?: number;
  molduraMaxima?: number;
  linhaColunaMinima?: number;
  linhaColunaMaxima?: number;
  sequenciaMaxima?: number;
  apenasIneditos?: boolean;
};

export type GeneratedGame = {
  dezenas: string[];
  quantidadePares: number;
  quantidadeImpares: number;
  somaDezenas: number;
  quantidadeRepetidas: number;
  quantidadePrimos: number;
  quantidadeMoldura: number;
  maiorSequencia: number;
};

export type GenerateGamesResponse = {
  jogos: GeneratedGame[];
  combinacoesTestadas: number;
};

// Jogo gerado da Mega-Sena: sem moldura (a cartela 10x6 nao tem esse conceito).
export type GeneratedMegaSenaGame = {
  dezenas: string[];
  quantidadePares: number;
  quantidadeImpares: number;
  somaDezenas: number;
  quantidadeRepetidas: number;
  quantidadePrimos: number;
  maiorSequencia: number;
};

export type GenerateMegaSenaGamesResponse = {
  jogos: GeneratedMegaSenaGame[];
  combinacoesTestadas: number;
};

// Jogo gerado da Quina: sem moldura (a cartela 10x8 nao tem esse conceito).
export type GeneratedQuinaGame = {
  dezenas: string[];
  quantidadePares: number;
  quantidadeImpares: number;
  somaDezenas: number;
  quantidadeRepetidas: number;
  quantidadePrimos: number;
  maiorSequencia: number;
};

export type GenerateQuinaGamesResponse = {
  jogos: GeneratedQuinaGame[];
  combinacoesTestadas: number;
};

// Aposta gerada da Lotomania: 50 dezenas de 00 a 99, sem moldura (a cartela 10x10 nao tem esse conceito).
export type GeneratedLotomaniaGame = {
  dezenas: string[];
  quantidadePares: number;
  quantidadeImpares: number;
  somaDezenas: number;
  quantidadeRepetidas: number;
  quantidadePrimos: number;
  maiorSequencia: number;
};

export type GenerateLotomaniaGamesResponse = {
  jogos: GeneratedLotomaniaGame[];
  combinacoesTestadas: number;
};

export type CheckGamesRequest = {
  dezenasSorteadas: string[];
  jogos: string[][];
};

export type CheckedGame = {
  numeroJogo: number;
  quantidadeAcertos: number;
  dezenasAcertadas: string[];
  premiado: boolean;
};

export type CheckGamesResponse = {
  jogos: CheckedGame[];
  resumoPremiacao: Record<string, number>;
};

export type CurrentUserResponse = {
  id: string;
  subject: string;
  username?: string;
  email?: string;
  roles: string[];
  ultimoLoginEm?: string;
  planoAtual?: {
    codigo: string;
    nome: string;
    limiteJogosPorGeracao: number;
    permiteExportarCsv: boolean;
    permiteExportarPdf: boolean;
  };
};

export type LotteryModeResponse = {
  codigo: string;
  nome: string;
  tipoJogoCaixa: string;
  quantidadeDezenasPrincipal: number;
  valorApostaSimples: number | null;
  ativa: boolean;
};

export type FilterStatisticsResponse = {
  codigoModalidade: string;
  totalConcursos: number;
  atualizadoEm?: string | null;
  categorias: Record<string, Array<{ valor: number; quantidade: number }>>;
};

export type LatestContestResponse = {
  codigoModalidade: string;
  numeroConcurso: number;
  dataApuracao?: string | null;
  dezenas: string[];
  totalConcursos: number;
};

export type DashboardSummary = {
  somaMedia: number;
  repeticaoMedia: number;
  combinacoesIneditasPercentual: number;
  faixaSomaPreferencialPercentual: number;
};

export type DashboardFrequency = {
  dezena: number;
  quantidade: number;
  percentual: number;
  atraso: number;
  ultimoConcurso: number | null;
};

export type DashboardLatestContest = {
  numero: number;
  dataApuracao?: string | null;
  dezenas: string[];
  pares: number;
  impares: number;
  soma: number;
  primos: number;
  moldura: number;
  repetidasAnterior: number;
};

export type DashboardResponse = {
  codigoModalidade: string;
  totalConcursos: number;
  ultimoConcurso: DashboardLatestContest | null;
  resumo: DashboardSummary;
  frequencias: DashboardFrequency[];
  categorias: Record<string, Array<{ valor: number; quantidade: number }>>;
};

export type ContestImportResponse = {
  codigoModalidade: string;
  numeroConcurso: number;
  quantidadeDezenasPrincipal: number;
  quantidadeFaixasPremio: number;
};

export type ContestBulkUpdateRequest = {
  inicio?: number;
  limitePorModalidade?: number;
  pausaMs?: number;
  pausaErroMs?: number;
  maxTentativasErro?: number;
};

export type ContestBulkUpdateResponse = {
  inicioEm: string;
  finalizadoEm: string;
  totalImportado: number;
  modalidades: Array<{
    codigoModalidade: string;
    nomeModalidade: string;
    concursoInicial: number;
    proximoConcurso: number;
    concursosImportados: number[];
    quantidadeImportada: number;
    status: string;
    erro?: string;
  }>;
};

export type ContestBulkUpdateProgressEvent = {
  evento: "modalidade_iniciada" | "concurso_importado" | "tentativa_falhou" | "modalidade_concluida";
  codigoModalidade: string;
  nomeModalidade: string;
  indiceModalidade: number;
  totalModalidades: number;
  numeroConcurso?: number | null;
  dezenas?: string[] | null;
  quantidadeImportada: number;
  retomarDoConcurso?: number | null;
  ultimoConcursoSalvo?: number | null;
  proximoConcurso?: number | null;
  totalNoBanco?: number | null;
  status?: string | null;
  erro?: string | null;
  tentativa?: number | null;
  aguardarMs?: number | null;
};

export type ContestBulkUpdateCompletedEvent = {
  evento: "concluido";
  resultado: ContestBulkUpdateResponse;
};

export type ContestBulkUpdateStreamEvent = ContestBulkUpdateProgressEvent | ContestBulkUpdateCompletedEvent;

export type GenerationHistoryResponse = {
  geracoes: Array<{
    id: string;
    quantidadeJogos: number;
    dezenasPorJogo: number;
    criadoEm: string;
    jogos: Array<{ numeroJogo: number; dezenas: string[]; somaDezenas: number }>;
  }>;
};

export type CheckingHistoryResponse = {
  conferencias: Array<{
    id: string;
    quantidadeJogos: number;
    criadoEm: string;
    resumoPremiacao: Record<string, number>;
    jogos: Array<{ numeroJogo: number; quantidadeAcertos: number; dezenasAcertadas: string[] }>;
  }>;
};

export class ApiClient {
  private readonly baseUrl: string;

  private readonly token?: string;

  public constructor(options: ApiClientOptions) {
    this.baseUrl = options.baseUrl.replace(/\/$/, "");
    this.token = options.token;
  }

  // Envia uma requisicao JSON para a API e devolve o contrato tipado.
  public async postJson<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse> {
    return await this.request<TResponse>(path, {
      method: "POST",
      body: JSON.stringify(body)
    });
  }

  // Consulta um recurso JSON autenticado ou publico.
  public async getJson<TResponse>(path: string): Promise<TResponse> {
    return await this.request<TResponse>(path, { method: "GET" });
  }

  // Envia JSON e consome a resposta NDJSON linha a linha, entregando cada evento ao callback.
  public async postJsonStream<TRequest, TEvent>(
    path: string,
    body: TRequest,
    onEvent: (event: TEvent) => void
  ): Promise<void> {
    const response = await fetch(`${this.baseUrl}${path}`, {
      method: "POST",
      headers: this.buildHeaders(true),
      body: JSON.stringify(body)
    });

    if (!response.ok) {
      throw new Error(`Falha HTTP ${response.status}`);
    }

    if (!response.body) {
      throw new Error("Resposta sem corpo para streaming.");
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = "";

    for (;;) {
      const { done, value } = await reader.read();
      if (done) {
        break;
      }

      buffer += decoder.decode(value, { stream: true });
      let lineBreakIndex = buffer.indexOf("\n");
      while (lineBreakIndex >= 0) {
        const line = buffer.slice(0, lineBreakIndex).trim();
        buffer = buffer.slice(lineBreakIndex + 1);
        if (line) {
          onEvent(JSON.parse(line) as TEvent);
        }
        lineBreakIndex = buffer.indexOf("\n");
      }
    }

    const rest = buffer.trim();
    if (rest) {
      onEvent(JSON.parse(rest) as TEvent);
    }
  }

  // Baixa um arquivo texto retornado pela API.
  public async getText(path: string): Promise<string> {
    const response = await fetch(`${this.baseUrl}${path}`, {
      method: "GET",
      headers: this.buildHeaders(false)
    });

    if (!response.ok) {
      throw new Error(`Falha HTTP ${response.status}`);
    }

    return await response.text();
  }

  // Executa a chamada HTTP e normaliza erros da API para a interface.
  private async request<TResponse>(path: string, init: RequestInit): Promise<TResponse> {
    const response = await fetch(`${this.baseUrl}${path}`, {
      ...init,
      headers: this.buildHeaders(true)
    });

    if (!response.ok) {
      throw new Error(`Falha HTTP ${response.status}`);
    }

    return (await response.json()) as TResponse;
  }

  // Monta cabecalhos com token JWT quando informado pelo usuario.
  private buildHeaders(includeJson: boolean): HeadersInit {
    const headers: Record<string, string> = {};

    if (includeJson) {
      headers["Content-Type"] = "application/json";
    }

    if (this.token) {
      headers.Authorization = `Bearer ${this.token}`;
    }

    return headers;
  }
}

export function parseNumbers(value: string): string[] {
  return value
    .split(/[\s,;]+/)
    .map((part) => part.trim())
    .filter(Boolean);
}

export function parseGames(value: string): string[][] {
  return value
    .split(/\r?\n/)
    .map(parseNumbers)
    .filter((game) => game.length > 0);
}
