-module(case_result_probe).
-export([run/1, pack/2, prepare/1, select/1, left/0, right/0]).
run(Value) ->
    Prepared = ?MODULE:prepare(Value),
    Result = case ?MODULE:select(Prepared) of
                 true -> ?MODULE:left();
                 false -> ?MODULE:right()
             end,
    ?MODULE:pack(Result, marker).
prepare(Value) -> Value.
select(invalid) -> other;
select(Value) -> is_atom(Value).
left() -> left.
right() -> right.
pack(Result, Marker) -> {Result, Marker}.
