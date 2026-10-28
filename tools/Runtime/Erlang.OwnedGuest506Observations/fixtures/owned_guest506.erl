-module(owned_guest506).
-export([run/1, held/1, make/1, capture/1, failure/1]).
run(X) -> A = {X, [1,2]}, owned_probe506:live(A), put(key,A), get(key).
held(X) -> A = {X, [1,2]}, owned_probe506:held(A), A.
make(X) -> A = {X, [1,2]}, fun() -> A end.
capture(X) -> A = {X, [1,2]}, owned_probe506:capture(A), A.
failure(X) -> erlang:error({X,[1,2]}).
