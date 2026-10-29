using System.Reflection;

namespace Erlang.Tests;

internal static class MixedNif686Names
{
    internal const string Case = "runtime/nif686/genuine-source-independent-ordinary-env-counter-host-export";
    internal const string Module = "mixed_source686";
    internal const string ProbeModule = "mixed_probe686";
    internal const string Store = "store";
    internal const string Pop = "pop";
    internal const string Start = "start";
    internal const string NativeModule = "erts_internal";
    internal const string Create = "counters_new";
    internal const string Get = "counters_get";
    internal const string Ready = "ready";
    internal const string Done = "done";
    internal const string Go = "go";
    internal const string Finish = "finish";
    internal const string Down = "DOWN";
    internal const string Normal = "normal";
    internal const string Ok = "ok";
    internal const string Profile = "OwnedTermsPilot506";
    internal const string Capability = "OwnedExecutionEnabled683";
    internal const string UsesOwned = "UsesOwnedExecution683";
    internal const string Host = "CountersHost596";
    internal const string Backing = "CountersBacking596";
    internal const string ArrayField = "Array";
    internal const string Retired = "Retired596";
    internal const string OwnedStorage = "OwnedStorage562";
    internal const string EnvironmentType = "Erlang.NifNativeCallEnvironment";
    internal const string CallbackType = "Erlang.NifNativeEnvironmentCallbacks";
    internal const string FailureType = "Erlang.NifNativeFailureState";
    internal const string ScopeType = "Erlang.NifNativeCallbackScope";
    internal const string AllocationsType = "Erlang.NifNativeAllocationOwner";
    internal const string Attach = "AttachGuestRoots516";
    internal const string Pointer = "Pointer";
    internal const string Add = "Add";
    internal const string Copy = "Copy";
    internal const string Export = "ExportResult516";
    internal const string Clear = "ClearIndependent";
    internal const string ThrowPending = "ThrowPending";
    internal const string Failure = "Mixed authentic NIF environments lost the ordinary Host counter carrier.";
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const string Source = """
-module(mixed_source686).
-export([start/2,worker/2]).
start(Parent,Ref) -> spawn_monitor(mixed_source686,worker,[Parent,Ref]).
worker(Parent,Ref) ->
    Parent ! {ready,self()},
    receive go -> ok end,
    Outcome=try mixed_probe686:store(Ref)
    catch C:R:S -> {raised,C,R,S}
    end,
    Parent ! {done,self(),Outcome},
    receive finish -> ok end.
""";
}
