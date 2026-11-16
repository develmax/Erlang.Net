-module(beam_dispatch_probe).
-export([dispatch/1, numeric/1, pair/1, remote/1, ordinary/1, reverse/1, checked/1, grouped_checked/1]).
dispatch(z) -> 1;
dispatch(a) -> 2;
dispatch(m) -> 3;
dispatch(_) -> 4.
numeric(8) -> a;
numeric(2) -> b;
numeric(4) -> c;
numeric(_) -> d.
pair(b) -> second;
pair(a) -> first;
pair(_) -> other.
remote(X) ->
    io:write(X).
ordinary(X) ->
    io:write(X),
    X.

reverse(X) -> lists:reverse(X).

checked(allowed) -> true;
checked(_) -> erlang:error({badtype, ?MODULE}).

grouped_checked(z) -> true;
grouped_checked(b) -> true;
grouped_checked(a) -> true;
grouped_checked(m) -> true;
grouped_checked(_) -> erlang:error({badtype, ?MODULE}).
