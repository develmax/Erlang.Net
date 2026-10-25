-module(otp_frame_comparisons482_observer).
-export([worker/1]).
worker(P)->R=[one(Op,Name,B,V)||Op<-[lt,le,gt,ge,eq,ne,exact_eq,exact_ne],{Name,V}<-[{less,9999},{equal,10000},{greater,10001},{float_equal,10000.0},{float_greater,10000.5},{atom,hello},{tuple,{a,1}},{map,#{a=>1}},{list,[1,2]},{huge,1 bsl 120}],B<-[<<>>,<<1,2,3>>]],P!{finished,R}.
one(Op,Name,B,V)->R=try erlang:apply(otp_frame_comparisons482,Op,[B,V]) of X->{returned,X} catch C:E:S->{caught,C,E,S} end,{Op,Name,B,V,R}.
