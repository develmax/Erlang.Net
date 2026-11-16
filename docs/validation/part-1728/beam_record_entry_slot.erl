-module(beam_record_entry_slot).
-export([run/2]).
-record(item, {value}).
run(A, R) ->
    First = R#item.value,
    {A, First}.
