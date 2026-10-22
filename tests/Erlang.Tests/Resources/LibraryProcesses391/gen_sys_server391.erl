-module(gen_sys_server391).
-export([start/2, init_it/6, loop/3, system_continue/3, system_terminate/4,
         system_code_change/4, system_get_state/1, system_replace_state/2,
         format_status/2, format_event/3, debug_event/3]).

start(Owner, Mode) ->
    process_flag(trap_exit, true),
    register(gen_sys_target391, self()),
    Owner ! {server_ready, self()},
    loop(Owner, [], {Mode, 0}).

init_it(Starter, Parent0, Name, _Module, Misc, _Options) ->
    process_flag(trap_exit, true),
    case Name of
        Pid when is_pid(Pid) -> register(gen_sys_target391, self());
        {local, gen_sys_target391} -> ok
    end,
    Parent = case Parent0 of self -> self(); _ -> Parent0 end,
    proc_lib:init_ack(Starter, {ok, self()}),
    loop(Parent, [], Misc).

loop(Parent, Debug, {Mode, State} = Misc) ->
    receive
        {system, From, Request} ->
            sys:handle_system_msg(Request, From, Parent, ?MODULE, Debug, Misc);
        {'$gen_call', From, get} ->
            NextDebug = sys:handle_debug(Debug, fun ?MODULE:format_event/3, State, {in, get}),
            gen:reply(From, State),
            loop(Parent, NextDebug, Misc);
        {'$gen_call', From, {add, Increment}} ->
            NextDebug = sys:handle_debug(Debug, fun ?MODULE:format_event/3, State, {in, add}),
            gen:reply(From, {old, State}),
            loop(Parent, NextDebug, {Mode, State + Increment});
        {'$gen_call', _From, no_reply} ->
            loop(Parent, Debug, Misc);
        {'EXIT', Parent, Reason} ->
            exit(Reason);
        stop ->
            ok
    end.

system_continue(Parent, Debug, Misc) ->
    loop(Parent, Debug, Misc).

system_terminate(Reason, _Parent, _Debug, _Misc) ->
    exit(Reason).

system_code_change(_Misc, _Module, _Version, crash) ->
    erlang:error(code_change_failure);
system_code_change({Mode, State}, _Module, _Version, Extra) ->
    {ok, {Mode, State + Extra}}.

system_get_state({bad_get, _State}) -> invalid_return;
system_get_state({crash_get, _State}) -> erlang:error(callback_failure);
system_get_state({_Mode, State}) -> {ok, State}.

system_replace_state(Fun, {Mode, State}) ->
    New = Fun(State),
    {ok, New, {Mode, New}}.

format_status(_Option, [_Dictionary, SystemState, _Parent, _Debug, Misc]) ->
    [{system_state, SystemState}, {misc, Misc}].

format_event(_Device, _Event, _State) -> ok.

debug_event(Count, _Event, _State) -> Count + 1.
