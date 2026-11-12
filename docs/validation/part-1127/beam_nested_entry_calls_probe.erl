-module(beam_nested_entry_calls_probe).
-export([run/3, chain/2, survivors/3, boxed/3, discarded/3, siblings/2, siblings_keep/2, siblings_nonzero/2, siblings_three/3]).
run(First, Second, _Unused) ->
    finish(read(First, Second, []), tail).
chain(First, Second) ->
    finish(tail, wrap(tag, read(First, Second, []))).
survivors(First, Second, Third) ->
    finish(Second, wrap(Third, read(First, Second, []))).
boxed(First, Second, Third) ->
    finish(Third, [wrap(tag, read(First, Second, []))]).
discarded(First, Second, Third) ->
    read(First, entry, []),
    read(Second, middle, []),
    finish(Third, tail).
siblings(First, Second) ->
    finish(left(First), right(Second)).
siblings_keep(First, Second) ->
    finish_keep(left(First), right(Second), Second).
siblings_nonzero(First, Second) ->
    finish(left(First), finish_keep(literal, Second, tail)).
siblings_three(First, Second, Third) ->
    finish_keep(left(First), right(Second), last(Third)).
left(Value) -> {left, Value}.
right(Value) -> {right, Value}.
last(Value) -> {last, Value}.
read(fail, _, _) -> erlang:error(probe_failure);
read(First, Second, Empty) -> {First, Second, Empty}.
wrap(Tag, Value) -> {Tag, Value}.
finish(First, Second) -> {First, Second}.
finish_keep(First, Second, Saved) -> {First, Second, Saved}.

