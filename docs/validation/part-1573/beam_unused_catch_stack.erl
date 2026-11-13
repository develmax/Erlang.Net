-module(beam_unused_catch_stack).
-export([run/1, source/1]).
run(Kind) ->
    Prepared = prepare(Kind),
    try source(Prepared) of
        {ok, _, Value, _} -> {ok, Value};
        _Other -> failed
    catch
        error:_ -> failed
    end.
prepare(prefix_error) -> erlang:error(prefix_broken);
prepare(Kind) -> Kind.
source(error) -> erlang:error(broken);
source(throw) -> throw(uncaught);
source(Value) -> Value.
