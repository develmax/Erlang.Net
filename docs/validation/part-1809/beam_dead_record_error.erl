-module(beam_dead_record_error).
-export([run/1]).

-record(entry, {value}).

run(Value) ->
    Entry = #entry{value=Value},
    put(last_entry, Entry),
    read_private(Entry).

read_private(Entry) ->
    {value, Entry#entry.value}.
