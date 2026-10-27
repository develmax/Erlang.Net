namespace Erlang.Tests;

internal static class BeamGuestReturn507Names
{
    internal const string Test = "runtime/beam-owned507/trusted-return-alias-and-portable-host-boundaries";
    internal const string Resource = "Erlang.Tests.LibraryDependencies392.context_forwarding_observer397.beam";
    internal const string OriginalSha256 = "82981A64B3CE6731B9A32EF83EA3240035792B4C8B5C7123D85DA61A1E6BBCFA";
    internal const string OriginalModule = "context_forwarding_observer397";
    internal const string Identity = "identity";
    internal const string Guest = "beam_return_guest507";
    internal const string Probe = "beam_return_probe507";
    internal const string Collect = "collect";
    internal const string Opaque = "opaque";
    internal const string Run = "run";
    internal const string HostRoute = "host_route";
    internal const string ErLang = "erlang";
    internal const string Storage = "OwnedStorage506";
    internal const string Heap = "TermHeap506";
    internal const string HeapCollect = "Collect";
    internal const string Failure = "BEAM trusted return507 boundary contract failed.";
    internal const string Source = """
        -module(beam_return_guest507).
        -export([run/1, host_route/1]).
        run(X) -> V={X,[X]}, A=context_forwarding_observer397:identity(V),
          true=erts_debug:same(V,A), beam_return_probe507:collect(), A.
        host_route(X) -> V={X,[X]}, A=beam_return_probe507:opaque(V),
          erts_debug:same(V,A).
        """;
}
