-module(beam_ms_leaf420).
-export([run/0,run_stacks/0,worker/2,capture/1]).
cases()->[tuple_nested,list_head,list_tail,tuple_element,map_value,map_key,nested_argument,lazy_boolean,multiple_bodies,whole_body_failure,guard_failure,next_clause,empty_body,bad_constructor,bad_program,select_tuple_nested].
run()->[{Name,observe(Name)}||Name<-cases()].
run_stacks()->Old=erlang:system_flag(backtrace_depth,8),erlang:system_flag(backtrace_depth,Old),try [{default,Old,run()}|[depth(D)||D<-[0,1,2,8]]] after erlang:system_flag(backtrace_depth,Old) end.
depth(D)->erlang:system_flag(backtrace_depth,D),Effective=erlang:system_flag(backtrace_depth,D),{D,Effective,run()}.
observe(Name)->{Pid,Ref}=spawn_monitor(?MODULE,worker,[self(),Name]),receive {Name,V}->receive {'DOWN',Ref,process,Pid,normal}->V end after 10000->exit(Pid,kill),receive {'DOWN',Ref,process,Pid,_}->ok end,exit(worker_timeout) end.
worker(Parent,Name)->V=try {value,perform(Name)} catch C:R:S->{exception,C,R,S} end,Parent!{Name,V}.
execute(Guards,Bodies)->M=ets:match_spec_compile([{{'$1','$2'},Guards,Bodies}]),ets:match_spec_run_r([{key,value}],M,[]).
perform(tuple_nested)->execute([],[{{'$1',{'div',1,0}}}]);
perform(list_head)->execute([],[[{'div',1,0},'$1']]);
perform(list_tail)->execute([],[[key|{'div',1,0}]]);
perform(tuple_element)->execute([],[{{'$1',{element,99,'$_'}}}]);
perform(map_value)->execute([],[#{key=>{'div',1,0}}]);
perform(map_key)->execute([],[#{ {'div',1,0}=>value}]);
perform(nested_argument)->execute([],[{{'$1',{'+',{'div',1,0},5}}}]);
perform(lazy_boolean)->execute([],[{{'$1',{'andalso',true,{'div',1,0}}}}]);
perform(multiple_bodies)->execute([],[{'div',1,0},{{'$1',ok}}]);
perform(whole_body_failure)->execute([],[{'div',1,0}]);
perform(guard_failure)->execute([{'div',1,0}],[{{'$1',ok}}]);
perform(next_clause)->M=ets:match_spec_compile([{{'$1','$2'},[{'div',1,0}],[{{'$1',ok}}]},{'_',[],['$_']}]),ets:match_spec_run_r([{key,value}],M,[]);
perform(empty_body)->execute([],[]);
perform(bad_constructor)->execute([],[{'$1',ok}]);
perform(bad_program)->ets:match_spec_compile(bad);
perform(select_tuple_nested)->T=ets:new(ms_leaf420,[]),try true=ets:insert(T,{key,value}),ets:select(T,[{{'$1','$2'},[],[{{'$1',{'div',1,0}}}]}]) after ets:delete(T) end.
capture([Dir])->ok=prim_file:write_file(Dir++"/values.etf",term_to_binary(run(),[{minor_version,2}])),ok=prim_file:write_file(Dir++"/stacks.etf",term_to_binary(run_stacks(),[{minor_version,2}])),erlang:halt(0).
