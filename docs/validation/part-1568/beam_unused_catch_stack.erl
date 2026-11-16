-module(beam_unused_catch_stack).
-export([run/1, used/1, source/1]).
run(Kind) ->
    Result = try source(Kind) of
        Value -> {ok, Value}
    catch
        error:Reason -> {caught, Reason}
    end,
    {result, Result}.
used(Kind) ->
    try source(Kind)
    catch error:_Reason:Stack -> is_list(Stack)
    end.
source(error) -> erlang:error(broken);
source(throw) -> throw(uncaught);
source(Value) -> Value.
