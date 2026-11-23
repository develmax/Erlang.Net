-module(beam_external_leaf_tail).
-export([resolve/1]).

resolve(Value) when is_integer(Value) -> {ok, Value};
resolve(Value) when is_atom(Value) -> leaf_tail_probe:resolve(Value, stream);
resolve(_) -> {error, invalid}.
