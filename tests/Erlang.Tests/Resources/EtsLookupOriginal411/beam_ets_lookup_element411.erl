-module(beam_ets_lookup_element411).
-export([run/0,run_stacks/0,worker/2]).
cases() -> [set_found,set_default_found,missing3,missing4,missing_large_position,position_zero,position_negative,position_atom,position_float,position_bignum,position_intmax,short3,short4,bag_values,duplicate_values,bag_missing3,bag_missing4,bag_short,duplicate_short,ordered_numeric,set_numeric,ordered_replaced,keypos,default_composite,private_access,private_before_position,protected_read,missing_table_bad_position,bad_table_type,deleted_table,copy_fun,copy_binary,application_env,application_env_missing,wrapped_set_second,wrapped_set_key,wrapped_ordered,wrapped_bag,wrapped_over_arity,wrapped_negative,wrapped_missing,wrapped_negative_missing].
run() -> [{C,observe(C)} || C<-cases()].
run_stacks() -> [depth(D) || D<-[default,0,1,2,8]].
depth(default) -> {default,run()};
depth(D) -> Old=erlang:system_flag(backtrace_depth,D),try {D,run()} after erlang:system_flag(backtrace_depth,Old) end.
observe(C) -> P=self(),R=make_ref(),{Pid,M}=spawn_monitor(?MODULE,worker,[{P,R},C]),receive {R,V}->receive {'DOWN',M,process,Pid,normal}->V end end.
worker({P,R},C) -> V=try probe(C) of X->{value,X} catch K:E:S->{caught,K,E,S} end,P!{R,V}.
setup(Opts,Rows) -> T=ets:new(ets_lookup_element411,[named_table|Opts]),true=ets:insert(T,Rows),T.
other(F) -> P=self(),R=make_ref(),{Pid,M}=spawn_monitor(fun()->V=try F() of X->{value,X} catch K:E:S->{caught,K,E,S} end,P!{R,V} end),receive {R,V}->receive {'DOWN',M,process,Pid,normal}->case V of {value,X}->X;{caught,K,E,S}->erlang:raise(K,E,S) end end end.
probe(set_found) -> setup([], [{key,10}]),ets:lookup_element(ets_lookup_element411,key,2);
probe(set_default_found) -> setup([], [{key,10}]),ets:lookup_element(ets_lookup_element411,key,2,fallback);
probe(missing3) -> setup([], []),ets:lookup_element(ets_lookup_element411,key,2);
probe(missing4) -> setup([], []),ets:lookup_element(ets_lookup_element411,key,2,fallback);
probe(missing_large_position) -> setup([], []),ets:lookup_element(ets_lookup_element411,key,999,fallback);
probe(position_zero) -> setup([], []),ets:lookup_element(ets_lookup_element411,key,0,fallback);
probe(position_negative) -> setup([], []),ets:lookup_element(ets_lookup_element411,key,-1);
probe(position_atom) -> setup([], []),ets:lookup_element(ets_lookup_element411,key,bad,fallback);
probe(position_float) -> setup([], [{key,10}]),ets:lookup_element(ets_lookup_element411,key,2.0);
probe(position_bignum) -> setup([], []),ets:lookup_element(ets_lookup_element411,key,1 bsl 100,fallback);
probe(position_intmax) -> setup([], [{key,10}]),ets:lookup_element(ets_lookup_element411,key,2147483647);
probe(short3) -> setup([], [{key}]),ets:lookup_element(ets_lookup_element411,key,2);
probe(short4) -> setup([], [{key}]),ets:lookup_element(ets_lookup_element411,key,2,fallback);
probe(bag_values) -> setup([bag], [{key,10},{key,20},{key,10}]),ets:lookup_element(ets_lookup_element411,key,2);
probe(duplicate_values) -> setup([duplicate_bag], [{key,10},{key,10},{key,20}]),ets:lookup_element(ets_lookup_element411,key,2,fallback);
probe(bag_missing3) -> setup([bag], []),ets:lookup_element(ets_lookup_element411,key,2);
probe(bag_missing4) -> setup([bag], []),ets:lookup_element(ets_lookup_element411,key,2,fallback);
probe(bag_short) -> setup([bag], [{key,10},{key}]),ets:lookup_element(ets_lookup_element411,key,2);
probe(duplicate_short) -> setup([duplicate_bag], [{key},{key,20}]),ets:lookup_element(ets_lookup_element411,key,2,fallback);
probe(ordered_numeric) -> setup([ordered_set], [{1,10}]),ets:lookup_element(ets_lookup_element411,1.0,2);
probe(set_numeric) -> setup([], [{1,10}]),ets:lookup_element(ets_lookup_element411,1.0,2,fallback);
probe(ordered_replaced) -> setup([ordered_set], [{1,10},{1.0,20}]),ets:lookup_element(ets_lookup_element411,1,1);
probe(keypos) -> setup([{keypos,2}], [{10,key}]),ets:lookup_element(ets_lookup_element411,key,1);
probe(default_composite) -> setup([], []),ets:lookup_element(ets_lookup_element411,key,2,{fallback,<<1,2>>});
probe(private_access) -> setup([private], [{key,10}]),other(fun()->ets:lookup_element(ets_lookup_element411,key,2) end);
probe(private_before_position) -> setup([private], [{key,10}]),other(fun()->ets:lookup_element(ets_lookup_element411,key,0,fallback) end);
probe(protected_read) -> setup([protected], [{key,10}]),other(fun()->ets:lookup_element(ets_lookup_element411,key,2) end);
probe(missing_table_bad_position) -> ets:lookup_element(ets_missing_lookup_element411,key,0,fallback);
probe(bad_table_type) -> ets:lookup_element(42,key,0);
probe(deleted_table) -> setup([], [{key,10}]),ets:delete(ets_lookup_element411),ets:lookup_element(ets_lookup_element411,key,2,fallback);
probe(copy_fun) -> N=40,setup([], [{key,fun(X)->X+N end}]),F=ets:lookup_element(ets_lookup_element411,key,2),ets:delete(ets_lookup_element411),F(2);
probe(copy_binary) -> setup([], [{key,binary:copy(<<7>>,256)}]),B=ets:lookup_element(ets_lookup_element411,key,2),ets:delete(ets_lookup_element411),{byte_size(B),binary:at(B,255)};
probe(application_env) -> setup([], [{{env,kernel,key},10}]),ets:lookup_element(ets_lookup_element411,{env,kernel,key},2,undefined);
probe(application_env_missing) -> setup([], []),ets:lookup_element(ets_lookup_element411,{env,kernel,key},2,undefined);
probe(wrapped_set_second) -> setup([], [{key,10}]),ets:lookup_element(ets_lookup_element411,key,(1 bsl 32)+2);
probe(wrapped_set_key) -> setup([], [{key,10}]),ets:lookup_element(ets_lookup_element411,key,(1 bsl 32)+1);
probe(wrapped_ordered) -> setup([ordered_set], [{key,10}]),ets:lookup_element(ets_lookup_element411,key,(1 bsl 32)+2,fallback);
probe(wrapped_bag) -> setup([bag], [{key,10},{key,20}]),ets:lookup_element(ets_lookup_element411,key,(1 bsl 32)+2);
probe(wrapped_over_arity) -> setup([], [{key,10}]),ets:lookup_element(ets_lookup_element411,key,(1 bsl 32)+3);
probe(wrapped_negative) -> setup([], [{key,10}]),ets:lookup_element(ets_lookup_element411,key,(1 bsl 31)+1);
probe(wrapped_missing) -> setup([], []),ets:lookup_element(ets_lookup_element411,key,(1 bsl 32)+2,fallback);
probe(wrapped_negative_missing) -> setup([], []),ets:lookup_element(ets_lookup_element411,key,(1 bsl 31)+1,fallback).
