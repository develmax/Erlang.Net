-module(beam_inline_bindings_probe).
-export([run/3]).
-compile({inline, pop/1}).
run(Value, Left, Right) -> {pop(Left), pop(Right), Value}.
pop(Stack) -> [Value|_] = Stack, Value.
