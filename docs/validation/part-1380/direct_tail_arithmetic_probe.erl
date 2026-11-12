-module(direct_tail_arithmetic_probe).
-export([count/3, index/3, forward/2, seeded/1]).

count([_ | Rest], Ext, Root) ->
    count(Rest, Ext + 1, Root);
count([], Ext, Root) ->
    [0, Ext, Root].

index(Name, [{entry, Name} | _], N) ->
    N;
index(Name, [_ | Rest], N) ->
    index(Name, Rest, N + 1);
index(Name, [], _) ->
    missing(Name, {unknown, Name}).

forward(A, B) ->
    finish(B + 1, A, B).

finish(C, A, B) ->
    {C, A, B}.

missing(Name, Reason) ->
    {Name, Reason}.

seeded(List) ->
    seed_step(List, 0).

seed_step(List, Count) ->
    seed_count(List, Count).

seed_count([_ | Rest], Count) ->
    seed_count(Rest, Count + 1);
seed_count([], Count) ->
    Count.
