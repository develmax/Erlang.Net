-module(beam_computed_tail_probe).
-export([convert/2]).

convert(Tuple, Seed) ->
    Reserved = reserved(Seed),
    finish(tuple_to_list(Tuple), Reserved, []).

reserved(Value) ->
    Value.

finish(Items, Reserved, _) ->
    {Items,Reserved}.
