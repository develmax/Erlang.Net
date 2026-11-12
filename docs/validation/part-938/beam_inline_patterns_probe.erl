-module(beam_inline_patterns_probe).
-export([run/3]).
-compile({inline,[classify/2,bits/2,protect/1]}).
run(Key, Input, Binary) ->
    {classify(Key, Input), bits(8, Binary), protect(Input)}.
classify(Key, Input) -> case Input of #{Key := Value} = Map -> {Value, Map}; _ -> absent end.
bits(Size, Binary) ->
    case Binary of <<N:Size, Rest/binary>> -> {N,Rest}; _ -> absent end.
protect(Value) ->
    try element(1, Value) catch error:badarg -> absent end.
