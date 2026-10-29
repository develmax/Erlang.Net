-module(dynamic_apply_target520).
-export([echo/1, raise_error/1, raise_throw/1, raise_exit/1]).

echo(Value) -> {echoed, Value}.
raise_error(Value) -> erlang:error({target_error, Value}).
raise_throw(Value) -> throw({target_throw, Value}).
raise_exit(Value) -> exit({target_exit, Value}).
