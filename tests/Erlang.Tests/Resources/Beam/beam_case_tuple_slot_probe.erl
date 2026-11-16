-module(beam_case_tuple_slot_probe).
-export([run/2, retain/2]).
-record(state, {payload, rule}).

run(File, Payload) ->
    Rule = normalize(Payload),
    State = #state{payload=Payload, rule=Rule},
    case select(File) of
        yes -> State;
        no -> no
    end.

retain(File, Payload) ->
    Rule = normalize(Payload),
    State = #state{payload=Payload, rule=Rule},
    case select(File) of
        yes -> {State, Rule};
        no -> no
    end.

normalize(Payload) -> {rule,Payload}.
select(1) -> yes;
select(_) -> no.
