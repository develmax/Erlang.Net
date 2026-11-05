namespace Erlang;

internal static class NativeMagicLayout
{
    internal static int BinaryDataOffset => 4 * NativeWordLayout.Bytes;
    internal static int UnalignedDataOffset => 5 * NativeWordLayout.Bytes + 4 * sizeof(uint);
    internal static int AlignedDataOffset => (UnalignedDataOffset + sizeof(ulong) - 1) & -sizeof(ulong);
}
