-module(beam_spawn_tail_wrappers).
-export([start/1, linked/1, monitored/1, options/2, local/3]).
start(Fun) -> spawn(Fun).
linked(Fun) -> erlang:spawn_link(Fun).
monitored(Fun) -> spawn_monitor(Fun).
options(Fun, Options) -> spawn_opt(Fun, Options).
local(Module, Function, Args) -> spawn(Module, Function, Args).
