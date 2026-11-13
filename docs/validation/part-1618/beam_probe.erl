-module(beam_probe).
-export([pass/1,select/1]).
pass(Value) -> put(trace,[pass]), Value.
select(Value) -> put(trace,[select|get(trace)]), Value.
