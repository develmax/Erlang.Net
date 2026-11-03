namespace Erlang;

internal sealed unsafe partial class NativeResourcePayload524
{
    // Caller already retained an actual root. This is the real allocation's
    // magic-binary address, not the userdata address or a managed object handle.
    internal nint MagicBinaryPointer576()
    {
        lock (gate)
        {
            RequireLive524();

            return allocation;
        }
    }

    internal IReadOnlyList<uint> ReadMagicNumbers576(nint expectedBinary)
    {
        lock (gate)
        {
            RequireLive524();
            if (allocation == 0 || allocation != expectedBinary)
                throw new InvalidOperationException(MagicReference576Names.Invalid);
            var header = (NativeResourceMagicHeader527*)allocation;
            if (header->Flags != NativeResourceMagic527.Magic || header->RefCount <= 0
                || (header->Ref1 & ReferenceNumberLayout.MagicMarker) == 0)
                throw new InvalidOperationException(MagicReference576Names.Invalid);

            return Array.AsReadOnly(new[] { header->Ref0, header->Ref1, header->Ref2 });
        }
    }
}
