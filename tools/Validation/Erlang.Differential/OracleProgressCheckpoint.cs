using System.Text.Json;

namespace Erlang.Differential;

internal static class OracleProgressCheckpoint
{
    public static async Task Write(
        string reportPath,
        string source,
        OracleAbortEvidence evidence,
        int completedComparisons
    )
    {
        string path = Path.ChangeExtension(reportPath, OracleProgressNames.Extension);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + OracleProgressNames.TemporarySuffix;
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(new OracleProgressEvidence(
            source,
            evidence,
            completedComparisons
        )));
        File.Move(temporary, path, true);
    }
}
