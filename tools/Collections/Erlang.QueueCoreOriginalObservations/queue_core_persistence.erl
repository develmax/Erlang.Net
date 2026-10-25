-module(queue_core_persistence).
-export([run/0]).
run()->Q0=queue:new(), Q1=queue:in(a,Q0), Q2=queue:in(b,Q1), Q3=queue:in(c,Q2), {[c,b],[a]}=Q3, {{value,a},{[c],[b]}}=queue:out(Q3), [a,b,c]=queue:to_list(Q3), {[a],[]}=Q1, 3=queue:len(Q3), done.