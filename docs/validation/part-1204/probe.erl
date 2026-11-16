-module(map_record_lifetime_probe).
-export([f/3]).
-record(state, {fixed, value}).
identity(V) -> V.
f(Map, Key, Value) ->
    First = Map#{identity(Key) => (Bound = identity(Value))},
    Second = (identity(#state{}))#state{fixed = tag, value = (Next = identity(Bound))},
    Third = First#{identity(Key) := identity(Next)},
    {Third, Second, Bound, Next}.
