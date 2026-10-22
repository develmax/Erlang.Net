-module(chain411_held).
-on_load(initialize/0).
-export([run/0]).
initialize() ->
    chain411_observer ! {callback_held, self()},
    receive release -> ok end.
run() -> held_ready.
