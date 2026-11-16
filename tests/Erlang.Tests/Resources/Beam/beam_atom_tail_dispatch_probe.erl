-module(beam_atom_tail_dispatch_probe).
-export([select/2, select_second/2, bound_fallback/2]).

select(Key, Input) ->
    case Key of
        gamma -> pack(Input, [3], 1);
        alpha -> pack(Input, [1], 1);
        beta -> pack(Input, [2], 1);
        Other -> exit({failed, {unrecognized, Other}})
    end.

select_second(Input, Key) ->
    case Key of
        beta -> pack(Key, Input, 2);
        alpha -> pack(Key, Input, 1);
        _ -> erlang:exit({failed, {unrecognized, Key}})
    end.

bound_fallback(Key, Input) ->
    case Key of
        beta -> pack(Input, Key, 2);
        alpha -> pack(Input, Key, 1);
        Input -> erlang:exit({failed, {unrecognized, Input}})
    end.

pack(First, Second, Third) ->
    {First, Second, Third}.
