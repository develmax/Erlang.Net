-module(otp_native485_indexed).
-export([worker/1]).
worker({P,I})->P!{finished,I,one(I)}.
one(1)->T=ets:new(native_profile485,[set,named_table,public]),[{new,T},{insert,ets:insert(T,{key,1})},{lookup,ets:lookup(T,key)},{counter,ets:update_counter(T,key,2)},{member,ets:member(T,key)},{delete,ets:delete(T)},{info,ets:info(T)}];
one(2)->cap(fun()->re:run("ab","a(.)",[{capture,all,binary}])end);
one(3)->cap(fun()->re:run("xy","a(.)",[{capture,all,binary}])end);
one(4)->cap(fun()->re:run("ab","a(.)",[])end);
one(5)->cap(fun()->re:run("ab","(",[])end);
one(6)->cap(fun()->erl_ddll:format_error(no_driver)end);
one(7)->cap(fun()->erl_ddll:info("missing485",processes)end);
one(8)->cap(fun()->erts_internal:term_type({a,b})end);
one(9)->cap(fun()->erts_internal:cmp_term({a,1},{a,2})end);
one(10)->cap(fun()->erts_internal:map_to_tuple_keys(#{a=>1,b=>2})end).
cap(F)->try F() of V->{ok,V} catch C:E:S->{caught,C,E,S} end.
