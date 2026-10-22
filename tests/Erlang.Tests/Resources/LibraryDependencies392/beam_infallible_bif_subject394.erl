-module(beam_infallible_bif_subject394).
-export([worker/2,probe/1,get_before/1,min_before/2,max_before/2,retain/2]).

worker(Parent,Case) ->
    Outcome=try probe(Case) of Value->{returned,Value}
            catch Class:Reason:Stack->{caught,Class,Reason,Stack} end,
    Parent ! {Case,Outcome},ok.

probe(get_absent) -> get_before({state,[1,2]});
probe(get_tuple_key) -> erlang:put({key,[1,2]},{stored,42}),get_before({state,3});
probe(min_numbers) -> min_before(42,-3);
probe(max_numbers) -> max_before(42,-3);
probe(min_equal_numeric) -> min_before(1,1.0);
probe(max_equal_numeric) -> max_before(1.0,1);
probe(min_general_terms) -> min_before({a,1},[a]);
probe(max_general_terms) -> max_before({a,1},[a]).

get_before(State) ->
    Value=erlang:get({key,[1,2]}),
    try ?MODULE:retain(State,Value) of Result->{Value,Result}
    catch C:R:S->{Value,{caught,C,R,S}} end.

min_before(First,Second) ->
    Value=erlang:min(First,Second),
    try ?MODULE:retain(First,Value) of Result->{Value,Result}
    catch C:R:S->{Value,{caught,C,R,S}} end.

max_before(First,Second) ->
    Value=erlang:max(First,Second),
    try ?MODULE:retain(First,Value) of Result->{Value,Result}
    catch C:R:S->{Value,{caught,C,R,S}} end.

retain(State,Value) -> {State,Value}.
