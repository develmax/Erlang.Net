-module(call_line_probe).
-export([run/0, id/1, fail/1]).
run() ->
    ?MODULE:fail([{value, ?MODULE:id(ok)}]),
    done.
id(Value) -> Value.
fail(Value) -> erlang:error(Value).
