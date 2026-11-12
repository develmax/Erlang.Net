-module(discarded_branch_catch_probe).
-export([run/3]).
run(Mode, Payload, Decision) ->
    case choose(Mode, Payload, Decision) of
        {failed, _Reason} -> (catch cleanup(Mode, Payload)), recovered;
        _ -> ok
    end.
choose(_, _, yes) -> {failed, reason};
choose(_, _, _) -> ok.
cleanup(error, Value) -> erlang:error(Value);
cleanup(throw, Value) -> throw(Value);
cleanup(exit, Value) -> exit(Value);
cleanup(_, Value) -> Value.
