-module(literal_body_probe).
-export([dispatch/3, recycle/3, protected/1, guarded/2]).

dispatch(skip, _, _) ->
    skipped;
dispatch(run, Value, Keep) ->
    Seen = step(Value),
    step(Seen),
    pair(Seen, Keep);
dispatch(_, Value, _) ->
    {unexpected, Value}.

recycle(run, First, Next) ->
    step(First),
    Seen = step(Next),
    Checked = step(Seen),
    pair(Checked, done);
recycle(_, First, _) ->
    First.

protected(default) ->
    ok;
protected(Value) ->
    try step(Value) of
        Result -> {ok, Result}
    catch
        throw:bad -> ignored
    end.

guarded(run, Value) when is_integer(Value) ->
    step(Value),
    step(Value + 1),
    {ran, Value};
guarded(run, Value) ->
    {skipped, Value};
guarded(_, Value) ->
    Value.

step(Value) ->
    put(trace, [Value | get(trace)]),
    case Value of
        bad -> throw(bad);
        _ -> Value
    end.

pair(A, B) ->
    {A, B}.
