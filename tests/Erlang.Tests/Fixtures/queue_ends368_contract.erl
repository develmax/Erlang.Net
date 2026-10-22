-module(queue_ends368_contract).
-export([run/0]).

run() ->
    Q = {[3,2],[1]},
    [queue:in_r(0,Q), queue:out_r(Q),
     queue:get(Q), queue:get_r(Q), queue:peek(Q), queue:peek_r(Q),
     queue:drop(Q), queue:drop_r(Q), queue:cons(0,Q), queue:head(Q),
     queue:tail(Q), queue:snoc(Q,4), queue:last(Q), queue:daeh(Q),
     queue:init(Q), queue:liat(Q), erlang:apply(queue,lait,[Q])].
