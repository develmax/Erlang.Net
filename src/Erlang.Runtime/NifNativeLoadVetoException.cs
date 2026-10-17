namespace Erlang;

internal sealed class NifNativeLoadVetoException(int returnCode)
    : InvalidOperationException(NifNativeDiagnostics.LoadVeto)
{
    public int ReturnCode { get; } = returnCode;
    public NifNativeLoadReason PublicReason { get; } = new(
        NifNativeLoadReasonKind.LoadVeto,
        string.Empty,
        string.Empty,
        string.Empty,
        0,
        returnCode
    );
}
