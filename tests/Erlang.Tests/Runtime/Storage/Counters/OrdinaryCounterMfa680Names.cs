namespace Erlang.Tests;

internal static class OrdinaryCounterMfa680Names
{
    internal const string Case = "runtime/counters680/ordinary-source-mfa-keeps-real-argument-custody-until-child-unwinds";
    internal const string CaseHibernate = "runtime/counters680/ordinary-dictionary-counter-survives-real-hibernate-and-retires";
    internal const string Module = "ordinary_counter680";
    internal const string Start = "start";
    internal const string NativeModule = "erts_internal";
    internal const string Create = "counters_new";
    internal const string Ready = "ready";
    internal const string Done = "done";
    internal const string Go = "go";
    internal const string Finish = "finish";
    internal const string Value = "value";
    internal const string Down = "DOWN";
    internal const string Normal = "normal";
    internal const string Profile = "OwnedTermsPilot506";
    internal const string Input = "VmSpawnInput641";
    internal const string Processes = "processes";
    internal const string Unwound = "Unwound";
    internal const string Host = "CountersHost596";
    internal const string Backing = "CountersBacking596";
    internal const string ArrayField = "Array";
    internal const string Retired = "Retired596";
    internal const string Failure = "Ordinary real MFA counter argument lost physical custody.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const string Source = """
-module(ordinary_counter680).
-export([start/2,worker/2]).
start(Parent,Ref) -> spawn_monitor(ordinary_counter680,worker,[Parent,Ref]).
worker(Parent,Ref) ->
    Parent ! {ready,self()},
    receive go -> ok end,
    Outcome=try
        ok=erts_internal:counters_add(Ref,1,1),
        {value,erts_internal:counters_get(Ref,1)}
    catch C:R:S -> {raised,C,R,S}
    end,
    Parent ! {done,self(),Outcome},
    receive finish -> ok end.
""";
    internal const string SourceHibernate = """
-module(ordinary_counter680).
-export([start/2,worker/2,resume/1]).
start(Parent,Ref) -> spawn_monitor(ordinary_counter680,worker,[Parent,Ref]).
worker(Parent,Ref) ->
    put(counter680,Ref),
    Parent ! {ready,self()},
    receive go -> ok end,
    self() ! wake680,
    erlang:hibernate(ordinary_counter680,resume,[Parent]).
resume(Parent) ->
    receive wake680 -> ok end,
    Ref=erase(counter680),
    Outcome=try
        ok=erts_internal:counters_add(Ref,1,1),
        {value,erts_internal:counters_get(Ref,1)}
    catch C:R:S -> {raised,C,R,S}
    end,
    Parent ! {done,self(),Outcome},
    receive finish -> ok end.
""";
}
