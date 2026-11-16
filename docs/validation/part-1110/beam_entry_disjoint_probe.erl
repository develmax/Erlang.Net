-module(beam_entry_disjoint_probe).
-export([run/1]).
run(Input) ->
    case source(Input) of
        {first,X,_} -> out(X,first);
        {second,_,Y} -> out(second,Y);
        {both,X,Y} -> out(Y,X);
        {neither,_,_} -> out(neither,neither);
        _ -> missing
    end.
source(Value) -> Value.
out(X,Y) -> {X,Y}.
