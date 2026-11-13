-module(beam_record_call_result).
-export([run/2, nested/2]).
-record(entry, {a, b}).
run(A, B) ->
    First = observe(first, A),
    Second = observe(second, B),
    #entry{a = First, b = Second}.
nested(A, B) ->
    First = observe(first, A),
    {tag, #entry{a = First, b = observe(second, B)}}.
observe(Key, Value) -> put(Key, Value), Value.
