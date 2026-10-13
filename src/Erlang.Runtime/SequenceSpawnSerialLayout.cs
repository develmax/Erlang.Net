namespace Erlang;

internal static class SequenceSpawnSerialLayout
{
    public const int SmallTagBits = 4;
    public const ulong SmallTag = 0xF;

    public static ulong TaggedWord(long value) => unchecked(((ulong)value << SmallTagBits) | SmallTag);
}
