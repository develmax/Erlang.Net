namespace MfaContractVerification;

internal sealed record ContractFixture(
    string Id,
    string SourcePath,
    string SourceSha256,
    string DeclaredSourceFile,
    string Module,
    string EntryPoint,
    string[] Arguments,
    bool TrapExits,
    string CompilerPolicy,
    string? ExpectedProtocolPath,
    string? ExpectedProtocolSha256,
    string[] EffectKeys,
    int DeadlineSeconds
);
