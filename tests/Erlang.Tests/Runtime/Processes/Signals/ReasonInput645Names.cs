namespace Erlang.Tests;

internal static class ReasonInput645Names
{
    internal const string NativeFamily = "native";
    internal const string AtomicsFamily = "atomics";
    internal const string CountersFamily = "counters";
    internal const string RegexFamily = "regex";
    internal const string BinaryFamily = "binary";
    internal const string Module = "reason_input645";
    internal const string FileName = "reason_input645.erl";
    internal const string Arm = "arm";
    internal const string Take = "take";
    internal const string Cancel = "cancel";
    internal const string Stored = "stored";
    internal const string NativeMakeReference = "MakeNativeResource524";
    internal const string NativeRetired = "Retired524";
    internal const string MajorCollect = "CollectGeneration508";
    internal const string DictionaryData = "data";
    internal const string Failure = "Source monitor641 lost stable tag/DOWN custody or bounded retirement.";
    internal const string DownCase = "runtime/monitor641/source-tag-and-independent-counters-reason-complete-down";
    internal const string CancelCase = "runtime/monitor641/source-tag-demonitor-after-source-erase-and-gc";
    internal const string ExitCase = "runtime/monitor641/source-tag-observer-exit-releases-entry";
    internal const string VmCase = "runtime/monitor641/source-tag-vm-close-joins-entry";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const string Drop = "drop";
    internal const string Closure = "closure";
    internal const string Mfa = "mfa";
    internal const string ClosureCase = "runtime/reason645/source-parent-captured-resource-closure-child-down-last-root";
    internal const string MfaCase = "runtime/reason645/source-parent-resource-mfa-child-down-last-root";
    internal const string Source = """
        -module(reason_input645).
        -export([arm/1,child/1,take/0,drop/0]).
        arm(closure) ->
          R=get(stored),Child=spawn(fun() -> child(R) end),arm_done(Child);
        arm(mfa) ->
          R=get(stored),Child=spawn(reason_input645,child,[R]),arm_done(Child).
        arm_done(Child) ->
          Ref=erlang:monitor(process,Child),put(child645,Child),put(ref645,Ref),
          erase(stored),erlang:garbage_collect(),Child.
        child(R) -> receive go -> exit({reason645,R}) end.
        take() ->
          get(child645)!go,
          receive
            {'DOWN',Ref,process,Child,{reason645,R}} ->
              Ref=erase(ref645),Child=erase(child645),
              true=is_reference(R),3=erts_debug:flat_size(R),put(held645,R),true;
            Unexpected -> erlang:error({unexpected_reason645,element(5,Unexpected)})
          after 5000 -> erlang:error(missing_reason645)
          end.
        drop() -> erase(stored),erase(held645),erase(child645),erase(ref645),erlang:garbage_collect(),true.
        """;
}
