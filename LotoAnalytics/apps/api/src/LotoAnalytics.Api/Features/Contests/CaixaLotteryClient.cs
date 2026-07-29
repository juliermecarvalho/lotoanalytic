namespace LotoAnalytics.Api.Features.Contests;

public interface ICaixaLotteryClient
{
    Task<string> GetContestResultJsonAsync(string lotteryModeCode, int contestNumber, CancellationToken cancellationToken);
}

public sealed class CaixaLotteryClient(HttpClient httpClient) : ICaixaLotteryClient
{
    // Busca o JSON bruto de um concurso na API publica da Caixa.
    public async Task<string> GetContestResultJsonAsync(
        string lotteryModeCode,
        int contestNumber,
        CancellationToken cancellationToken)
    {
        var caixaLotteryCode = ToCaixaLotteryCode(lotteryModeCode);
        var requestUri = $"https://servicebus3.caixa.gov.br/portaldeloterias/api/{caixaLotteryCode}/{contestNumber}";
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.TryAddWithoutValidation("accept", "application/json, text/plain, */*");
        request.Headers.TryAddWithoutValidation("accept-language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
        request.Headers.TryAddWithoutValidation("origin", "https://loterias.caixa.gov.br");
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
        return statusCode is System.Net.HttpStatusCode.Forbidden
            or System.Net.HttpStatusCode.TooManyRequests
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
