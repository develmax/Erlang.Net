-module(beam_pretrim).
-export([assign/2]).
assign({Left,Right}, Key) ->
    Previous = erlang:put(saved, Key),
    {Previous, Left, Right}.
