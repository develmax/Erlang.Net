-module(forwarded_tail_probe).
-export([forward/3, captured/2, fixed/2, variable/3, variable_fixed/2, pattern_fixed/3]).

forward(Map, Left, Right) ->
    Built = identity(Map),
    finish(maps:values(Built), Left, Right).

finish(Values, Left, Right) ->
    {Values,Left,Right}.

identity(Value) ->
    Value.

captured(Input, State) ->
    Result = identity(Input),
    invoke(fun(V) -> {Result,V} end, Result, State).

invoke(Fun, Value, State) ->
    {Fun(State),Value}.

fixed(Input, State) ->
    relay(Input, fixed, State).

relay(Input, Prefix, State) ->
    worker(Input, Prefix, State).

worker(Input, Prefix, State) ->
    Values = lists:reverse(Input),
    lists:mapfoldl(fun(Entry, Acc) -> {{Prefix,Entry},Acc+1} end, State, Values).

variable(Input, Prefix, State) ->
    relay_variable(Input, Prefix, State).

variable_fixed(Input, State) ->
    relay_variable(Input, fixed, State).

relay_variable(Input, Prefix, State) ->
    worker_variable(Input, Prefix, State).

worker_variable(Input, Prefix, State) ->
    Values = lists:reverse(Input),
    lists:mapfoldl(fun(Entry, Acc) -> {{Prefix,Entry},Acc+1} end, State, Values).

pattern_fixed({wrapped,pattern=Prefix}, Input, State) ->
    worker_pattern(Input, Prefix, State).

worker_pattern(Input, Prefix, State) ->
    Values = lists:reverse(Input),
    lists:mapfoldl(fun(Entry, Acc) -> {{Prefix,Entry},Acc+1} end, State, Values).
