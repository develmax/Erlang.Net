-module(module_observer690).
-export([run/2,capture/2,worker/3]).
run(Module, Path) ->
    Load = outcome(fun() -> Module:load(Path) end),
    First = outcome(fun() -> Module:identity() end),
    Second = outcome(fun() -> Module:second({payload,[17,<<1,2,3>>]}) end),
    {Load,First,Second}.
outcome(Fun) ->
    try Fun() of Value -> {value,Value}
    catch Class:Reason:Stack -> {raised,Class,Reason,Stack} end.

% Same actual compiled MFA worker on original OTP and managed PreparedBeam replay.
capture(Module, Path) ->
    Parent=self(), Ref=make_ref(),
    {Pid,Monitor}=spawn_monitor(?MODULE,worker,[Parent,Ref,{Module,Path}]),
    try
        Result=receive {Ref,Pid,Value}->Value
            after 30000->erlang:error(module690_worker_timeout) end,
        receive
            {'DOWN',Monitor,process,Pid,normal}->Result;
            {'DOWN',Monitor,process,Pid,Reason}->erlang:error({module690_worker_failed,Reason})
        after 30000->erlang:error(module690_down_timeout)
        end
    after
        erlang:demonitor(Monitor,[flush]),
        case is_process_alive(Pid) of true->exit(Pid,kill);false->ok end
    end.
worker(Parent, Ref, {Module,Path}) ->
    Result=run(Module,Path),
    Parent ! {Ref,self(),Result},
    ok.
