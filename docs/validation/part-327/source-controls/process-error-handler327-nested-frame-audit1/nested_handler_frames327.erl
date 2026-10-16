%% Proposed ordinary source only; not compiled or executed.
-module(nested_handler_frames327).
-export([run/0,undefined_function/3]).
run() ->
    Old = process_flag(error_handler, ?MODULE),
    try
        try missing327_frame_probe:outer(tag327) of
            _ -> erlang:error(missing_expected_error)
        catch
            error:{nested_boom,tag327}:Stack ->
                Frames = [A || {?MODULE,undefined_function,A,_} <- Stack],
                %% Native ordinary callers use arity; an SDK frame carrying
                %% actual args must identify the INNER invocation, never outer.
                case Frames of
                    [[missing327_frame_probe,inner,[tag327]]|_] -> ok;
                    [3|_] -> ok;
                    _ -> erlang:error({wrong_innermost_handler,Frames})
                end,
                true = length(Frames) >= 2,
                undefined = get(outer_returned),
                done
        end
    after
        process_flag(error_handler, Old),
        erase(outer_returned)
    end.
undefined_function(M, outer, [Tag]) ->
    Value = erlang:apply(M, inner, [Tag]),
    put(outer_returned, Value),
    Value;
undefined_function(missing327_frame_probe, inner, [Tag]) ->
    erlang:error({nested_boom,Tag}).
