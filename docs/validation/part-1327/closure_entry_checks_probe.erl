-module(closure_entry_checks_probe).
-export([make/1, make_dispatch/1, target/1]).
make(Saved) ->
    fun({tag,A}) -> X = target(A), {X,Saved} end.
make_dispatch(Saved) ->
    fun({tag,A}) when is_atom(A) -> target({A,Saved});
       (_) -> no
    end.
target(A) -> {called,A}.
