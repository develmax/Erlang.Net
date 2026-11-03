namespace Erlang;

internal static class GcStatisticsImageImports
{
    private const string Auth = "auth";
    private const string CodePurger = "erts_code_purger";
    private const string Internal = "erts_internal";
    private const string Lists = "lists";
    private const string NetworkKernel = "net_kernel";
    private const string Zlib = "zlib";
    internal static readonly IReadOnlyList<string> Modules = Array.AsReadOnly<string>(
        [Auth, ErlangModuleNames.Module, CodePurger, Internal, Lists, NetworkKernel, Zlib]);
}
