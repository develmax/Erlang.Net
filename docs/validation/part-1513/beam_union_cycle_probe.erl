-module(beam_union_cycle_probe).
-export([choose/3, choose_unknown/3]).

choose(Count, Input, Default) ->
    case first(Count, Input) of
        missing -> {value, Default};
        Found -> Found
    end.

choose_unknown(Count, Input, Default) ->
    case first_unknown(Count, Input) of
        missing -> {value, Default};
        Found -> Found
    end.

first(0, _) -> missing;
first(1, Value) -> {value, Value};
first(Count, Value) -> second(Count - 1, Value).

second(0, Value) -> {second, Value};
second(Count, Value) -> first(Count - 1, Value).

first_unknown(0, _) -> missing;
first_unknown(1, Value) -> {value, Value};
first_unknown(Count, Value) -> second_unknown(Count - 1, Value).

second_unknown(0, Value) -> Value;
second_unknown(Count, Value) -> first_unknown(Count - 1, Value).
