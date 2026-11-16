-module(beam_call_comparison_probe).
-export([same/2, different/2, literal/1]).
same(Input, Expected) -> helper(Input) =:= Expected.
different(Expected, Input) -> Expected =/= helper(Input).
literal(Input) -> helper(Input) =:= ok.
helper(fail) -> erlang:error(probe_failure);
helper(Value) -> Value.
