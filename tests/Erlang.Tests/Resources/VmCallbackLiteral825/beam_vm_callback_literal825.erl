-module(beam_vm_callback_literal825).
-export([literal/0,run/0]).
-compile({no_inline,[literal/0]}).

literal() -> {retained,#{a=>b}}.

run() ->
    Parent = self(),
    % External MFA plus no_inline keeps V as a genuine captured environment value.
    V = ?MODULE:literal(),
    {Child, Monitor} = spawn_monitor(fun() -> Parent ! {done,V} end),
    Result = receive
        {done, Value} -> Value;
        {'DOWN', Monitor, process, Child, Reason} ->
            erlang:error({child_down_without_report,Reason})
    after 5000 ->
        erlang:error(callback_report_missing)
    end,
    receive
        {'DOWN', Monitor, process, Child, normal} -> Result;
        {'DOWN', Monitor, process, Child, Reason2} ->
            erlang:error({reported_but_abnormal_child,Reason2})
    after 5000 ->
        erlang:error(callback_down_missing)
    end.
