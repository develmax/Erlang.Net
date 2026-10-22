-module(queue_core_improper).
-export([run/0]).
run()->Q={[a|tail],[]}, true=queue:is_queue(Q), false=queue:is_empty(Q), {[x,a|tail],[]}=queue:in(x,Q), {{value,a},{[],tail}}=queue:out({[],[a|tail]}), {{value,a},{[b|tail],[c|tail]}}=queue:out({[b|tail],[a,c|tail]}), {error,badarg}=capture(fun()->queue:len(Q) end), {error,badarg}=capture(fun()->queue:to_list(Q) end), {error,badarg}=capture(fun()->queue:out(Q) end), {error,badarg}=capture(fun()->queue:from_list([a|tail]) end), done.
capture(F)->try F() of V->{ok,V} catch C:R->{C,R} end.