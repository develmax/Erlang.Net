using System.Text.Json;

internal static class ReadinessTestReportAdmission
{
    internal static T Load<T>(string path, JsonSerializerOptions options)
    {
        string json = File.ReadAllText(path);
        using var document = JsonDocument.Parse(json);
        foreach (var property in document.RootElement.EnumerateObject())
        {
            bool marker = property.Name.Equals(ReadinessTestReportNames.Partial, StringComparison.OrdinalIgnoreCase)
                || property.Name.Equals(ReadinessTestReportNames.Listed, StringComparison.OrdinalIgnoreCase);
            if (marker && property.Value.ValueKind != JsonValueKind.False)
                throw new InvalidOperationException(ReadinessTestReportNames.IncompleteReport);
        }

        return JsonSerializer.Deserialize<T>(json, options)!;
    }
}
