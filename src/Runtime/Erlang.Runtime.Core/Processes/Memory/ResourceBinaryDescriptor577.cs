namespace Erlang;

// Safety/lifetime bindings, not semantic offset/length mirrors. Every getter
// reads current actual SubBits words and validates its linked BinRef first.
internal sealed class ResourceBinaryDescriptor577(
    ProcessTermHeap506 owner,
    ProcessTermSequence506 block,
    NativeOffheapHeader576 binRef,
    BinaryBacking backing,
    nint data,
    int bytes
)
{
    internal ProcessTermHeap506 Owner { get; } = owner;
    internal ProcessTermSequence506 Block { get; } = block;
    internal NativeOffheapHeader576 BinRef { get; } = binRef;
    internal BinaryBacking Backing { get; } = backing;
    private readonly nint resourceData = data;
    internal nint Data => BinRef.OrdinaryBinary is INativeBinaryStorage524 ordinary ? ordinary.Pointer524 : resourceData;
    private readonly int resourceBytes = bytes;
    internal int Bytes => BinRef.OrdinaryBinary is INativeBinaryStorage524 ordinary ? ordinary.DataLength524 : resourceBytes;
    internal bool Published { get; set; }
}
