-module(case_result_probe).
-export([run/1, pack/2, prepare/1, select/1, pair/2, take/1]).
run(Value) ->
    Prepared = ?MODULE:prepare(Value),
    Result = case ?MODULE:select(Prepared) of
                 true -> ?MODULE:take(?MODULE:pair(Prepared, left));
                 false -> ?MODULE:take(?MODULE:pair(Prepared, right))
             end,
    ?MODULE:pack(Result, marker).
prepare(Value) -> Value.
select(invalid) -> other;
select(Value) -> is_atom(Value).
pair(Value, Side) -> {Value, Side}.
take({_Value, Side}) -> Side.
pack(Result, Marker) -> {Result, Marker}.

