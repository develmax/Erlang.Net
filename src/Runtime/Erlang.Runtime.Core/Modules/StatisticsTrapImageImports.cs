namespace Erlang;

// Exact external module vector of the authenticated full pinned image. This
// declares registry resolution during whole-image validation; it does not
// publish exports or widen the four admitted statistics entry signatures.
internal static class StatisticsTrapImageImports
{
    private const string Erlang = "erlang";
    private const string ErrorLogger = "error_logger";
    private const string ErtsDebug = "erts_debug";
    private const string ErtsInternal = "erts_internal";
    private const string LiteralAreaCollector = "erts_literal_area_collector";
    private const string Ets = "ets";
    private const string KernelReferenceCounts = "kernel_refc";
    private const string PersistentTerms = "persistent_term";
    internal static readonly IReadOnlyList<string> Modules = Array.AsReadOnly<string>(
        [Erlang, ErrorLogger, ErtsDebug, ErtsInternal, LiteralAreaCollector, Ets, KernelReferenceCounts, PersistentTerms]);
}
