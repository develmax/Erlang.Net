-module(beam_record_entry_guard).
-export([run/2]).
-record(item, {value}).
run(A, R) when R#item.value =:= active -> {accepted, A};
run(A, _) -> {rejected, A}.
