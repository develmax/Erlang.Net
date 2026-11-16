-module(beam_entry_slot_order).
-export([run/3]).

run(Key, Values, Default) ->
    case lists:member(Key, Values) of
        true -> Values;
        false -> [Default|Values]
    end.
