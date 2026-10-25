-module(otp_native485_map_protocol).
-export([worker/1]).
worker(P)->
 M=#{a=>1,b=>2},K=erts_internal:map_to_tuple_keys(M),K2=erts_internal:map_to_tuple_keys(M),
 {IK,IV}=iter(maps:iterator(M)),
 V=[maps:get(Key,M)||Key<-tuple_to_list(K)],
 R={tuple_size(K)=:=map_size(M),K=:=list_to_tuple(IK),V=:=IV,erts_debug:same(K,K2)},
 P!{finished,R,K}.
iter(I)->case maps:next(I) of none->{[],[]};{K,V,N}->{Ks,Vs}=iter(N),{[K|Ks],[V|Vs]} end.
