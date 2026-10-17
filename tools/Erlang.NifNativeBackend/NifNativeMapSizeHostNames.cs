namespace Erlang.NifNativeBackend;

internal static class NifNativeMapSizeHostNames
{
    internal const string Module = "map_size_host_contract";
    internal const string Failure = "NIF map size host contract failed";
    internal const int Slot = 109;
    internal const nuint Before = 17;
    internal const nuint After = 29;
    internal static readonly int[] Sizes = [0, 1, 31, 32, 33, 512, 8193];
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
