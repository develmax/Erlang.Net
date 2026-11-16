-module(beam_shared_integer_guard).
-export([run/1]).
run(default) -> ok;
run(infinity) -> ok;
run(Int) when is_integer(Int), Int > 0 -> ok;
run(Value) -> throw({error, {bad_value, Value}}).
