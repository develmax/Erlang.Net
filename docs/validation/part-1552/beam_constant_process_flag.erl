-module(beam_constant_process_flag).
-compile({no_auto_import,[process_flag/2]}).
-export([remote/1,local/0]).
remote([]) ->
    erlang:process_flag(trap_exit, true),
    {ok,[]}.
local() ->
    process_flag(trap_exit, false),
    ok.
process_flag(Key, Value) -> put(Key, Value).
