using System.Security.Cryptography;
using System.Text;

namespace Erlang.Differential;

public static class OriginalOtpDefects
{
    private const string ZeroWordReferenceFixture = "erlang-etf-wire-identity-invalid-162";
    private const string LocalFunctionEvidence = "docs/validation/part-130";
    private const string ZeroWordReferenceEvidence = "docs/validation/part-132";
    private const string LocalFunctionEncoding = "OTP-29.1.1 external.c recursively emits LOCAL_EXT for function metadata without corresponding size/hash accounting. Part130 preserves native badarg and process exit134; managed roundtrip remains a mandatory test.";
    private const string ZeroWordReference = "OTP-29.1.1 external.c permits ref_words0 in decoded_size but unconditionally reads the first reference word beyond this packet. Part132 preserves successful native admission; mandatory managed badarg protects the declared packet boundary.";

    public static string? Reason(CompiledModuleCase fixture) => fixture.Name switch
    {
        "erlang-etf-roundtrip-136" or
        "erlang-etf-roundtrip-137" or
        "erlang-etf-roundtrip-141" or
        "erlang-etf-roundtrip-142" or
        "erlang-etf-local-iovec" => LocalFunctionEncoding,
        ZeroWordReferenceFixture => ZeroWordReference,
        _ => null
    };

    public static IReadOnlyList<CompiledModuleCase> Comparable { get; } =
        CompiledModuleCases.All.Where(fixture => Reason(fixture) is null).ToArray();

    public static object[] Exclusions { get; } = CompiledModuleCases.All
        .Where(fixture => Reason(fixture) is not null)
        .Select(fixture => (object)new
        {
            fixture.Name,
            fixture.Source,
            SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fixture.Source))),
            ManagedExpected = fixture.Expected.ToString(),
            Reason = Reason(fixture),
            NativeExecuted = false,
            OriginalEvidence = fixture.Name == ZeroWordReferenceFixture ? ZeroWordReferenceEvidence : LocalFunctionEvidence
        }).ToArray();
}
