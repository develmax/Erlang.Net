-module(beam_discarded_calls).
-export([run/2]).
run(Kept, FirstOnly) ->
    note(first, FirstOnly),
    note(second, Kept),
    note(last, done),
    ok.
note(second, stop) -> erlang:error(stopped);
note(Key, Value) -> put(Key, Value).
