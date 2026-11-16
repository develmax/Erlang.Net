-module(beam_type_forms_probe).
-export([value/1]).

-file("grammar.yrl", 100).
-type(item(A) :: {ok, A} | {error, '+' | '-' | 'bnot'}).
-opaque hidden() :: integer().
-nominal tag() :: atom().
-spec(value(Input :: term()) -> item(term())).
value(Input) -> {ok, Input}.
