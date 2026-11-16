-module(beam_reuse_dying_call_slot).
-export([run/3, twice/3, prepare/1, observe/3, observe/4, finish/1]).

run(Seed, First, Last) ->
    Value = prepare(Seed),
    observe(First, Value, Last),
    finish({Value, First}).

twice(Seed, First, Last) ->
    Value = prepare(Seed),
    observe(First, Value, Last, Last),
    finish({Value, First}).

prepare(Seed) -> {value, Seed}.
observe(First, Value, Last) -> put(observed, {First, Value, Last}).
observe(First, Value, Left, Right) -> put(observed, {First, Value, Left, Right}).
finish(Value) -> {Value, get(observed)}.
