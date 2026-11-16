-module(constructor_tail_probe).
-export([record_tail/2, tuple_tail/2]).
-record(state, {parent, monitor, includes, table}).

record_tail(Parent, Includes) ->
    Ref = erlang:monitor(process, Parent),
    finish(#state{parent = Parent, monitor = Ref, includes = Includes,
                  table = ets:new(?MODULE, [])}).

tuple_tail(Key, Value) ->
    finish({entry, Value, erlang:integer_to_list(Key)}).

finish(Term) ->
    {finished, Term}.
