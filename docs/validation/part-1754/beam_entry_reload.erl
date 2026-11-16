-module(beam_entry_reload).
-export([guarded/2, two/2]).

guarded(Mode, Value) ->
    Old = case Mode of
        one -> put(Mode, Value);
        two -> put(Mode, Value)
    end,
    {Mode, Value, Old}.

two(Key, Value) ->
    put(Key, Value),
    put(Key, Value).
