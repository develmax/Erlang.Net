-module(init_prefix_probe).
-export([prefix/2]).
prefix(Key, Tag) ->
    Fixed = fixed,
    Pid = erlang:self(),
    Value = erlang:get(Key),
    Previous = erlang:put(prefix_output, Value),
    {Tag, Fixed, Pid, Value, Previous}.
