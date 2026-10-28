namespace Erlang.Tests;

internal static class TrustedGuest507Names
{
    internal const string SharingTest = "runtime/trusted-guest507/source-dictionary-and-pure-read-aliases";
    internal const string AllocationTest = "runtime/trusted-guest507/dictionary-no-payload-copy-and-root-slot-reuse";
    internal const string OverrideTest = "runtime/trusted-guest507/public-erlang-override-stays-portable";
    internal const string Guest = "trusted_guest507";
    internal const string Target = "trusted_target507";
    internal const string Probe = "trusted_probe507";
    internal const string Run = "run";
    internal const string Allocation = "allocation";
    internal const string Override = "override";
    internal const string Words = "words";
    internal const string RootIds = "root_ids";
    internal const string Collect = "collect";
    internal const string ErLang = "erlang";
    internal const string Get = "get";
    internal const string Heap = "TermHeap506";
    internal const string Snapshot = "Snapshot";
    internal const string Payload = "LivePayloadWords";
    internal const string Frame = "frame";
    internal const string RootIdProperty = "IssuedRootSlotIds507";
    internal const string Storage = "OwnedStorage506";
    internal const string Failure = "Trusted guest507 source-only integration control failed.";
    internal const string Source = """
        -module(trusted_guest507).
        -export([run/1, allocation/1, override/1]).
        run(X) -> V={X,[X]}, undefined=put(k,V),
          A=trusted_target507:id(V), B=get(k), C=erase(k),
          L=[V], T={V}, H=hd(L), E=element(1,T),
          trusted_probe507:collect(),
          [erts_debug:same(V,A),erts_debug:same(V,B),erts_debug:same(V,C),
           erts_debug:same(V,H),erts_debug:same(V,E)].
        allocation(X) -> V={X,[X]}, put(anchor,V),
          W=trusted_probe507:words(),
          undefined=put(k,V), V=get(k), V=erase(k),
          W=trusted_probe507:words(),
          R=trusted_probe507:root_ids(),
          churn(64,V), R=trusted_probe507:root_ids(), erase(anchor), true.
        churn(0,V) -> V;
        churn(N,V) -> put(k,V), V=get(k), V=erase(k), churn(N-1,V).
        override(X) -> V={X,[X]}, erlang:get(V).
        """;
    internal const string TargetSource = """
        -module(trusted_target507).
        -export([id/1]).
        id(V) -> V.
        """;
}
