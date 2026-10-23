-module(beam_tuple_receive454).
-export([run/0]).

run()->{[match(Value)||Value<-inputs()],[match_proven(Value)||Value<-inputs()]}.

inputs()->[{}, {one}, {one,two}, {one,two,three}, bad, [], <<>>, <<1>>].

match(Input)->
    self()!{candidate,Input},
    self()!stop,
    receive
        {candidate,Value} when tuple_size(Value)>=2 -> {matched,Value,drain()};
        stop -> {skipped,drain()}
    end.

match_proven(Input)->
    self()!{candidate,Input},
    self()!stop,
    receive
        {candidate,Value} when is_tuple(Value), tuple_size(Value)>=2 -> {matched,Value,drain()};
        stop -> {skipped,drain()}
    end.

drain()->receive Message->Message after 0->empty end.
