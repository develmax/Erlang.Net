-module(beam_typed_selector).
-export([integer_result/1, mixed_result/1]).

integer_result(Bin) ->
    case signed_value(Bin) of
        0 -> zero;
        1 -> one;
        2 -> two;
        Other -> {unknown, Other}
    end.

mixed_result(Value) ->
    case either_value(Value) of
        0 -> zero;
        1 -> one;
        2 -> two;
        Other -> {unknown, Other}
    end.

signed_value(Bin) ->
    Len = byte_size(Bin),
    <<Value:Len/signed-unit:8>> = Bin,
    Value.

either_value(<<Value:8>>) -> Value;
either_value(Value) -> Value.
