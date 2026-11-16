-module(register_clause_probe).
-export([run/2, fallback/2]).
run(42, X) -> lists:reverse(X);
run(stop, X) -> lists:append(X).
fallback(42, X) -> lists:reverse(X);
fallback(_, X) -> lists:append(X).
