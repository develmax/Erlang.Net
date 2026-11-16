-module(beam_local_fun_probe).
-export([constant/0, captured/1]).

constant() -> fun Named() -> done end.
captured(Value) ->
    fun() -> Value end.
