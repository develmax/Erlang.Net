-module(beam_catch_stack_slots).
-export([run/2, twice/3, after_catch/2]).

run(Input, Keep) ->
    First = put(start, Input),
    Value = catch begin
        X = put(work, First),
        Y = checked(X),
        Z = put(done, Y),
        {Y, Z}
    end,
    {Keep, Value, put(last, Value)}.

twice(Input, Keep, Other) ->
    Base = put(start, Input),
    Value = catch begin
        A = checked(Base),
        B = put(work, A),
        C = checked(B),
        D = put(done, C),
        {A, C, D}
    end,
    {Value, Keep, Other}.

after_catch(Input, Keep) ->
    Old = put(start, Input),
    Value = catch checked(Old),
    Next = checked(put(last, Value)),
    {Value, Next, Keep}.

checked({ok, Value}) -> Value;
checked({throw, Value}) -> throw(Value);
checked({exit, Value}) -> exit(Value);
checked(Other) -> erlang:error({bad, Other}).
