-module(beam_bif_result_store).
-export([run/1, live/1]).

run(Value) ->
    try
        Result = Value + 1,
        erlang:garbage_collect(),
        {ok, Result}
    catch
        error:badarith -> badarith
    end.

live(Value) ->
    Result = Value + 1,
    Collected = collect(Result),
    {Result, Collected, Value}.

collect(Value) ->
    erlang:garbage_collect(),
    Value.
