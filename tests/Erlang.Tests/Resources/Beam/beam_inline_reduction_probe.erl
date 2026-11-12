-module(beam_inline_reduction_probe).
-export([run/1, second/2]).
-compile({inline, reduce/1}).

run(Stack) -> reduce(Stack).
second(_, Stack) -> reduce(Stack).

-file("grammar.yrl", 100).
reduce(Stack) ->
    [Head | Tail] = Stack,
    [begin Head end | Tail].
