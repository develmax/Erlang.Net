-module(queue_core_heterogeneous).
-export([run/0]).
run()->L=[a,1,1.0,#{x=>[x|tail]},<<1:1>>,{x,[]}], Q=queue:from_list(L), 6=queue:len(Q), L=queue:to_list(Q), true=queue:is_queue(Q), false=queue:is_empty(Q), done.