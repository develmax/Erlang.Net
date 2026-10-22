-module(beam_dist_decode423).
-export([run/0,run_stacks/0,worker/2,capture/1]).
cases()->[plain_integer,plain_atom,plain_tuple,cache_first,cache_second,cache_last,cache_nested,cache_unicode,empty_cache_miss,cache_outside,cache_index255,oversize_cache,bad_cache_type,bad_cache_member,bad_binary,empty_binary,header_only,version,compressed,truncated_cache,tuple_partial,bit_tail,trailing,unaligned,pid_cached,port_cached,ref_cached,pid_old_cached,export_value,export_call,local_term,local_identifiers,local_function,local_bad_hash,local_trailing,node_cache_bad,static_local_fun_refusal,static_normal_fun].
run()->[{Name,observe(Name)}||Name<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(Name)->{Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),Name]),receive {Name,V}->receive {'DOWN',Ref,process,Pid,normal}->V end after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,Name)->V=try {value,perform(Name)} catch C:R:S->{exception,C,R,S} end,Parent!{Name,V}.
d(C,B)->erts_debug:dist_ext_to_term(C,B).
perform(plain_integer)->d({},<<131,97,7>>);
perform(plain_atom)->d({},<<131,119,1,97>>);
perform(plain_tuple)->d({},<<131,104,2,97,7,106>>);
perform(cache_first)->d({a,b},<<131,82,0>>);
perform(cache_second)->d({a,b},<<131,82,1>>);
perform(cache_last)->d(erlang:make_tuple(255,last),<<131,82,254>>);
perform(cache_nested)->d({a,b},<<131,104,3,82,0,108,0,0,0,2,82,1,82,0,106,116,0,0,0,1,82,1,82,0>>);
perform(cache_unicode)->d({'λ'},<<131,82,0>>);
perform(empty_cache_miss)->d({},<<131,82,0>>);
perform(cache_outside)->d({a},<<131,82,1>>);
perform(cache_index255)->d(erlang:make_tuple(255,a),<<131,82,255>>);
perform(oversize_cache)->d(erlang:make_tuple(256,a),<<131,97,7>>);
perform(bad_cache_type)->d([a],<<131,97,7>>);
perform(bad_cache_member)->d({a,42},<<131,97,7>>);
perform(bad_binary)->d({},[131,97,7]);
perform(empty_binary)->d({},<<>>);
perform(header_only)->d({},<<131>>);
perform(version)->d({},<<130,97,7>>);
perform(compressed)->d({},<<131,80,0,0,0,1,120,156,75,4,0,0,98,0,98>>);
perform(truncated_cache)->d({a},<<131,82>>);
perform(tuple_partial)->d({},<<131,104,2,97,1>>);
perform(bit_tail)->d({},<<131,97,7,1:1>>);
perform(trailing)->d({a},<<131,82,0,255,82,255>>);
perform(unaligned)->B= <<131,82,0>>,N=byte_size(B),Whole= <<0:1,B/binary,0:7>>,<<_:1,X:N/binary,_:7>>=Whole,d({a},X);
perform(pid_cached)->d({'dist423@node'},<<131,88,82,0,1:32,2:32,7:32>>);
perform(port_cached)->d({'dist423@node'},<<131,120,82,0,21:64,7:32>>);
perform(ref_cached)->d({'dist423@node'},<<131,90,3:16,82,0,7:32,1:32,2:32,3:32>>);
perform(pid_old_cached)->d({'dist423@node'},<<131,103,82,0,1:32,2:32,3>>);
perform(export_value)->d({erlang,abs},<<131,113,82,0,82,1,97,1>>);
perform(export_call)->F=d({erlang,abs},<<131,113,82,0,82,1,97,1>>),F(-3);
perform(local_term)->T={a,[b,7],#{c=>8}},d({},term_to_binary(T,[local]))=:=T;
perform(local_identifiers)->T={self(),make_ref()},d({},term_to_binary(T,[local]))=:=T;
perform(local_function)->F=fun(X)->{x,X} end,B=term_to_binary(F,[local]),try d({},B) of G->{accepted,G=:=F} catch error:badarg:S->{rejected,case S of [{erts_debug,dist_ext_to_term,[{},B],[]}|_]->true;_->false end} end;
perform(local_bad_hash)-><<131,121,H:32,Rest/binary>>=term_to_binary({a,7},[local]),B= <<131,121,(H bxor 1):32,Rest/binary>>,try d({},B),false catch error:badarg->true end;
perform(local_trailing)->T={a,7},B=term_to_binary(T,[local]),d({},<<B/binary,255,82,255>>)=:=T;
perform(node_cache_bad)->d({'dist423@node'},<<131,88,82,1,1:32,2:32,7:32>>);
perform(static_local_fun_refusal)->d({},<<131,121,104,144,50,150,112,0,0,0,81,1,49,72,11,244,24,192,63,115,122,255,2,245,199,215,176,124,0,0,0,0,0,0,0,0,119,19,98,101,97,109,95,100,105,115,116,95,100,101,99,111,100,101,52,50,51,121,88,84,39,164,97,0,121,84,166,240,78,98,1,138,64,95,88,106,0,0,0,0,0,0,0,0,0,0,0,0>>);
perform(static_normal_fun)->F=d({},<<131,112,0,0,0,85,1,49,72,11,244,24,192,63,115,122,255,2,245,199,215,176,124,0,0,0,0,0,0,0,0,119,19,98,101,97,109,95,100,105,115,116,95,100,101,99,111,100,101,52,50,51,97,0,98,1,138,64,95,88,119,13,110,111,110,111,100,101,64,110,111,104,111,115,116,0,0,0,0,0,0,0,0,0,0,0,0>>),{is_function(F,1),erlang:fun_info(F,module)}.
capture([Dir])->ok=prim_file:write_file(Dir++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(Dir++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
