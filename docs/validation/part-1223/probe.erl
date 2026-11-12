-module(forwarded_stack_raise_probe).
-export([f/2]).
f(Frame, Value) ->
    try erlang:error(Value)
    catch error:expected:Stack ->
        Trace = [Frame | tl(Stack)],
        erlang:raise(error, replaced, Trace)
    end.
