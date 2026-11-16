-module(beam_binary_tail_recursion).
-export([trim/1]).
trim(<<C, Rest/binary>>) when C == 9; C == 10; C == 13; C == 32 ->
    trim(Rest);
trim(Value) ->
    Value.
