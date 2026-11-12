-module(beam_projection_moves).
-export([count/1, wrapped/2, use_twice/1, keep_argument/2, visit/1]).
count({items, Values}) -> count(Values);
count([]) -> 0;
count([_|Tail]) -> 1+count(Tail).
wrapped({data, Value}, Keep) -> pack(Keep, Value);
wrapped(Value, Keep) -> pack(Keep, Value).
use_twice({repeat, Value}) -> pack(Value, Value);
use_twice(Value) -> pack(Value, Value).
keep_argument({data, Value}, Keep) -> pack(Value, Keep);
keep_argument(Value, Keep) -> pack(Value, Keep).
visit({node, Values}) -> visit(Values);
visit([]) -> ok;
visit([Value|Tail]) ->
    Key = identity(Value),
    observe(Key, Tail),
    visit(Tail).
identity(Value) -> Value.
observe(Key, Tail) -> put(last_visit, {Key, Tail}).
pack(A, B) -> {A, B}.

