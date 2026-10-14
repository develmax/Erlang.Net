-module(beam_fixture_closure).
-export([run/0,make/1]).
run() -> F=make(5), {ok,F(7),is_function(F,1)}.
make(N) -> fun(X) -> N+X end.
