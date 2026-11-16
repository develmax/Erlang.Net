-module(compound_guard_probe).
-export([run/4, target/0, work/4]).
run(A,B,C,D) when is_float(A),tuple_size(B) =:= 2;
                  tuple_size(B) =:= 3,tuple_size(C) =:= 2;
                  tuple_size(C) =:= 3,tuple_size(D) =:= 2 ->
    Target = ?MODULE:target(),
    Target:work(A,B,C,D),
    ok.
target() -> ?MODULE.
work(A,B,C,D) -> self() ! {A,B,C,D}, ok.
