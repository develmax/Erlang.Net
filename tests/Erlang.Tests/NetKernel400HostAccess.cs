namespace Erlang.Tests;

internal static class NetKernel400HostAccess
{
    internal static byte[] Resource(string name)
    {
        using Stream source = typeof(NetKernel400HostAccess).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(NetKernel400HostNames.Invariant);
        using var output = new MemoryStream();
        source.CopyTo(output);

        return output.ToArray();
    }
}
