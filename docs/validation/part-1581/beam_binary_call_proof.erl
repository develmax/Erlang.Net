-module(beam_binary_call_proof).
-export([run/1]).
run(<<Prefix:8, Payload/binary>>) ->
    {Prefix, forward(Payload, [])}.
forward(Value, []) ->
    [B || B <- [trim(Value)], <<>> /= B];
forward(Value, Acc) ->
    [Value | Acc].
trim(<<C, Rest/binary>>) when C == 9; C == 10; C == 13; C == 32 ->
    trim(Rest);
trim(Value) ->
    Value.
