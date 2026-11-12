-module(branch_liveness_probe).
-export([multi/3, selected/3, exported/3, assigned/3, checked/3, if_tail/3, if_join/3, if_failed/3]).
multi(yes, Old, Keep) when is_atom(Old) ->
    New = erlang:put(branch, Old),
    erlang:put(follow, New),
    {New, Keep};
multi(_, Old, Keep) -> {Old, Keep}.
selected(Mode, Old, Keep) ->
    case Mode of
        yes when is_atom(Old) ->
            New = erlang:put(branch, Old),
            erlang:put(follow, New),
            {New, Keep};
        _ -> {Old, Keep}
    end.
exported(Mode, Old, Keep) ->
    case Mode of
        yes when is_atom(Old) ->
            New = erlang:put(branch, Old),
            erlang:put(follow, New);
        _ -> New = fallback
    end,
    {Old, Keep, New}.

assigned(Mode, Old, Keep) ->
    Result = case Mode of
        yes ->
            New = erlang:put(branch, Old),
            erlang:put(follow, New),
            New;
        _ -> Old
    end,
    {Old, Keep, Result}.
checked(Mode, Old, Expected) ->
    Expected = case Mode of
        yes ->
            New = erlang:put(branch, Old),
            erlang:put(follow, New),
            New;
        _ -> Old
    end,
    {Old, Expected}.

if_tail(Mode, Old, Keep) ->
    if
        Mode =:= yes ->
            New = erlang:put(branch, Old),
            erlang:put(follow, New),
            {New, Keep};
        true -> {Old, Keep}
    end.
if_join(Mode, Old, Keep) ->
    if
        Mode =:= yes ->
            New = erlang:put(branch, Old),
            erlang:put(follow, New);
        true -> New = fallback
    end,
    {Old, Keep, New}.
if_failed(Mode, Old, Keep) ->
    if
        Mode =:= yes ->
            New = erlang:put(branch, Old),
            erlang:put(follow, New),
            {New, Keep}
    end.
