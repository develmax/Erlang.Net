using System.Security.Cryptography;
using System.Text;

namespace Erlang.Differential;

public static class OriginalOtpDefects
{
    private const string LocalFunctionEncoding = "OTP-29.1.1 external.c recursively emits LOCAL_EXT for function metadata without corresponding size/hash accounting. Part130 preserves native badarg and process exit134; managed roundtrip remains a mandatory test.";

    public static string? Reason(CompiledModuleCase fixture) => fixture.Name switch
    {
        "erlang-etf-roundtrip-136" or
        "erlang-etf-roundtrip-137" or
        "erlang-etf-roundtrip-141" or
        "erlang-etf-roundtrip-142" or
        "erlang-etf-local-iovec" => LocalFunctionEncoding,
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
            OriginalEvidence = "docs/validation/part-130"
        }).ToArray();
}
