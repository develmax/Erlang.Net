namespace Erlang;

internal sealed unsafe class NifNativeBinaryNode(
    NifNativeBinaryStorage storage,
    nuint offset,
    nuint length
) : NifNativeTermNode
{
    internal NifNativeBinaryStorage Storage => storage;
    internal nuint Offset => offset;
    internal nuint Length => length;
    internal byte* Data => storage.Pointer + offset;
    internal override int ChildCount => 0;

    internal override NifNativeTermValue Child(int index) => throw new ArgumentOutOfRangeException(nameof(index));

    internal override Term Build(IReadOnlyDictionary<NifNativeTermNode, Term> cache)
    {
        if (length > NifNativeBinaryNames.MaximumTermBytes)
            throw new NotSupportedException(NifNativeBinaryNames.TermSizeBoundary);

        return storage.FreezeView(offset, length);
    }
}
