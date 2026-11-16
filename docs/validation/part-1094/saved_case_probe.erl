-module(saved_case_probe).
-export([terminal/3, sequenced/3, nested/3, alias_live/3]).
terminal(First, Second, Keep) ->
    case {First, Second} of
        {old, expected} ->
            Saved = erlang:put(First, Keep),
            erlang:put(follow, Saved),
            {Saved, Keep}
    end.
sequenced(First, Second, Keep) ->
    erlang:put(mark, Keep),
    case {First, Second} of
        {old, expected} ->
            Saved = erlang:put(First, Keep),
            erlang:put(follow, Saved),
            {Saved, Keep}
    end.
nested(First, Second, Keep) ->
    Result = case {First, Second} of
        {old, expected} ->
            Saved = erlang:put(First, Keep),
            erlang:put(follow, Saved),
            {Saved, Keep}
    end,
    {First, Second, Result}.
alias_live(First, Second, Keep) ->
    case {First, Second} of
        {Alias, expected} ->
            Saved = erlang:put(Alias, Keep),
            erlang:put(follow, Saved),
            {Alias, Saved, Keep}
    end.
