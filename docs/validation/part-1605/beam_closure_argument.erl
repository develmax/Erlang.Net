-module(beam_closure_argument).
-export([run/3]).
run(Input, Title, Callback) ->
    consume(Input, [{hook, fun handler/2, {Title, Callback}}]).
consume(Input, [{hook, F, {A, B}}]) ->
    {Input, F(A, B)}.
handler(A, B) ->
    {B,A}.
