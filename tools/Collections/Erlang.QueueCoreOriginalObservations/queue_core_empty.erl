-module(queue_core_empty).
-export([run/0]).
run()->Q=queue:new(), {[],[]}=Q, true=queue:is_queue(Q), true=queue:is_empty(Q), 0=queue:len(Q), []=queue:to_list(Q), {empty,Q}=queue:out(Q), done.