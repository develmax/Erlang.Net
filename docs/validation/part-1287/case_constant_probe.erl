-module(case_constant_probe).
-export([run/0, select/0, pack/2]).
run() ->
    Base = "constant",
    Result = case ?MODULE:select() of
                 {tag, _} -> ?MODULE:pack(left, Base);
                 _ -> ?MODULE:pack(right, Base)
             end,
    ?MODULE:pack(Result, marker).
select() -> get(choice).
pack(A, B) -> {A,B}.
