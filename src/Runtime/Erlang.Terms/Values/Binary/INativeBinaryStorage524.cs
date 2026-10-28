namespace Erlang;

internal interface INativeBinaryStorage524
{
    object ResourceIdentity524 { get; }
    nint Pointer524 { get; }
    int DataLength524 { get; }
    long AllocatedBytes524 { get; }
    long ReferencedBytes524 { get; }
    ReadOnlySpan<byte> Bytes524 { get; }

    IDisposable RetainRoot524();
}
