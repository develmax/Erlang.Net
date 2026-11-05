namespace Erlang;

internal static class IoLibraryDependencies
{
    internal const string Formatter = "io_lib";
    internal const string Scanner = "erl_scan";
    internal const string Parser = "erl_parse";
    internal const string Network = "net_kernel";

    internal static readonly string[] Required =
        [Formatter, Scanner, Parser, UnicodeNativeNames.Module, Network];
}
