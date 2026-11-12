-module(beam_release_inline_probe).
-export([run/3]).

run(Items, First, Second) -> {fold(Items, First), fold(Items, Second)}.

fold(Items, Bias) ->
    lists:foldl(fun (Item, Acc) -> [Item+Bias|Acc] end, [], Items).
