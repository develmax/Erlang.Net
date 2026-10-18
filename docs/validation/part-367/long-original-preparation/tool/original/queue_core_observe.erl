-module(queue_core_observe).
-export([run/0]).
capture(F)->try F() of V->{ok,V} catch C:R->{C,R} end.
raw()->[bad,{}, {[],[]},{[a],[]},{[b,a],[]},{[],[a,b]},{[d,c],[a,b]},
 {[a|tail],[]},{[],[a|tail]},{[b|tail],[a]},{[b|tail],[a,c|tail]},{bad,[a,b]},{[a],bad}].
run()->
 [{new,queue:new()},
  {is_queue,[{Q,capture(fun()->queue:is_queue(Q) end)} || Q<-raw()]},
  {is_empty,[{Q,capture(fun()->queue:is_empty(Q) end)} || Q<-raw()]},
  {len,[{Q,capture(fun()->queue:len(Q) end)} || Q<-raw()]},
  {in,[{Q,capture(fun()->queue:in(x,Q) end)} || Q<-raw()]},
  {out,[{Q,capture(fun()->queue:out(Q) end)} || Q<-raw()]},
  {from_list,[{L,capture(fun()->queue:from_list(L) end)} || L<-[[],[a],[a,b],[a,b,c,d,e],[a|tail],tail]]},
  {to_list,[{Q,capture(fun()->queue:to_list(Q) end)} || Q<-raw()]}].