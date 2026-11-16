-module(beam_case_multi_projected_tail).
-export([run/3]).

run(Key, Rows, Extra) ->
    case proplists:lookup(Key, Rows) of
        {First, Second} -> finish(First, Second, Extra);
        none -> missing
    end.

finish(First, Second, Extra) ->
    {First, Second, Extra}.
