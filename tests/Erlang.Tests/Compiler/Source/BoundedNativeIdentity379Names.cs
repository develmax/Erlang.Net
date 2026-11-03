using System.Reflection;

namespace Erlang.Tests;

internal static class BoundedNativeIdentity379Names
{
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string TestModule = "bounded_native_identity379";
    internal const string Run = "run";
    internal const string Key = "bounded379_payload";
    internal const string Data = "data";
    internal const string Gate = "gate";
    internal const string Storage = "OwnedStorage506";
    internal const string PortableType = "Erlang.PortableControlTerm826";
    internal const string Capture = "Capture";
    internal const string Failure = "Bounded379 genuine guest payload alias or public detached outcome differs.";
    internal const string Source = """
        -module(bounded_native_identity379).
        -export([run/0]).
        run() ->
            {_,_,{H,T,P},_}=bounded_bif_contract:run(),
            put(bounded379_payload,{H,T,P}),
            {erts_debug:same(H,P),erts_debug:same(T,P)}.
        """;
}
