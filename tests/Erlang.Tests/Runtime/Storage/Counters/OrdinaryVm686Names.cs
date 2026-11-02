namespace Erlang.Tests;

internal static class OrdinaryVm686Names
{
    internal const string PureCase = "runtime/genuine-vm686/pure-mfa-storage-guest-admission-and-retirement";
    internal const string ActiveCase = "runtime/genuine-vm686/active-native-mfa-vm-close-joins-real-heap";
    internal const string QueuedCase = "runtime/genuine-vm686/offline-claimed-native-input-vm-close-before-entry";
    internal const string HibernateCase = "runtime/genuine-vm686/native-counter-in-real-hibernate-arguments";
    internal const string ErrorCase = "runtime/genuine-vm686/native-error-reason-and-explicit-argument-stack";
    internal const string ThrowCase = "runtime/genuine-vm686/native-uncaught-throw-reason-after-source-heap-retirement";
    internal const string ExitCase = "runtime/genuine-vm686/native-exit-reason-after-source-heap-retirement";
    internal const string Module = "ordinary_vm686";
    internal const string SourceFile = "ordinary_vm686.erl";
    internal const string Probe = "ordinary_vm_probe686";
    internal const string Entered = "entered";
    internal const string Start = "start";
    internal const string StartOffline = "start_offline";
    internal const string Ready = "ready";
    internal const string Go = "go";
    internal const string Done = "done";
    internal const string Finish = "finish";
    internal const string Down = "DOWN";
    internal const string Normal = "normal";
    internal const string Ok = "ok";
    internal const string Native = "native686";
    internal const string NoCatch = "nocatch";
    internal const string Value = "value";
    internal const string Profile = "OwnedTermsPilot506";
    internal const string StorageEnabled = "VmStorageBackingEnabled";
    internal const string Enabled = "OwnedExecutionEnabled683";
    internal const string HeapField = "termHeap506";
    internal const string DictionaryFrame = "frame";
    internal const string Snapshot = "Snapshot";
    internal const string CapacityWords = "CapacityWords";
    internal const string RootFrames = "RootFrames";
    internal const string Owners = "ownedTermOwners506";
    internal const string OwnerGate = "ownedTermOwnersGate506";
    internal const string Input = "VmSpawnInput641";
    internal const string Prepared = "prepared";
    internal const string Graph = "Graph641";
    internal const string LiveGraph = "HasGraphReferences593";
    internal const string Processes = "processes";
    internal const string Unwound = "Unwound";
    internal const string Host = "CountersHost596";
    internal const string OwnedStorage = "OwnedStorage562";
    internal const string Backing = "CountersBacking596";
    internal const string ArrayField = "Array";
    internal const string Retired = "Retired596";
    internal const string NativeModule = "erts_internal";
    internal const string Create = "counters_new";
    internal const string Get = "counters_get";
    internal const string SystemFlag = "system_flag";
    internal const string SchedulersOnline = "schedulers_online";
    internal const string Failure = "Actual genuine VM686 lifecycle or native term transfer differs.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(1);
    internal const string Retirement = "OwnedTermRetirement506";
    internal const string ModePure = "pure";
    internal const string ModeHibernate = "hibernate_argument";
    internal const string ModeError = "error";
    internal const string ModeThrow = "throw";
    internal const string ModeExit = "exit";
    internal const string Source = """
-module(ordinary_vm686).
-export([start/3,start_offline/3,worker/3,resume/2]).
start(Parent,Ref,Mode) -> spawn_monitor(ordinary_vm686,worker,[Parent,Ref,Mode]).
start_offline(Parent,Ref,Mode) -> spawn_opt(ordinary_vm686,worker,[Parent,Ref,Mode],[monitor,{scheduler,2}]).
worker(Parent,Ref,Mode) ->
    ordinary_vm_probe686:entered(self()),
    Parent ! {ready,self()},
    receive go -> ok end,
    case Mode of
        pure -> finish686(Parent,ok);
        hibernate_argument ->
            self() ! wake686,
            erlang:hibernate(ordinary_vm686,resume,[Parent,Ref]);
        error -> erlang:error({native686,Ref},[Ref]);
        throw -> erlang:throw({native686,Ref});
        exit -> erlang:exit({native686,Ref})
    end.
resume(Parent,Ref) ->
    receive wake686 -> ok end,
    ok=erts_internal:counters_add(Ref,1,1),
    finish686(Parent,{value,erts_internal:counters_get(Ref,1)}).
finish686(Parent,Value) ->
    Parent ! {done,self(),Value},
    receive finish -> ok end.
""";
}
