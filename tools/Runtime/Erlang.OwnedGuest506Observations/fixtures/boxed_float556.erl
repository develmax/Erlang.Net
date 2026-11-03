-module(boxed_float556).
-export([run/0]).

run() ->
    Bits = [16#0000000000000000, 16#8000000000000000,
            16#3ff8000000000000, 16#0000000000000001,
            16#8000000000000001, 16#7fefffffffffffff],
    [{dynamic, [observe(B) || B <- Bits]},
      {literal, observe_literal(0.5)},
      {rejected_encodings, [decode_outcome(B) || B <-
          [16#7ff0000000000000, 16#fff0000000000000,
           16#7ff8000000000001]]}].

observe(Bits) ->
    First = decode(Bits),
    Second = decode(Bits),
    Shared = {First, First},
    Independent = {First, Second},
    [{bits, Bits},
      {value, First},
      {encoded, term_to_binary(First)},
      {is_float, is_float(First)},
      {self_same, erts_debug:same(First, First)},
      {independent_same, erts_debug:same(First, Second)},
      {scalar_sizes, sizes(First)},
      {shared_sizes, sizes(Shared)},
      {independent_sizes, sizes(Independent)}].

observe_literal(Value) ->
    [{value, Value},
      {encoded, term_to_binary(Value)},
      {sizes, sizes(Value)},
      {self_same, erts_debug:same(Value, Value)}].

sizes(Value) ->
    [{flat, erts_debug:flat_size(Value)},
      {shared, erts_debug:size_shared(Value)}].

decode(Bits) ->
    binary_to_term(<<131, 70, Bits:64/unsigned-big>>).

decode_outcome(Bits) ->
    try decode(Bits) of
        Value -> {returned, Value}
    catch
        Class:Reason:Stack -> {raised, Class, Reason, Stack}
    end.
