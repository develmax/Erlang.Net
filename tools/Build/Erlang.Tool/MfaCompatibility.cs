using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

internal static class MfaCompatibility
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static MfaCompatibilityLedger Load(
        string docs,
        string tag,
        string commit,
        IReadOnlySet<string> registered
    )
    {
        MfaCompatibilityLedger ledger;
        try
        {
            ledger = JsonSerializer.Deserialize<MfaCompatibilityLedger>(File.ReadAllText(System.IO.Path.Combine(docs, MfaCompatibilityNames.File)), Options)
                ?? throw new InvalidOperationException(MfaCompatibilityNames.Invalid);
        }
        catch (JsonException error)
        {
            throw new InvalidOperationException(MfaCompatibilityNames.Invalid, error);
        }
        if (ledger.SchemaVersion != MfaCompatibilityNames.SchemaVersion || ledger.Tag != tag || ledger.Commit != commit || ledger.Entries is null || ledger.VerifiedSourceFiles is null)
            throw new InvalidOperationException(MfaCompatibilityNames.Invalid);
        var found = new HashSet<string>(StringComparer.Ordinal);
        var evidenceHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in ledger.Entries)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Module) || string.IsNullOrWhiteSpace(entry.Function) || entry.Arity < 0 ||
                !registered.Contains(entry.Key) || !found.Add(entry.Key) || string.IsNullOrWhiteSpace(entry.Scope) ||
                entry.Status is not (MfaCompatibilityNames.Verified or MfaCompatibilityNames.Pending or MfaCompatibilityNames.Different) ||
                entry.Evidence is null || entry.Limits is null || entry.Limits.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException(MfaCompatibilityNames.Invalid);
            foreach (var evidence in entry.Evidence)
                ValidateEvidence(docs, evidence, evidenceHashes);
            if (entry.Status == MfaCompatibilityNames.Verified && (entry.Limits.Length != 0 ||
                !entry.Evidence.Any(value => value.Kind == MfaCompatibilityNames.OriginalComparison) ||
                !entry.Evidence.Any(value => value.Kind == MfaCompatibilityNames.Audit)))
                throw new InvalidOperationException(MfaCompatibilityNames.Invalid);
            if (entry.Status == MfaCompatibilityNames.Pending && entry.Limits.Length == 0 ||
                entry.Status == MfaCompatibilityNames.Different && (entry.Limits.Length == 0 || entry.Evidence.Length == 0))
                throw new InvalidOperationException(MfaCompatibilityNames.Invalid);
        }
        if (ledger.Entries.Any(entry => entry.Status == MfaCompatibilityNames.Verified) && ledger.VerifiedSourceFiles.Length == 0)
            throw new InvalidOperationException(MfaCompatibilityNames.Invalid);
        string repository = Directory.GetParent(System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(docs)))!.FullName;
        var sourcePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in ledger.VerifiedSourceFiles)
        {
            if (source is null || source.Path is null || !source.Path.StartsWith("src/", StringComparison.Ordinal) || !sourcePaths.Add(source.Path))
                throw new InvalidOperationException(MfaCompatibilityNames.Invalid);
            ValidateEvidence(
                repository,
                new MfaCompatibilityEvidence
                {
                    Kind = MfaCompatibilityNames.SourceReview,
                    Path = source.Path,
                    Sha256 = source.Sha256,
                    Scope = MfaCompatibilityNames.SourceBindingScope
                },
                evidenceHashes
            );
        }
        if (!found.SetEquals(registered))
            throw new InvalidOperationException(MfaCompatibilityNames.Invalid);

        return ledger;
    }

    private static void ValidateEvidence(string docs, MfaCompatibilityEvidence evidence, Dictionary<string, string> hashes)
    {
        if (evidence is null || evidence.Kind is not (MfaCompatibilityNames.OriginalComparison or MfaCompatibilityNames.Audit or MfaCompatibilityNames.SourceReview or MfaCompatibilityNames.LocalValidation) ||
            string.IsNullOrWhiteSpace(evidence.Scope) || string.IsNullOrWhiteSpace(evidence.Path) ||
            System.IO.Path.IsPathRooted(evidence.Path) || evidence.Path.Split('/', '\\').Any(part => part is ".." or "." or "") ||
            evidence.Sha256 is null || evidence.Sha256.Length != MfaCompatibilityNames.Sha256Length || evidence.Sha256.Any(value => !char.IsAsciiHexDigit(value)))
            throw new InvalidOperationException(MfaCompatibilityNames.Invalid);
        string root = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(docs)) + System.IO.Path.DirectorySeparatorChar;
        string path = System.IO.Path.GetFullPath(System.IO.Path.Combine(docs, evidence.Path));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            throw new InvalidOperationException(MfaCompatibilityNames.Invalid);
        if (!hashes.TryGetValue(path, out string? actual))
            hashes.Add(path, actual = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))));
        if (!actual.Equals(evidence.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(MfaCompatibilityNames.Invalid);
    }
}
