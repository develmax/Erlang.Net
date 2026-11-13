-module(beam_guarded_body_binary).
-export([split/2, optional/2]).
split(Bin, Count) when is_binary(Bin), is_integer(Count) ->
    <<Part:Count/binary, Tail/binary>> = Bin,
    {Part,Tail}.
optional(Bin, Count) when is_binary(Bin); is_atom(Bin) ->
    <<Part:Count/binary, Tail/binary>> = Bin,
    {Part,Tail}.
