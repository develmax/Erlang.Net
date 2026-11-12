-module(branch_liveness_probe).
-export([multi/3, selected/3, exported/3]).
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
