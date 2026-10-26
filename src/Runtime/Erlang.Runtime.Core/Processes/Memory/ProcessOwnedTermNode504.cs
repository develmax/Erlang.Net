namespace Erlang;

internal sealed class ProcessOwnedTermNode504(
    ProcessOwnedTermKind504 kind,
    nint pointer,
    int length,
    long bytes
)
{
    internal ProcessOwnedTermKind504 Kind { get; } = kind;
    internal nint Pointer { get; } = pointer;
    internal int Length { get; } = length;
    internal long Bytes { get; } = bytes;
}
