-module(tuple_guard_probe).
-export([sized/1, typed/1, target/0, work/1]).
sized(Value) when tuple_size(Value) =:= 2 ->
    Target = ?MODULE:target(),
    Target:work(Value),
    ok.
typed(Value) when is_tuple(Value) orelse is_binary(Value) ->
    Target = ?MODULE:target(),
    Target:work(Value),
    ok.
target() -> ?MODULE.
work(Value) -> self() ! {ran, Value}, ok.
