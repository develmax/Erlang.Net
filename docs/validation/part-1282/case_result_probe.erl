-module(case_result_probe).
-export([run/1, pack/2, select/1, left/0, right/0]).
run(Value) ->
    Result = case ?MODULE:select(Value) of
                 true -> ?MODULE:left();
                 false -> ?MODULE:right()
             end,
    ?MODULE:pack(Result, marker).
select(Value) -> is_atom(Value).
left() -> left.
right() -> right.
pack(Result, Marker) -> {Result, Marker}.
