-module(register_guard_probe).
-export([run/2, checked/1]).
run(tag, X) when is_integer(X) -> lists:duplicate(X, a);
run(_, X) when erlang:is_list(X) -> lists:reverse(X);
run(_, X) -> lists:member(X, [other]).
checked(X) when is_binary(X) -> binary:copy(X);
checked(X) when is_list(X) -> lists:reverse(X).
