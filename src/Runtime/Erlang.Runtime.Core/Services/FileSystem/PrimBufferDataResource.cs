namespace Erlang;

internal sealed class PrimBufferDataResource
{
    internal object Gate { get; } = new();
    internal Queue<BitString> Segments { get; } = new();
    internal int HeadOffset { get; set; }
    internal long Size { get; set; }
    internal int Locked;
}
