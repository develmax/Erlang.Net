-module(beam_ets_compressed_subject442).
-export([cases/0,worker/2]).
cases()->[set_case,ordered_case,bag_case,duplicate_case,keypos_case,repeat_flag,
 complex_value,binary_value,bit_value,identity_value,local_fun,external_fun,
 update_element,update_counter,take_case,delete_object,select_case,match_case,
 ordered_numeric,bag_duplicates,fix_case,bad_tuple_flag,bad_keypos,bad_option,
 improper_options,bad_flag_arity].
worker(Parent,Case)->
 Outcome=try probe(Case) of V->{returned,V} catch K:R:S->{raised,K,R,S} end,
 Parent!{Case,Outcome}.
probe(set_case)->table([compressed,set],basic);
probe(ordered_case)->table([compressed,ordered_set],basic);
probe(bag_case)->table([compressed,bag],basic);
probe(duplicate_case)->table([compressed,duplicate_bag],basic);
probe(keypos_case)->table([compressed,{keypos,2}],keypos);
probe(repeat_flag)->table([compressed,compressed],basic);
probe(complex_value)->table([compressed],complex);
probe(binary_value)->table([compressed],binary);
probe(bit_value)->table([compressed],bits);
probe(identity_value)->table([compressed],identity);
probe(local_fun)->table([compressed],local_function);
probe(external_fun)->table([compressed],external_function);
probe(update_element)->table([compressed],element);
probe(update_counter)->table([compressed],counter);
probe(take_case)->table([compressed],take);
probe(delete_object)->table([compressed,bag],delete_object);
probe(select_case)->table([compressed],select);
probe(match_case)->table([compressed],match);
probe(ordered_numeric)->table([compressed,ordered_set],numeric);
probe(bag_duplicates)->table([compressed,duplicate_bag],duplicates);
probe(fix_case)->table([compressed],fix);
probe(bad_tuple_flag)->ets:new(compressed442,[{compressed,false}]);
probe(bad_keypos)->ets:new(compressed442,[compressed,{keypos,0}]);
probe(bad_option)->ets:new(compressed442,[compressed,unknown_compressed442]);
probe(improper_options)->ets:new(compressed442,[compressed|bad]);
probe(bad_flag_arity)->ets:new(compressed442,[{compressed}]).
table(Options,Mode)->
 T=ets:new(compressed442,Options),
 try {ets:info(T,compressed),exercise(T,Mode),ets:info(T,size)}
 after true=ets:delete(T) end.
exercise(T,basic)->true=ets:insert(T,{key,{nested,[1,2,3]}}),ets:lookup(T,key);
exercise(T,keypos)->true=ets:insert(T,{value,key,[1,2]}),ets:lookup(T,key);
exercise(T,complex)->V={#{a=>[1,2],b=>{3,4}},[alpha,beta|tail]},true=ets:insert(T,{key,V}),ets:lookup(T,key);
exercise(T,binary)->true=ets:insert(T,{key,<<0:8192>>}),[{key,B}]=ets:lookup(T,key),{byte_size(B),B=:= <<0:8192>>};
exercise(T,bits)->true=ets:insert(T,{key,<<5:3,7:5,3:2>>}),ets:lookup(T,key);
exercise(T,identity)->R=make_ref(),P=self(),true=ets:insert(T,{key,R,P}),[{key,R2,P2}]=ets:lookup(T,key),{R=:=R2,P=:=P2};
exercise(T,local_function)->N=17,F=fun(X)->X+N end,true=ets:insert(T,{key,F}),[{key,G}]=ets:lookup(T,key),{is_function(G,1),G(5)};
exercise(T,external_function)->F=fun erlang:abs/1,true=ets:insert(T,{key,F}),[{key,G}]=ets:lookup(T,key),G(-9);
exercise(T,element)->true=ets:insert(T,{key,1,old}),true=ets:update_element(T,key,[{2,8},{3,new}]),ets:lookup(T,key);
exercise(T,counter)->true=ets:insert(T,{key,3}),R=ets:update_counter(T,key,{2,4}),{R,ets:lookup(T,key)};
exercise(T,take)->true=ets:insert(T,{key,7}),{ets:take(T,key),ets:lookup(T,key)};
exercise(T,delete_object)->true=ets:insert(T,[{key,1},{key,2}]),true=ets:delete_object(T,{key,1}),ets:lookup(T,key);
exercise(T,select)->true=ets:insert(T,{key,9}),ets:select(T,[{{key,'$1'},[],['$1']}]);
exercise(T,match)->true=ets:insert(T,{key,9}),ets:match(T,{key,'$1'});
exercise(T,numeric)->true=ets:insert(T,[{1,first},{1.0,second}]),ets:lookup(T,1);
exercise(T,duplicates)->true=ets:insert(T,[{key,1},{key,1}]),ets:lookup(T,key);
exercise(T,fix)->true=ets:insert(T,{key,1}),true=ets:safe_fixtable(T,true),R=ets:lookup(T,key),true=ets:safe_fixtable(T,false),R.
