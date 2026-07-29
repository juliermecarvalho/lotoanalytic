using System.Globalization;
using System.Text.Json;

namespace LotoAnalytics.Api.Features.Contests;

public static class CaixaLotteryResultParser
{
    // Converte o JSON bruto da Caixa em um contrato interno normalizado.
    public static CaixaLotteryResult Parse(string resultJson)
    {
        using var document = JsonDocument.Parse(resultJson);
        var root = document.RootElement;

        return new CaixaLotteryResult(
            ContestNumber: root.GetProperty("numero").GetInt32(),
            CaixaGameType: root.GetProperty("tipoJogo").GetString() ?? string.Empty,
            DrawDate: ReadBrazilianDate(root, "dataApuracao"),
            MainNumbers: ReadStringArray(root, "listaDezenas"),
            SecondDrawNumbers: ReadStringArray(root, "listaDezenasSegundoSorteio"),
            DrawOrderNumbers: ReadStringArray(root, "dezenasSorteadasOrdemSorteio"),
            Trevos: ReadStringArray(root, "trevosSorteados"),
            PrizeTiers: ReadPrizeTiers(root),
            WinnerCities: ReadWinnerCities(root));
    }

    // Le uma data no formato brasileiro usado pela API da Caixa.
    private static DateOnly? ReadBrazilianDate(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = property.GetString();
        return DateOnly.TryParseExact(value, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    // Le uma propriedade array de strings, tratando null e ausencia como lista vazia.
    private static IReadOnlyList<string> ReadStringArray(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        return property.EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();
    }

    // Le as faixas de premio retornadas no campo listaRateioPremio.
    private static IReadOnlyList<PrizeTier> ReadPrizeTiers(JsonElement root)
    {
        if (!root.TryGetProperty("listaRateioPremio", out var property) || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        return property.EnumerateArray()
            .Select(item => new PrizeTier(
                Tier: item.GetProperty("faixa").GetInt32(),
                Description: item.GetProperty("descricaoFaixa").GetString() ?? string.Empty,
                WinnersCount: item.GetProperty("numeroDeGanhadores").GetInt32(),
                PrizeValue: item.GetProperty("valorPremio").GetDecimal()))
            .ToArray();
    }

    // Le os municipios ganhadores retornados quando existe detalhamento por cidade.
    private static IReadOnlyList<WinnerCity> ReadWinnerCities(JsonElement root)
    {
        if (!root.TryGetProperty("listaMunicipioUFGanhadores", out var property) || property.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        return property.EnumerateArray()
            .Select(item => new WinnerCity(
                City: item.GetProperty("municipio").GetString() ?? string.Empty,
                State: item.GetProperty("uf").GetString() ?? string.Empty,
                WinnersCount: item.GetProperty("ganhadores").GetInt32()))
            .ToArray();
    }
}

public sealed record CaixaLotteryResult(
    int ContestNumber,
    string CaixaGameType,
    DateOnly? DrawDate,
    IReadOnlyList<string> MainNumbers,
    IReadOnlyList<string> SecondDrawNumbers,
    IReadOnlyList<string> DrawOrderNumbers,
    IReadOnlyList<string> Trevos,
    IReadOnlyList<PrizeTier> PrizeTiers,
    IReadOnlyList<WinnerCity> WinnerCities);

public sealed record PrizeTier(
    int Tier,
    string Description,
    int WinnersCount,
    decimal PrizeValue);

public sealed record WinnerCity(
    string City,
    string State,
    int WinnersCount);
