-module(otp_receive483_observer).
-export([worker/1]).
worker(P) ->
 <<_:3,Unaligned:16/bitstring,_:5>> = <<7:3,12,34,0:5>>,
 Bs=[<<>>,<<1>>,<<1,2>>,<<1:1>>,<<3:5>>,<<1:9>>,Unaligned,atom,17,[],{tuple,1},#{a=>b}],
 Fs=[fun()->zero end,fun(X)->{one,X} end,fun(X,Y)->{two,X,Y} end,atom,17],
 As=[0,1,2],
 B=[binary_case(B,T)||T<-[0,1],B<-Bs],
 F=[function_case(F,A,T)||T<-[0,1],A<-As,F<-Fs],
 P!{finished,B++F}.
binary_case(B,T) ->
 self()!marker,self()!B,self()!tail,
 R=receive M when is_binary(M)->{selected,M} after T->timeout end,
 {binary,describe(B),T,R,drain()}.
function_case(F,A,T) ->
 self()!marker,self()!F,self()!tail,
 R=select_function(A,T),
 {function,describe(F),A,T,R,drain()}.
select_function(0,T) -> receive M when is_function(M,0)->{selected,describe(M)} after T->timeout end;
select_function(1,T) -> receive M when is_function(M,1)->{selected,describe(M)} after T->timeout end;
select_function(2,T) -> receive M when is_function(M,2)->{selected,describe(M)} after T->timeout end.
drain() -> receive M->[describe(M)|drain()] after 0->[] end.
describe(F) when is_function(F,0)->{fun0,F()};
describe(F) when is_function(F,1)->{fun1,F(argument)};
describe(F) when is_function(F,2)->{fun2,F(left,right)};
describe(V)->V.
