-module(beam_call_result_bif).
-export([prefix/2, details/2]).
prefix(Input, Limit) ->
    Binary = iolist_to_binary(Input),
    binary_part(Binary, 0, min(Limit, byte_size(Binary))).
details(Input, Keep) ->
    Reversed = lists:reverse(Input),
    pack(length(Reversed), Reversed, Keep).
pack(Length, Value, Keep) -> {Length, Value, Keep}.
