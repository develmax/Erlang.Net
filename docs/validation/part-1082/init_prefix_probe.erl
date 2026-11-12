-module(init_prefix_probe).
-export([prefix/2]).
prefix(Key, Tag) ->
    Fixed = fixed,
    Pid = erlang:self(),
    Intermediate = erlang:get(Key),
    Value = erlang:get(Intermediate),
    Previous = erlang:put(prefix_output, Value),
    {Tag, Fixed, Pid, Value, Previous}.

