-module(beam_ets_select409).
-include_lib("stdlib/include/ms_transform.hrl").
-export([run/0,run_stacks/0,worker/2]).

cases() -> [code_server_all_loaded,ordered_projection,ordered_numeric,ordered_keypos,guard_exception,body_exception,false_body,first_clause,empty_spec,bad_spec,bad_table_bad_spec,unbound_body,private_access,empty_table].
run() -> [{Case,observe(Case)} || Case <- cases()].
run_stacks() -> [with_depth(Depth) || Depth <- [default,0,1,2,8]].
with_depth(default) -> {default,run()};
with_depth(Depth) -> Old=erlang:system_flag(backtrace_depth,Depth), try {Depth,run()} after erlang:system_flag(backtrace_depth,Old) end.
observe(Case) ->
    Parent=self(), Ref=make_ref(), {Pid,Mon}=spawn_monitor(?MODULE,worker,[{Parent,Ref},Case]),
    receive {Ref,Value} -> receive {'DOWN',Mon,process,Pid,normal} -> Value end end.
worker({Parent,Ref},Case) ->
    Outcome=try probe(Case) of Value -> {value,Value} catch Class:Reason:Stack -> {caught,Class,Reason,Stack} end,
    Parent ! {Ref,Outcome}.
table(Options) -> ets:new(ets_select409,[named_table|Options]).
whole() -> [{'_',[],['$_']}].
probe(code_server_all_loaded) ->
    T=table([]), true=ets:insert(T,[{module,preloaded},{{sticky,module},true}]),
    Ms=ets:fun2ms(fun({M,_}=Row) when is_atom(M) -> Row end), ets:select(T,Ms);
probe(ordered_projection) ->
    T=table([ordered_set]), true=ets:insert(T,[{3,30},{1,10},{2,20}]),
    ets:select(T,[{{'$1','$2'},[{'>','$2',10}],['$1']}]);
probe(ordered_numeric) ->
    T=table([ordered_set]), true=ets:insert(T,[{3,30},{1,10},{1.0,11},{2,20}]), ets:select(T,whole());
probe(ordered_keypos) ->
    T=table([ordered_set,{keypos,2}]), true=ets:insert(T,[{1,3},{2,1},{3,2}]), ets:select(T,whole());
probe(guard_exception) ->
    T=table([]), true=ets:insert(T,{key,atom}), ets:select(T,[{'_',[{'div',1,0}],['$_']}]);
probe(body_exception) ->
    T=table([]), true=ets:insert(T,{key,atom}), ets:select(T,[{'_',[],[{'div',1,0}]}]);
probe(false_body) ->
    T=table([]), true=ets:insert(T,{key,atom}), ets:select(T,[{'_',[],[false]}]);
probe(first_clause) ->
    T=table([]), true=ets:insert(T,{key,atom}), ets:select(T,[{'_',[],[first]},{'_',[],[second]}]);
probe(empty_spec) -> T=table([]), ets:select(T,[]);
probe(bad_spec) -> T=table([]), ets:select(T,invalid_match_spec409);
probe(bad_table_bad_spec) -> ets:select(ets_missing_select409,invalid_match_spec409);
probe(unbound_body) -> T=table([]), ets:select(T,[{'_',[],['$7']}]);
probe(private_access) ->
    T=table([private]), Parent=self(), Ref=make_ref(),
    {Pid,Mon}=spawn_monitor(fun() ->
        Value=try ets:select(T,invalid_match_spec409) of V -> {value,V} catch C:R:S -> {caught,C,R,S} end,
        Parent ! {Ref,Value}
    end),
    receive {Ref,Value} -> receive {'DOWN',Mon,process,Pid,normal} -> Value end end;
probe(empty_table) -> T=table([]), ets:select(T,whole()).
