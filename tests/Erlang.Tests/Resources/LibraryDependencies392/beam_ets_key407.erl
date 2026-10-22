-module(beam_ets_key407).
-export([run/0,run_stacks/0,worker/2]).

cases() -> [code_server_moddb,code_server_namedb,set_exact,ordered_numeric,bag_unique,duplicate_bag,batch_rollback,public_delete].
run() -> [{Case, observe(Case)} || Case <- cases()].
run_stacks() -> [with_depth(Depth) || Depth <- [default,0,1,2,8]].
with_depth(default) -> {default,run()};
with_depth(Depth) ->
    Old = erlang:system_flag(backtrace_depth,Depth),
    try {Depth,run()} after erlang:system_flag(backtrace_depth,Old) end.
observe(Case) ->
    Parent = self(), Ref = make_ref(), {Pid,Monitor} = spawn_monitor(?MODULE,worker,[{Parent,Ref},Case]),
    receive {Ref,Value} -> receive {'DOWN',Monitor,process,Pid,normal} -> Value end end.
worker({Parent,Ref},Case) ->
    Outcome = try probe(Case) of Value -> {value,Value} catch Class:Reason:Stack -> {caught,Class,Reason,Stack} end,
    Parent ! {Ref,Outcome}.
probe(code_server_moddb) ->
    Name = ets:new(ets_key407,[named_table,protected]), T = ets:whereis(Name),
    true = ets:insert(T,[{module,preloaded},{{sticky,module},true}]),
    Value = {is_reference(T),ets:lookup(Name,module),ets:member(T,{sticky,module})},
    true = ets:delete(T), {Value,ets:whereis(Name)};
probe(code_server_namedb) ->
    T = ets:new(code_names,[]), true = ets:insert(T,{name,directory,base,[]}),
    Value = {is_reference(T),ets:lookup(T,name)}, true = ets:delete(T), Value;
probe(set_exact) -> family(set);
probe(ordered_numeric) -> family(ordered_set);
probe(bag_unique) -> family(bag);
probe(duplicate_bag) -> family(duplicate_bag);
probe(batch_rollback) ->
    T = ets:new(ets_key407,[named_table]), true = ets:insert(T,{key,old}),
    Outcome = try ets:insert(T,[{key,new},bad]) of V -> {returned,V} catch C:R:S -> {caught,C,R,S} end,
    Result = {Outcome,ets:lookup(T,key)}, true = ets:delete(T), Result;
probe(public_delete) ->
    T = ets:new(ets_key407,[public]), Parent=self(), Ref=make_ref(),
    {Pid,Mon}=spawn_monitor(fun() -> Parent ! {Ref,ets:delete(T)} end),
    receive {Ref,V} -> receive {'DOWN',Mon,process,Pid,normal} -> V end end.
family(Type) ->
    T = ets:new(ets_key407,[Type]), true = ets:insert(T,[{1,a},{1.0,b},{1,a}]),
    Result = {ets:lookup(T,1),ets:lookup(T,1.0),ets:member(T,missing)},
    true = ets:delete(T,1), After=ets:lookup(T,1.0), true = ets:delete(T), {Result,After}.
