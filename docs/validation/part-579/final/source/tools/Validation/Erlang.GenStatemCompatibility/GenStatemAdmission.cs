using System.Security.Cryptography;
using System.Text.Json;
using Erlang;

internal static class GenStatemAdmission
{
    internal static void Verify(string repository, string directory, string stdlib)
    {
        using JsonDocument provenance = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(directory, "provenance.json")));
        JsonElement root = provenance.RootElement;
        Require(root.GetProperty("Baseline").GetString() == "OTP-29.1.1"
            && root.GetProperty("Commit").GetString() == "ad05823719d77c8faee87348ea39513d4e2f99c5");

        void Check(string property, string path) => Require(Hash(path) == root.GetProperty(property).GetString());
        Check(
            "ObserverSourceSha256",
            Path.Combine(repository, "tools/Validation/Erlang.GenStatemCompatibility/gen_statem_port_cases.erl")
        );
        Check("ObserverBeamSha256", Path.Combine(directory, "gen_statem_port_cases.beam"));
        Check(
            "CompanionSourceSha256",
            Path.Combine(repository, "tools/Validation/Erlang.GenStatemCompatibility/gen_statem_port_second.erl")
        );
        Check("CompanionBeamSha256", Path.Combine(directory, "gen_statem_port_second.beam"));
        Check("CorpusSha256", Path.Combine(directory, "original.etf"));
        Check("EffectsSha256", Path.Combine(directory, "effects.etf"));
        Check("ExportsSha256", Path.Combine(directory, "exports.etf"));
        Check("FeatureBootstrapSha256", Path.Combine(directory, "feature-bootstrap.etf"));
        JsonElement original = root.GetProperty("Inputs").EnumerateArray().Single(entry => entry.GetProperty("Path").GetString()!.EndsWith("/ebin/gen_statem.beam", StringComparison.Ordinal));
        Require(Hash(Path.Combine(stdlib, "gen_statem.beam")) == original.GetProperty("Sha256").GetString());
        Term declared = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, "exports.etf")));
        Require(Cons.Items(declared).Cast<TupleTerm>().Select(row => (((Atom)row.Items[0]).Name, (int)((Integer)row.Items[1]).Value)).ToHashSet().SetEquals(GenStatemModule.Exports));
    }

    internal static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Require(bool value)
    {
        if (!value)
        {
            throw new InvalidDataException(GenStatemValidationDiagnostics.AdmissionFailed);
        }
    }
}
