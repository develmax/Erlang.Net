-module(integer_guard_range_probe).
-export([low/2, high/2, exported/3, indirect/0, indirect_low/2, indirect_high/2, target/1]).
low(A, R) -> route(A, R, 1).
high(A, R) -> route(A, R, 2).
route(A, {tag,N}, Level) when N >= Level -> target({wrapped,A});
route(_,_,_) -> no.
exported(A, {tag,N}, Level) when N >= Level -> target({wrapped,A});
exported(_,_,_) -> no.
indirect() -> fun indirect_route/3.
indirect_low(A, R) -> indirect_route(A, R, 1).
indirect_high(A, R) -> indirect_route(A, R, 2).
indirect_route(A, {tag,N}, Level) when N >= Level -> target({wrapped,A});
indirect_route(_,_,_) -> no.
target(A) -> A.
