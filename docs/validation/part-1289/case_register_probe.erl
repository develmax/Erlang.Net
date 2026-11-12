-module(case_register_probe).
-export([run/0, prepare/0, select/0, pack/2]).
run() ->
    First = ?MODULE:prepare(),
    Second = case ?MODULE:select() of
                 {tag, _} -> left;
                 _ -> right
             end,
    Joined = ?MODULE:pack(First, Second),
    ?MODULE:pack(Joined, marker).
prepare() -> saved.
select() -> get(choice).
pack(A, B) -> {A,B}.
