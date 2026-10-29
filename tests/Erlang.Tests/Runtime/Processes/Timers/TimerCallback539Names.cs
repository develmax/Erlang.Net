using System.Reflection;

namespace Erlang.Tests;

internal static class TimerCallback539Names
{
    internal const string Cancel = "runtime/timer539/actual-fire-claim-before-cancel";
    internal const string Close = "runtime/timer539/actual-vm-close-during-publication";
    internal const string PhaseType = "Erlang.RuntimeTimerCallbackPhase539";
    internal const string Timers = "Timers";
    internal const string Attach = "AttachCallbackPhase539";
    internal const string Claimed = "Claimed539";
    internal const string Exited = "Exited539";
    internal const string Claims = "Claims539";
    internal const string Release = "Release539";
    internal const string Module = "timer_callback539";
    internal const string File = "timer_callback539.erl";
    internal const string Arm = "arm";
    internal const string Take = "take";
    internal const string Empty = "empty";
    internal const string EmptyAtom = "empty539";
    internal const string Stored = "stored539";
    internal const string DictionaryData = "data";
    internal const string ErlangModule = "erlang";
    internal const string CancelTimer = "cancel_timer";
    internal const string False = "false";
    internal const string Failure = "Actual timer callback539 phase or payload retirement failed.";
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(15);
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    internal const string Source = """
        -module(timer_callback539).
        -export([arm/1,take/1,empty/0]).
        arm(Kind) ->
            Msg={payload539,[#{resource=>get(stored539)}]},
            T=case Kind of
                0 -> erlang:send_after(0,self(),Msg);
                1 -> erlang:start_timer(0,self(),Msg,[{abs,false}])
            end,
            erase(stored539),
            erlang:garbage_collect(),
            T.
        take(T) ->
            receive
                {timeout,T,{payload539,[#{resource:=P}]}} -> true=is_reference(P),erts_debug:flat_size(P);
                {payload539,[#{resource:=P}]} -> true=is_reference(P),erts_debug:flat_size(P)
            after 5000 -> erlang:error(missing_callback539)
            end.
        empty() -> receive _ -> unexpected539 after 0 -> empty539 end.
        """;
}
