using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class DdllFormatterSameHost437
{

    internal static string ProfileIdentity()
        => Environment.GetEnvironmentVariable(DdllFormatterSameHost437Names.ProfileVariable) ?? DdllFormatterSameHost437Names.HistoricalProfile;

    internal static byte[] Expected(string file)
    {
        string? profile = Environment.GetEnvironmentVariable(DdllFormatterSameHost437Names.ProfileVariable);
        string? hash = Environment.GetEnvironmentVariable(DdllFormatterSameHost437Names.HashVariable);
        if (profile is null)
        {
            // Historical local cohort remains immutable; clean CI must opt into its
            // actual same-host native oracle rather than silently compare another OS.
            Require(hash is null && Environment.GetEnvironmentVariable(DdllFormatterSameHost437Names.GitHubActionsVariable) != DdllFormatterSameHost437Names.CiEnabledValue);

            return DdllErrorOriginal416Access.Resource(file);
        }
        Require(hash is not null && Path.IsPathFullyQualified(profile));
        byte[] proof = File.ReadAllBytes(profile);
        Require(Convert.ToHexString(SHA256.HashData(proof)) == hash);
        using JsonDocument document = JsonDocument.Parse(proof);
        JsonElement root = document.RootElement;
        Require(root.GetProperty(DdllFormatterSameHost437Names.FieldPassed).GetBoolean()
            && root.GetProperty(DdllFormatterSameHost437Names.FieldSettled).GetBoolean()
            && root.GetProperty(DdllFormatterSameHost437Names.FieldPin).GetString() == DdllFormatterSameHost437Names.Pin
            && root.GetProperty(DdllFormatterSameHost437Names.FieldWholeRows).GetInt32() == DdllErrorOriginal416Names.WholeRows
            && root.GetProperty(DdllFormatterSameHost437Names.FieldProviderFiles).GetInt32() == DdllFormatterSameHost437Names.ProviderFileCount
            && root.GetProperty(DdllFormatterSameHost437Names.FieldProviderBeforeEqual).GetBoolean()
            && root.GetProperty(DdllFormatterSameHost437Names.FieldProviderAfterEqual).GetBoolean()
            && !root.GetProperty(DdllFormatterSameHost437Names.FieldCompilerUsed).GetBoolean()
            && !root.GetProperty(DdllFormatterSameHost437Names.FieldProjection).GetBoolean()
            && !root.GetProperty(DdllFormatterSameHost437Names.FieldHistoricalGoldenRewritten).GetBoolean()
            && root.GetProperty(DdllFormatterSameHost437Names.FieldErrors).GetArrayLength() == 0
            && root.GetProperty(DdllFormatterSameHost437Names.FieldHost).GetString() == Environment.GetEnvironmentVariable(DdllFormatterSameHost437Names.ComputerNameVariable)
            && root.GetProperty(DdllFormatterSameHost437Names.FieldGitHubRunId).GetString() == Environment.GetEnvironmentVariable(DdllFormatterSameHost437Names.GitHubRunVariable));
        Require(root.GetProperty(DdllFormatterSameHost437Names.FieldWorkerSha256).GetString()
            == Convert.ToHexString(SHA256.HashData(DdllErrorOriginal416Access.Resource(DdllFormatterSameHost437Names.Worker))));
        string directory = Path.GetDirectoryName(Path.GetFullPath(profile))!;
        JsonElement files = root.GetProperty(DdllFormatterSameHost437Names.FieldFiles);
        // Authenticate every captured artifact, including real process settlement
        // and native stock-module authority, before consuming either full outcome.
        foreach (JsonProperty entry in files.EnumerateObject())
        {
            string full = Path.GetFullPath(Path.Combine(directory, entry.Name));
            Require(full.StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase));
            Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(full))) == entry.Value.GetString());
        }
        foreach (string required in new[] { DdllErrorOriginal416Names.Values, DdllErrorOriginal416Names.StackValues, DdllFormatterSameHost437Names.AuthorityFile, DdllFormatterSameHost437Names.SettlementFile, DdllFormatterSameHost437Names.CapturedWorker })
            Require(files.TryGetProperty(required, out _));
        using JsonDocument settlement = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, DdllFormatterSameHost437Names.SettlementFile)));
        JsonElement exit = settlement.RootElement;
        Require(exit.GetProperty(DdllFormatterSameHost437Names.FieldSettled).GetBoolean() && !exit.GetProperty(DdllFormatterSameHost437Names.FieldTimedOut).GetBoolean()
            && exit.GetProperty(DdllFormatterSameHost437Names.FieldExitCode).GetInt32() == 0 && exit.GetProperty(DdllFormatterSameHost437Names.FieldFailures).GetArrayLength() == 0);
        Require(file == DdllErrorOriginal416Names.Values || file == DdllErrorOriginal416Names.StackValues);

        return File.ReadAllBytes(Path.Combine(directory, file));
    }

    private static void Require(bool valid)
    {
        if (!valid) throw new InvalidDataException(DdllFormatterSameHost437Names.Failure);
    }
}
