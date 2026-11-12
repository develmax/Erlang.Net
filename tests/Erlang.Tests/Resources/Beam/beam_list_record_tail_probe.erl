-module(beam_list_record_tail_probe).
-export([run/2, unchecked/2]).
-record(state, {a, b, c, active=false}).

run(Items, Data) ->
    State = {state,Data,undefined,undefined,false},
    {State,forward(Items, State)}.

forward(Items, State) ->
    collect(Items, State, []).

collect([marker|Rest], State, Acc) ->
    collect(Rest, State#state{active=true}, Acc);
collect([Head|Rest], State, Acc) ->
    collect(Rest, State, [Head|Acc]);
collect([], State, Acc) ->
    {Acc, State}.

unchecked(Items, State) ->
    collect_unchecked(Items, State, []).

collect_unchecked([marker|Rest], State, Acc) ->
    collect_unchecked(Rest, State#state{active=true}, Acc);
collect_unchecked([Head|Rest], State, Acc) ->
    collect_unchecked(Rest, State, [Head|Acc]);
collect_unchecked([], State, Acc) ->
    {Acc, State}.
