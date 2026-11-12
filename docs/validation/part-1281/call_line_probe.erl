-module(call_line_probe).
-export([run/0, separate/0, id/1, fail/1]).
run() ->
    ?MODULE:fail(?MODULE:id(ok)),
    done.
separate() ->
    ?MODULE:fail(
        ?MODULE:id(ok)),
    done.
id(Value) -> Value.
fail(Value) -> erlang:error(Value).
