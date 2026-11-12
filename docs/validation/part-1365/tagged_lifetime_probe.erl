-module(tagged_lifetime_probe).
-export([delay/1, repeat/1, late_repeat/1, retain_input/1, trim/2]).

delay(Input) ->
    {ok, Payload} = check(Input),
    Next = step(first),
    finish(Next, Payload).

repeat(Input) ->
    {ok, Payload} = check(Input),
    Next = step(Payload),
    finish(Next, Payload).

late_repeat(Input) ->
    {ok, Payload} = check(Input),
    step(first),
    Next = step(Payload),
    finish(Next, Payload).

retain_input(Input) ->
    {ok, Payload} = check(Input),
    Next = step(first),
    finish_three(Input, Next, Payload).

trim(Input, Carry) ->
    {ok, Payload} = check(Input),
    step_pair(Payload, Carry),
    finish(Payload, Payload).

check(Input) ->
    put(tagged_lifetime_checked, Input),
    get(tagged_lifetime_value).

step(Value) ->
    put(tagged_lifetime_step, Value),
    {step, Value}.

step_pair(Value, Carry) ->
    put(tagged_lifetime_step, {Value, Carry}).

finish(Value, Payload) ->
    {Value, Payload, get(tagged_lifetime_step)}.

finish_three(Input, Value, Payload) ->
    {Input, Value, Payload, get(tagged_lifetime_step)}.
