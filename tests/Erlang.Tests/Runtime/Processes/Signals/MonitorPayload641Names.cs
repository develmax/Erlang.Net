namespace Erlang.Tests;

internal static class MonitorPayload641Names
{
    internal const string NativeFamily = "native";
    internal const string AtomicsFamily = "atomics";
    internal const string CountersFamily = "counters";
    internal const string RegexFamily = "regex";
    internal const string BinaryFamily = "binary";
    internal const string Module = "monitor_payload641";
    internal const string File = "monitor_payload641.erl";
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
    internal const string Source = """
        -module(monitor_payload641).
        -export([arm/0,take/0,cancel/0]).
        arm() ->
          Child=spawn(fun() -> receive go -> R=erts_internal:counters_new(1),exit({reason,R}) end end),
          Tag={payload,[#{resource=>get(stored)}]},
          Ref=erlang:monitor(process,Child,[{tag,Tag}]),
          put(child641,Child),put(ref641,Ref),
          erase(stored),erlang:garbage_collect(),Ref.
        take() ->
          get(child641)!go,
          receive
            {{payload,[#{resource:=P}]},Ref,process,Child,{reason,R}} ->
              Ref=erase(ref641),Child=erase(child641),
              true=is_reference(P),true=is_reference(R),
              3=erts_debug:flat_size(P),3=erts_debug:flat_size(R),true;
            Unexpected -> erlang:error({unexpected_down641,element(5,Unexpected)})
          after 5000 -> erlang:error(missing_down641)
          end.
        cancel() ->
          Ref=erase(ref641),Child=erase(child641),
          true=erlang:demonitor(Ref,[flush]),exit(Child,kill),true.
        """;
}
