-module(otp_receive_length484_observer).
-export([worker/1]).
worker(P) ->
 Ls=[[],[a],[a,b],[a,b,c],[a|bad],bad,<<1>>,{},#{},seq(15),seq(127),seq(4096)],
 Ns=[-1,0,1,2,3,4,5,10,100,1.0,2.0,bad],
 R=[one(Kind,L,N,T)||Kind<-[stable,candidate],T<-[0,1],N<-Ns,L<-Ls],
 P!{finished,R}.
seq(0)->[0];
seq(N)->[N|seq(N-1)].
one(Kind,L,N,T) ->
 self()!marker,self()!{candidate,L},self()!tail,
 R=case Kind of stable->stable(L,N,T);candidate->candidate(N,T) end,
 {Kind,L,N,T,R,drain()}.
stable(L,N,T) ->
 receive
  {candidate,V} when length(L)+1 >= N->{selected,V};
  {candidate,V}->{fallback,V}
 after T->timeout end.
candidate(N,T) ->
 try receive
  {candidate,V} when length(V)+1 >= N->{selected,V};
  {candidate,V}->{fallback,V}
 after T->timeout end
 catch C:R:S->{caught,C,R,S} end.
drain() -> receive V->[V|drain()] after 0->[] end.
