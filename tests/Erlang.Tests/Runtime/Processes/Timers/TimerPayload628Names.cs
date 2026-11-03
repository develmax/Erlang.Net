namespace Erlang.Tests;

internal static class TimerPayload628Names
{
    internal const string NativeFamily = "native";
    internal const string AtomicsFamily = "atomics";
    internal const string CountersFamily = "counters";
    internal const string RegexFamily = "regex";
    internal const string BinaryFamily = "binary";
    internal const string Erlang = "erlang";
    internal const string SendAfter = "send_after";
    internal const string PublicAlias = "public_timer628";
    internal const string Opaque = "opaque";
    internal const string Functions = "functions";
    internal const string OpaqueCase = "runtime/timer628/public-same-delegate-alias-remains-opaque";
    internal const string CancelTimer = "cancel_timer";
    internal const string Module = "timer_payload628";
    internal const string File = "timer_payload628.erl";
    internal const string Arm = "arm";
    internal const string Take = "take";
    internal const string Stored = "stored";
    internal const string NativeMakeReference = "MakeNativeResource524";
    internal const string NativeRetired = "Retired524";
    internal const string MajorCollect = "CollectGeneration508";
    internal const string DictionaryData = "data";
    internal const string FireCase = "runtime/timer628/five-families-source-erase-gc-and-fire";
    internal const string CancelCase = "runtime/timer628/five-families-cancel-after-source-erase-and-gc";
    internal const string ExitCase = "runtime/timer628/five-families-owner-exit-pending-payload";
    internal const string VmCase = "runtime/timer628/five-families-vm-close-joins-payload-retirement";
    internal const string Failure = "Actual timer payload628 lost stable graph custody or deterministic retirement.";
    internal const int FireDelay = 250;
    internal const int PendingDelay = 60000;
    internal const int MagicWords = 3;
    internal const int SendAfterThree = 0;
    internal const int StartTimerThree = 1;
    internal const int SendAfterFour = 2;
    internal const int StartTimerFour = 3;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const string Source = """
        -module(timer_payload628).
        -export([arm/2,take/0,opaque/0]).
        arm(T,Kind) ->
          Msg={payload,[#{resource=>get(stored)}]},
          R=case Kind of
            0 -> erlang:send_after(T,self(),Msg);
            1 -> erlang:start_timer(T,self(),Msg);
            2 -> erlang:send_after(T,self(),Msg,[{abs,false}]);
            3 -> erlang:start_timer(T,self(),Msg,[{abs,false}])
          end,
          erase(stored),
          erlang:garbage_collect(),
          R.
        opaque() -> public_timer628:send_after(60000,self(),get(stored)).
        take() ->
          receive
            {timeout,_,P} -> unpack(P);
            P -> unpack(P)
          after 5000 -> erlang:error(missing_timer_payload)
          end.
        unpack({payload,[#{resource:=P}]}) -> true=is_reference(P),erts_debug:flat_size(P).
        """;
}
