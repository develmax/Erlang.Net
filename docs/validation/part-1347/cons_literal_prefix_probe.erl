-module(cons_literal_prefix_probe).
-export([route/1, scan/2, forward/1, keep/2]).

route([$-|Tail]) ->
    route(Tail);
route([$+|_]) ->
    rejected;
route(Value) ->
    Text = erlang:integer_to_list(Value),
    {accepted,Text}.

scan([skip|Tail], State) ->
    finish(State,Tail);
scan([stop|_], State) ->
    State;
scan(Value, State) ->
    {State,erlang:length(Value)}.

forward([pass|Tail]) ->
    pack(first,second,Tail);
forward(Value) ->
    {length,erlang:length(Value)}.

keep(State, [keep|_]) ->
    State;
keep(State, Value) ->
    {State,erlang:length(Value)}.

finish(State,Tail) ->
    {State,Tail}.

pack(First,Second,Third) ->
    {First,Second,Third}.
