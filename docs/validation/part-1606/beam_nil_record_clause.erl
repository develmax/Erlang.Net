-module(beam_nil_record_clause).
-export([run/2]).
-record(entry,{parents}).
run(E = #entry{parents=[]}, S) ->
    {Title, Callback} = read(S),
    Result = consume([E], Callback, [{title,Title}]),
    {Result,S};
run(X, S) -> {X,S}.
read(S) -> S.
consume(A,B,C) -> {A,B,C}.
