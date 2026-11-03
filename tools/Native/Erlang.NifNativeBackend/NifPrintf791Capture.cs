namespace Erlang.NifNativeBackend;

internal sealed class NifPrintf791Capture
{
    internal List<byte> Bytes { get; } = new();
    internal Exception? Failure { get; set; }
}
