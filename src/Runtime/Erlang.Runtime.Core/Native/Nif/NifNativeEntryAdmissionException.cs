namespace Erlang;

internal sealed class NifNativeEntryAdmissionException(
    string diagnostic,
    NifNativeLoadReason publicReason
) : NotSupportedException(diagnostic)
{
    public NifNativeLoadReason PublicReason { get; } = publicReason;
}
