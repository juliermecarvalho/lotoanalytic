using Microsoft.Extensions.Options;

namespace LotoAnalytics.Api.Features.Contests;

public interface ICaixaLotteryClient
{
    Task<string> GetContestResultJsonAsync(string lotteryModeCode, int contestNumber, CancellationToken cancellationToken);
}

public sealed class CaixaLotteryClient(HttpClient httpClient, IOptions<CaixaLotteryOptions> options) : ICaixaLotteryClient
{
    private readonly string baseUrl = NormalizeBaseUrl(options.Value.BaseUrl);

    // Busca o JSON bruto de um concurso na API publica da Caixa.
    public async Task<string> GetContestResultJsonAsync(
        string lotteryModeCode,
        int contestNumber,
        CancellationToken cancellationToken)
    {
        var caixaLotteryCode = ToCaixaLotteryCode(lotteryModeCode);
        var requestUri = $"{baseUrl}/{caixaLotteryCode}/{contestNumber}";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("accept", "application/json, text/plain, */*");
        request.Headers.TryAddWithoutValidation("accept-language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
        request.Headers.TryAddWithoutValidation("origin", "https://loterias.caixa.gov.br");
        request.Headers.TryAddWithoutValidation("referer", "https://loterias.caixa.gov.br/");
        request.Headers.TryAddWithoutValidation("sec-fetch-dest", "empty");
        request.Headers.TryAddWithoutValidation("sec-fetch-mode", "cors");
        request.Headers.TryAddWithoutValidation("sec-fetch-site", "same-site");
        request.Headers.TryAddWithoutValidation(
            "user-agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw new CaixaTransientApiException(lotteryModeCode, contestNumber, "Erro temporario ao chamar a API da Caixa.", exception);
        }

        using (response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new CaixaContestNotFoundException(lotteryModeCode, contestNumber);
            }

            // O 403 do CDN da Caixa e bloqueio por origem geografica e nao se resolve repetindo a chamada.
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                throw new CaixaAccessBlockedException(lotteryModeCode, contestNumber, requestUri);
            }

            if (IsTransientStatusCode(response.StatusCode))
            {
                throw new CaixaTransientApiException(
                    lotteryModeCode,
                    contestNumber,
                    $"API da Caixa retornou HTTP {(int)response.StatusCode}.");
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
    }

    // Remove a barra final do endereco base para montar a URL do concurso sem duplicar separadores.
    private static string NormalizeBaseUrl(string? baseUrl)
    {
        var effectiveBaseUrl = string.IsNullOrWhiteSpace(baseUrl)
            ? CaixaLotteryOptions.DefaultBaseUrl
            : baseUrl;

        return effectiveBaseUrl.TrimEnd('/');
    }

    // Converte o codigo interno para o slug esperado pela API da Caixa.
    private static string ToCaixaLotteryCode(string lotteryModeCode)
    {
        return lotteryModeCode switch
        {
            "mega_sena" => "megasena",
            "dupla_sena" => "duplasena",
            "dia_de_sorte" => "diadesorte",
            "super_sete" => "supersete",
            _ => lotteryModeCode
        };
    }

    // Indica codigos HTTP que devem repetir a mesma chamada apos pausa.
    private static bool IsTransientStatusCode(System.Net.HttpStatusCode statusCode)
    {
        return statusCode is System.Net.HttpStatusCode.TooManyRequests
            or System.Net.HttpStatusCode.InternalServerError
            or System.Net.HttpStatusCode.BadGateway
            or System.Net.HttpStatusCode.ServiceUnavailable
            or System.Net.HttpStatusCode.GatewayTimeout;
    }
}

public sealed class CaixaContestNotFoundException(string lotteryModeCode, int contestNumber)
    : Exception($"Concurso {contestNumber} da modalidade {lotteryModeCode} nao encontrado na Caixa.")
{
    public string LotteryModeCode { get; } = lotteryModeCode;

    public int ContestNumber { get; } = contestNumber;
}

public sealed class CaixaAccessBlockedException(string lotteryModeCode, int contestNumber, string requestUri)
    : Exception(
        $"Acesso bloqueado (HTTP 403) ao consultar {requestUri}. " +
        "O CDN da Caixa bloqueia requisicoes vindas de fora do Brasil; configure 'Caixa:BaseUrl' " +
        "para um relay brasileiro ou 'Caixa:Proxy' para sair por um IP no Brasil. " +
        $"Modalidade: {lotteryModeCode}. Concurso: {contestNumber}.")
{
    public string LotteryModeCode { get; } = lotteryModeCode;

    public int ContestNumber { get; } = contestNumber;

    public string RequestUri { get; } = requestUri;
}

public sealed class CaixaTransientApiException(
    string lotteryModeCode,
    int contestNumber,
    string message,
    Exception? innerException = null)
    : Exception($"{message} Modalidade: {lotteryModeCode}. Concurso: {contestNumber}.", innerException)
{
    public string LotteryModeCode { get; } = lotteryModeCode;

    public int ContestNumber { get; } = contestNumber;
}
