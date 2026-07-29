using System.Text.Json;

namespace LotoAnalytics.Api.Features.Contests;

public static class CaixaLotteryResultJsonSanitizer
{
    // Remove caracteres NUL recursivamente para permitir persistencia em jsonb no PostgreSQL.
    public static string Sanitize(string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        var sanitized = SanitizeElement(document.RootElement);

        return JsonSerializer.Serialize(sanitized);
    }

    // Converte um elemento JSON para objetos CLR sanitizados.
    private static object? SanitizeElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element
                .EnumerateObject()
                .ToDictionary(property => property.Name, property => SanitizeElement(property.Value), StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray().Select(SanitizeElement).ToArray(),
            JsonValueKind.String => element.GetString()?.Replace("\u0000", string.Empty, StringComparison.Ordinal),
            JsonValueKind.Number => ReadNumber(element),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    // Le numeros preservando inteiros quando possivel e decimais nos demais casos.
    private static object ReadNumber(JsonElement element)
    {
        if (element.TryGetInt64(out var longValue))
        {
            return longValue;
        }

        if (element.TryGetDecimal(out var decimalValue))
        {
            return decimalValue;
        }

        return element.GetDouble();
    }
}
