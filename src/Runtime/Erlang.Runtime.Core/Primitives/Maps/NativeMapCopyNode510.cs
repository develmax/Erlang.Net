namespace Erlang;

internal sealed class NativeMapCopyNode510(HamtViewNode source)
{
    internal HamtViewNode Source { get; } = source;
    internal int Row { get; set; } = -1;
    internal int Next { get; set; }
    internal List<NativeMapCopyNode510> Children { get; } = new();
}
