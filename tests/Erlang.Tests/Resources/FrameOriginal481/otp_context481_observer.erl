-module(otp_context481_observer).
-export([worker/1,run/0,decode/1]).
-file("otp_context481_observer.erl",1).
worker(Parent)->Parent!{finished,run()}.
run()->[one(B)||B<-[<<0>>,<<0,1>>,<<0,1,2>>,<<1>>,<<1,42>>,<<2>>,<<2,0,255>>,<<3,1,2>>,<<9>>,bad]].
one(B)->try decode(B) of R->{B,returned,R} catch C:R:S->{B,caught,C,R,S} end.
decode(<<0,Rest/binary>>)->forward_nil(Rest);
decode(<<1,Rest/binary>>)->forward_atom(Rest);
decode(<<2,Rest/binary>>)->forward_literal(Rest);
decode(<<3,Rest/binary>>)->forward_integer(Rest).
forward_nil(Bin)->loop(Bin,[]).
forward_atom(Bin)->loop(Bin,seed).
forward_literal(Bin)->loop(Bin,[seed,3]).
forward_integer(Bin)->loop(Bin,7).
loop(<<>>,Acc)->Acc;
loop(<<V:8,Rest/binary>>,Acc)->loop(Rest,[V|Acc]).
