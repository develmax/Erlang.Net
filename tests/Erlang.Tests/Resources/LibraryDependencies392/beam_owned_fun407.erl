-module(beam_owned_fun407).
-export([cases/0,run/0,run_stacks/0,worker/1,make/1,invoke/2]).

cases() -> [compound,named,external,info,roundtrip,legacy_loaded,empty_env,raise,badarity_zero,badarity_two,badfun].

run() ->
    {Child,Ref} = erlang:spawn_monitor(?MODULE,worker,[self()]),
    receive
        {Child,Values} ->
            receive
                {'DOWN',Ref,process,Child,normal} -> Values;
                {'DOWN',Ref,process,Child,Reason} -> erlang:error({child_exit,Reason})
            after 5000 -> erlang:error(child_not_settled)
            end;
        {'DOWN',Ref,process,Child,Reason} -> erlang:error({child_exit,Reason})
    after 30000 -> erlang:exit(Child,kill),erlang:error(child_timeout)
    end.

worker(Owner) -> Owner ! {self(),rows(cases())},ok.
rows([]) -> [];
rows([Case|Rest]) -> Outcome=observe(Case),[{Case,Outcome}|rows(Rest)].
observe(Case) ->
    try value(Case) of Value -> {returned,Value}
    catch Class:Reason:Stack -> {caught,Class,Reason,Stack}
    end.

run_stacks() ->
    Old=erlang:system_flag(backtrace_depth,8),
    erlang:system_flag(backtrace_depth,Old),
    try [{default,run()},at_depth(0),at_depth(1),at_depth(2),at_depth(8)]
    after erlang:system_flag(backtrace_depth,Old)
    end.
at_depth(Depth) -> erlang:system_flag(backtrace_depth,Depth),{Depth,run()}.

make(Environment) -> fun(Argument) -> {Environment,Argument} end.
invoke(Function,Argument) -> Function(Argument).
named(Argument) -> {named,Argument}.

value(compound) ->
    Shared={shared,<<1,2,3>>},Env={Shared,Shared,#{key=>[1,2,3]}},
    {Env,marker}=invoke(make(Env),marker),true;
value(named) -> {named,marker}=invoke(fun named/1,marker),true;
value(external) -> 3=invoke(fun erlang:length/1,[a,b,c]),true;
value(info) ->
    Env={environment,<<4,5,6>>},F=make(Env),
    {arity,1}=erlang:fun_info(F,arity),{type,local}=erlang:fun_info(F,type),
    {module,?MODULE}=erlang:fun_info(F,module),{env,[Env]}=erlang:fun_info(F,env),
    [erlang:fun_info(F,arity),erlang:fun_info(F,type),erlang:fun_info(F,module),
        erlang:fun_info(F,name),erlang:fun_info(F,index),erlang:fun_info(F,new_index),
        erlang:fun_info(F,uniq),erlang:fun_info(F,new_uniq),erlang:fun_info(F,env)];
value(roundtrip) ->
    F=make({retained,<<1,0,7>>}),G=binary_to_term(term_to_binary(F)),
    true=F=:=G,true=invoke(F,marker)=:=invoke(G,marker),true;
value(legacy_loaded) ->
    F=make({retained,legacy}),
    {module,Module}=erlang:fun_info(F,module),
    {index,OldIndex}=erlang:fun_info(F,index),{uniq,OldUniq}=erlang:fun_info(F,uniq),
    <<131,ModuleEncoding/binary>>=term_to_binary(Module),
    <<131,OldIndexEncoding/binary>>=term_to_binary(OldIndex),
    <<131,OldUniqEncoding/binary>>=term_to_binary(OldUniq),
    ModuleBytes=byte_size(ModuleEncoding),IndexBytes=byte_size(OldIndexEncoding),UniqBytes=byte_size(OldUniqEncoding),
    <<131,112,Size:32,Arity,Unique:16/binary,Index:32,Free:32,Rest/binary>>=term_to_binary(F),
    <<ModuleEncoding:ModuleBytes/binary,_:IndexBytes/binary,_:UniqBytes/binary,Tail/binary>>=Rest,
    <<131,ChangedIndex/binary>>=term_to_binary(255),<<131,ChangedUniq/binary>>=term_to_binary(1),
    ChangedSize=Size-IndexBytes-UniqBytes+byte_size(ChangedIndex)+byte_size(ChangedUniq),
    Wire = <<131,112,ChangedSize:32,Arity,Unique/binary,Index:32,Free:32,ModuleEncoding/binary,ChangedIndex/binary,ChangedUniq/binary,Tail/binary>>,
    G=binary_to_term(Wire),true=F=:=G,true=invoke(F,marker)=:=invoke(G,marker),
    true=erlang:fun_info(F,index)=:=erlang:fun_info(G,index),true=erlang:fun_info(F,uniq)=:=erlang:fun_info(G,uniq),true;
value(empty_env) -> F=fun named/1,{env,[]}=erlang:fun_info(F,env),{named,marker}=invoke(F,marker),true;
value(raise) -> F=fun() -> erlang:error(source_marker407) end,F();
value(badarity_zero) -> F=make(retained),F();
value(badarity_two) -> F=make(retained),F(one,two);
value(badfun) -> invoke(not_a_function,marker).
