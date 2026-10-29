namespace Erlang.Tests;

internal static class OrdinaryOpaque682Names
{
    internal const string RegisteredCase = "runtime/counters682/ordinary-source-native-ref-escapes-registered-host-callback";
    internal const string FunctionCase = "runtime/counters682/ordinary-source-native-ref-escapes-public-opaque-function";
    internal const string CaptureCase = "runtime/counters682/ordinary-source-function-environment-exports-native-ref-to-host";
    internal const string CaptureMode = "capture_function";
    internal const string Module = "ordinary_opaque682";
    internal const string Start = "start";
    internal const string HostModule = "opaque_host682";
    internal const string Store = "store";
    internal const string RegisteredMode = "registered";
    internal const string NativeModule = "erts_internal";
    internal const string Create = "counters_new";
    internal const string Get = "counters_get";
    internal const string Add = "counters_add";
    internal const string Ready = "ready";
    internal const string Done = "done";
    internal const string Go = "go";
    internal const string Finish = "finish";
    internal const string Ok = "ok";
    internal const string Down = "DOWN";
    internal const string Normal = "normal";
    internal const string Profile = "OwnedTermsPilot506";
    internal const string Input = "VmSpawnInput641";
    internal const string Processes = "processes";
    internal const string Unwound = "Unwound";
    internal const string Host = "CountersHost596";
    internal const string OwnedStorage = "OwnedStorage562";
    internal const string Backing = "CountersBacking596";
    internal const string ArrayField = "Array";
    internal const string Retired = "Retired596";
    internal const string Failure = "Ordinary source callback exposed a temporary native header to enduring Host code.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const string Source = """
-module(ordinary_opaque682).
-export([start/3,worker/3]).
start(Parent,Ref,Mode) -> spawn_monitor(ordinary_opaque682,worker,[Parent,Ref,Mode]).
worker(Parent,Ref,Mode) ->
    Parent ! {ready,self()},
    receive go -> ok end,
    Outcome=try
        case Mode of
            registered -> opaque_host682:store(Ref);
            capture_function -> opaque_host682:store(fun() -> Ref end);
            Fun -> Fun(Ref)
        end
    catch C:R:S -> {raised,C,R,S}
    end,
    Parent ! {done,self(),Outcome},
    receive finish -> ok end.
""";
}
