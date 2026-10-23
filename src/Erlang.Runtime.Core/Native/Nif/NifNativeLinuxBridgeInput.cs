namespace Erlang;

internal sealed class NifNativeLinuxBridgeInput(FileStream file, string hash) : IDisposable
{
    internal FileStream File { get; } = file;
    internal string Hash { get; } = hash;
    internal byte[] Descriptor => NifNativeLinuxNamespaceApi.Descriptor(File);

    public void Dispose() => File.Dispose();
}
