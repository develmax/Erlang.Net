-module(beam_unused_catch_stack).
-export([run/1, source/1]).
run(Kind) ->
    try source(Kind) of
        {ok, _, Value, _} -> {ok, Value};
        _ -> failed
    catch
        error:_ -> failed
    end.
source(error) -> erlang:error(broken);
source(throw) -> throw(uncaught);
source(Value) -> Value.
