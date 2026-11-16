-module(beam_nested_entry_calls_probe).
-export([run/3, chain/2]).
run(First, Second, _Unused) ->
    finish(read(First, Second, []), tail).
chain(First, Second) ->
    finish(tail, wrap(tag, read(First, Second, []))).
read(fail, _, _) -> erlang:error(probe_failure);
read(First, Second, Empty) -> {First, Second, Empty}.
wrap(Tag, Value) -> {Tag, Value}.
finish(First, Second) -> {First, Second}.

