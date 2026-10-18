-module(queue_core_errors).
-export([run/0]).
run()->check([bad,[],{},{[]},{[],[],[]},{bad,[]},{[],bad}]), {error,badarg}=capture(fun()->queue:from_list(bad) end), done.
capture(F)->try F() of V->{ok,V} catch C:R->{C,R} end.
check([])->ok; check([Q|Qs])->false=queue:is_queue(Q), {error,badarg}=capture(fun()->queue:is_empty(Q) end), {error,badarg}=capture(fun()->queue:len(Q) end), {error,badarg}=capture(fun()->queue:in(x,Q) end), {error,badarg}=capture(fun()->queue:out(Q) end), {error,badarg}=capture(fun()->queue:to_list(Q) end), check(Qs).