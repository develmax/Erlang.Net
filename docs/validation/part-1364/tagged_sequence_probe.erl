-module(tagged_sequence_probe).
-export([advance/1, carry/1, ignored/1, branch/2, pinned/1, whole/1]).

advance(Input) ->
    {ok, Value} = check(Input),
    Next = step(Value),
    finish(Next).

carry(Input) ->
    {ok, Value} = check(Input),
    Next = step(Value),
    finish_pair(Input, Next).

ignored(Input) ->
    {ok, _} = check(Input),
    Next = step(constant),
    finish(Next).

branch(Input, Flag) ->
    {ok, Value} = check(Input),
    case Flag of
        true -> step(Value);
        false -> {kept, Input, Value}
    end.

pinned(Input) ->
    {ok, Input} = check(Input),
    Next = step(Input),
    finish(Next).

whole(Input) ->
    {ok, _} = check(Input).

check(Input) ->
    put(tagged_probe_checked, Input),
    get(tagged_probe_result).

step(Value) ->
    put(tagged_probe_step, Value),
    {step, Value}.

finish(Value) ->
    {finished, Value, get(tagged_probe_step)}.

finish_pair(Input, Value) ->
    {finished, Input, Value, get(tagged_probe_step)}.
