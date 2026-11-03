namespace Erlang;

internal sealed unsafe partial class NativeResourcePayload524
{
    internal NativeBinaryInfoRow822 ReadBinaryInfo822(nint expectedBinary)
    {
        lock (gate)
        {
            RequireLive524();
            if (allocation == 0 || allocation != expectedBinary)
                throw new InvalidOperationException(NativeBinaryInfo822Names.Invalid);
            var header = (NativeResourceMagicHeader527*)allocation;
            if (header->Flags != NativeResourceMagic527.Magic || header->RefCount <= 0)
                throw new InvalidOperationException(NativeBinaryInfo822Names.Invalid);

            return new(
                checked((ulong)(nuint)allocation),
                checked((ulong)header->OriginalSize),
                header->RefCount
            );
        }
    }
}
