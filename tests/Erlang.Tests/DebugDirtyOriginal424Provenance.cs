using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class DebugDirtyOriginal424Provenance
{
    private static void Require(bool value) => DebugDirtyOriginal424Access.Require(value);

    private static JsonDocument Read(string name) => JsonDocument.Parse(DebugDirtyOriginal424Access.Resource(name));

    private static string Hash(string name) => Convert.ToHexString(SHA256.HashData(DebugDirtyOriginal424Access.Resource(name)));

    internal static void Check()
    {
        using JsonDocument accepted = Read("original-completion.json"), failed = Read("original-failed-completion.json"), inputs = Read("passive-inputs.json");
        JsonElement proof = accepted.RootElement, old = failed.RootElement, bindings = inputs.RootElement;
        Require(proof.GetProperty("Passed").GetBoolean() && proof.GetProperty("PassiveOnly").GetBoolean()
            && !proof.GetProperty("CompilerOrVmRerun").GetBoolean() && proof.GetProperty("OriginalFailedCompletionPreserved").GetBoolean()
            && !proof.GetProperty("ExpectedRewritten").GetBoolean() && !proof.GetProperty("FullDebug20Claim").GetBoolean()
            && proof.GetProperty("RawFullStacksPreserved").GetBoolean() && !proof.GetProperty("OwnStateRawCrossVmEquality").GetBoolean());
        Require(proof.GetProperty("WholeRows").GetInt32() == 948 && proof.GetProperty("OwnStateRows").GetInt32() == 12
            && proof.GetProperty("ProviderFiles").GetInt32() == 6427 && proof.GetProperty("Errors").GetArrayLength() == 0);
        Require(proof.GetProperty("ManifestSha256").GetString() == Hash("passive-manifest.json")
            && proof.GetProperty("InputsSha256").GetString() == Hash("passive-inputs.json"));
        Require(!old.GetProperty("Passed").GetBoolean() && !old.GetProperty("ExpectedRewritten").GetBoolean()
            && old.GetProperty("WholeRows").GetInt32() == 0 && old.GetProperty("OwnStateRows").GetInt32() == 0
            && old.GetProperty("PinnedOtpCommit").GetString() == bindings.GetProperty("Pin").GetString());
        JsonElement errors = old.GetProperty("Errors");
        Require(errors.GetArrayLength() == 1
            && errors[0].GetProperty("Stage").GetString() == "verify-complete-raw-outcomes"
            && errors[0].GetProperty("Failure").GetString() == "ValueError('Container exceeds remaining term budget')");
        foreach (string stage in new[] { "compile-observer", "compile-facet", "original" })
            Require(old.GetProperty("Stages").GetProperty(stage).GetBoolean());
        Require(old.GetProperty("ClosureChecks").GetArrayLength() == 3);
        foreach (JsonElement row in old.GetProperty("ClosureChecks").EnumerateArray())
            Require(row.GetProperty("BeforeEqual").GetBoolean() && row.GetProperty("AfterEqual").GetBoolean()
                && row.GetProperty("Failure").ValueKind == JsonValueKind.Null);
        JsonElement files = bindings.GetProperty("Files");
        foreach (var file in new[]
        {
            (Name: "original-failed-completion.json", Suffix: "/completion.json"),
            (Name: "beam_debug_dirty424.beam", Suffix: "/observer-ebin/beam_debug_dirty424.beam"),
            (Name: "erts_debug.beam", Suffix: "/facet-ebin/erts_debug.beam"),
            (Name: "observer.erl", Suffix: "/source/beam_debug_dirty424.erl"),
            (Name: "facet.erl", Suffix: "/source/erts_debug.erl")
        })
        {
            JsonElement row = files.EnumerateArray().Single(item => item.GetProperty("Path").GetString()!.EndsWith(file.Suffix, StringComparison.Ordinal));
            Require(row.GetProperty("Sha256").GetString() == Hash(file.Name));
        }
        foreach (JsonElement budget in bindings.GetProperty("WireBudgets").EnumerateArray())
        {
            string name = budget.GetProperty("Name").GetString()!;
            byte[] raw = DebugDirtyOriginal424Access.Resource(name);
            Require(name is "values.etf" or "stacks.etf" or "own-state.etf");
            Require(raw.Length == budget.GetProperty("Bytes").GetInt32() && raw.Length == budget.GetProperty("MaxBytes").GetInt32()
                && raw.Length == budget.GetProperty("MaxTerms").GetInt32() && budget.GetProperty("MaxDepth").GetInt32() == 128
                && !budget.GetProperty("Compressed").GetBoolean() && raw[0] == 131 && raw[1] != 80 && Hash(name) == budget.GetProperty("Sha256").GetString());
        }
    }
}
