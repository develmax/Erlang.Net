-module(boxed_integer559).
-export([run/0]).

run() ->
    Values = [-(1 bsl 59), (1 bsl 59) - 1, 1 bsl 59,
              -(1 bsl 59) - 1, (1 bsl 64) - 1, 1 bsl 64,
              -((1 bsl 127) - 1), 1 bsl 128,
              (1 bsl 1024) + (1 bsl 64) + 7],
    [{wordsize, erlang:system_info(wordsize)},
     {dynamic, [observe(N) || N <- Values]},
     {literal, observe_literal(1361129467683753853853498429727072845831)}].

observe(Input) ->
    First = decode(Input),
    Second = decode(Input),
    [{value, First},
     {encoded, term_to_binary(First)},
     {is_integer, is_integer(First)},
     {self_same, erts_debug:same(First, First)},
     {independent_same, erts_debug:same(First, Second)},
     {scalar_sizes, sizes(First)},
     {shared_sizes, sizes({First, First})},
     {independent_sizes, sizes({First, Second})}].

observe_literal(Value) ->
    [{value, Value}, {encoded, term_to_binary(Value)},
     {sizes, sizes(Value)}, {self_same, erts_debug:same(Value, Value)}].

sizes(Value) ->
    [{flat, erts_debug:flat_size(Value)},
     {shared, erts_debug:size_shared(Value)}].

decode(Value) ->
    binary_to_term(term_to_binary(Value)).
