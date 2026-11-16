-module(beam_try_stack_slots).
-export([run/2, twice/3, after_try/3]).

run(Input, Keep) ->
    First = put(start, Input),
    Value = try
        X = put(work, First),
        Y = checked(X),
        Z = put(done, Y),
        {Y, Z}
    catch error:Reason -> {caught, Reason, Keep}
    end,
    {Keep, Value, put(last, Value)}.

twice(Input, Keep, Other) ->
    Base = put(start, Input),
    try
        A = checked(Base),
        B = put(work, A),
        C = checked(B),
        D = put(done, C),
        {A, C, D, Keep}
    catch error:Reason:Stack ->
        {Reason, Keep, Other, hd(Stack)}
    end.

after_try(Input, Keep, Other) ->
    try put(first, Input) of
        X -> Y = checked(X), {Y, put(done, Y), Keep}
    catch error:Reason -> {caught, Reason, Keep, Other}
    end.

checked({ok, Value}) -> Value;
checked(Other) -> erlang:error({bad, Other}).
