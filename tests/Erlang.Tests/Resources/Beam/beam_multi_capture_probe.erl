-module(beam_multi_capture_probe).
-export([pair/3]).

pair(Zebra, _, Alpha) ->
    fun(Value) -> {Alpha, Value, Zebra, Alpha} end.
