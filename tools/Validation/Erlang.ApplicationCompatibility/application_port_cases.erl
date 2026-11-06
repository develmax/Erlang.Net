-module(application_port_cases).
-export([write/1, observe/2, scenario/1, cases/0]).

observe(F, Args) ->
    Parent=self(),
    {Pid,Ref}=spawn_monitor(fun() -> Parent!{self(),outcome(F,Args)} end),
    receive
        {Pid,Value} -> receive {'DOWN',Ref,process,Pid,normal} -> Value end;
        {'DOWN',Ref,process,Pid,Reason} -> exit(Reason)
    end.

outcome(F, Args) ->
    try apply(application, F, Args) of
        V -> {return,V}
    catch C:R:S -> {exception,C,R,S}
    end.

spec(Name, Apps, Mod) ->
    {application,Name,[{description,"application port"},{vsn,"1"},
        {modules,[application_port_callback]},{registered,[]},
        {applications,Apps},{env,[{marker,Name}]},{mod,Mod}]}.

cases() ->
    Base = [{behaviour_info,[callbacks]}, {behaviour_info,[optional_callbacks]},
        {behaviour_info,[bad]}, {get_application,[]}, {get_application,[absent_application_port_module]},
        {get_application,[42]}, {get_supervisor,[42]}, {get_supervisor,[port_absent]},
        {get_env,[marker]}, {get_env,[port_absent,marker]}, {get_env,[port_absent,marker,default]},
        {get_all_env,[]}, {get_all_env,[port_absent]}, {get_key,[vsn]},
        {get_key,[port_absent,vsn]}, {get_all_key,[]}, {get_all_key,[port_absent]}, {start_type,[]},
        {set_env,[[]]}, {set_env,[[],[]]}, {ensure_all_started,[[]]},
        {takeover,[port_absent,bad_restart]},
        {ensure_all_started,[[],temporary]}, {ensure_all_started,[[],temporary,serial]},
        {ensure_all_started,[[],temporary,concurrent]}, {ensure_all_started,[[],temporary,bad_mode]},
        {load,[spec(port_case,[],[])]}, {load,[spec(port_case,[],[]),[]]},
        {get_key,[port_case,vsn]}, {get_env,[port_case,marker]}, {get_env,[port_case,marker,default]},
        {get_all_env,[port_case]}, {get_all_key,[port_case]},
        {set_env,[port_case,marker,changed]}, {set_env,[port_case,marker,timed,infinity]},
        {set_env,[port_case,marker,timed2,5000]}, {set_env,[port_case,marker,persistent,[{persistent,true}]]},
        {unset_env,[port_case,marker]}, {unset_env,[port_case,marker,infinity]},
        {unset_env,[port_case,marker,5000]}, {unset_env,[port_case,marker,[{persistent,true}]]},
        {start,[port_case]}, {start,[port_case,temporary]},
        {ensure_started,[port_case]}, {ensure_started,[port_case,temporary]},
        {start_boot,[port_case]}, {start_boot,[port_case,temporary]},
        {stop,[port_case]}, {unload,[port_case]},
        {stop,[port_case]}, {unload,[port_case]},
        {load,[bad_spec]}, {load,[42]},
        {start,[spec(port_direct,[],[]),temporary]}, {stop,[port_direct]}, {unload,[port_direct]},
        {set_env,[[{port_bulk,[{a,1},{b,2}]}]]},
        {get_env,[port_bulk,a]}, {set_env,[[{port_bulk,[{a,3}]}],[{persistent,true}]]},
        {unset_env,[port_bulk,a,[{persistent,true}]]}, {unset_env,[port_bulk,b]}],
    Invalid = [{F,Args} || {F,Prefix} <- [{set_env,[port_case,k,v]},{unset_env,[port_case,k]}],
        V <- [-1,bad,1.0,{},<<>>,self()], Args <- [Prefix++[V]]],
    Guards = [{which_applications,[X]} || X <- [-1,bad,1.0,[],{},<<>>]] ++
        [{set_env,[X]} || X <- [42,bad,{},<<>>]] ++
        [{set_env,[X,Y]} || {X,Y} <- [{42,[]},{[],42},{bad,bad}]] ++
        [{ensure_all_started,[X,temporary,serial]} || X <- [42,{},<<>>]] ++
        [{ensure_all_started,[[42],temporary,serial]}] ++
        [{permit,[port_absent,X]} || X <- [true,false,bad,0,[],{}]] ++
        [{set_env,[[X],[]]} || X <- [bad,42,{}, {42,[]},{port_bad,bad},{port_bad,[bad]},
            {port_bad,[{42,1}]}]],
    %% Pids in arguments must be created on the executing runtime, not imported.
    [Row || {_,As}=Row <- Base++Invalid++Guards, not lists:any(fun is_pid/1,As)].

scenario({upstream,F}) ->
    [case application:load(S) of ok -> ok; {error,{already_loaded,_}} -> ok end
        || S<-persistent_term:get(application_port_prerequisites)],
    apply(application_SUITE,F,[[]]);
scenario(lifecycle) ->
    S = spec(port_life,[],{application_port_callback,hello}),
    ok=application:load(S),
    Before={application:get_key(port_life,vsn),application:get_all_env(port_life),
        application:get_all_key(port_life),application:get_supervisor(port_life)},
    ok=application:start(port_life),
    {ok,Root}=application:get_supervisor(port_life),
    Root!{self(),snapshot},
    Snapshot=receive {Root,A,E,Es,K,Ks,T} -> {A,E,Es,K,Ks,T} end,
    Active={application:get_application(Root),application:get_application(application_port_callback),
        lists:keymember(port_life,1,application:which_applications()),
        lists:keymember(port_life,1,application:which_applications(infinity)),
        lists:keymember(port_life,1,application:which_applications(5000)),
        lists:keymember(port_life,1,application:loaded_applications())},
    ok=application:stop(port_life),
    After={application:get_supervisor(port_life),application:get_application(Root)},
    ok=application:unload(port_life),
    {Before,Snapshot,Active,After};
scenario(persistence) ->
    App=port_persist,
    ok=application:set_env(App,marker,old),
    ok=application:set_env(App,keep,persist,[{persistent,true}]),
    ok=application:load(spec(App,[],[])),
    First={application:get_env(App,marker),application:get_env(App,keep)},
    ok=application:unload(App),
    ok=application:load(spec(App,[],[])),
    Second={application:get_env(App,marker),application:get_env(App,keep)},
    ok=application:unset_env(App,keep,[{persistent,true}]),
    ok=application:unload(App), {First,Second};
scenario({graph,Mode,Fail}) ->
    Names=[port_leaf,port_left,port_right,port_top],
    Mod=fun(port_top) when Fail -> {application_port_callback,fail};
           (_) -> {application_port_callback,graph} end,
    [ok=application:load(spec(N,Deps,Mod(N))) || {N,Deps} <-
        [{port_leaf,[]},{port_left,[port_leaf]},{port_right,[port_leaf]},
         {port_top,[port_left,port_right]}]],
    R=application:ensure_all_started([port_top],temporary,Mode),
    Running=[N || {N,_,_} <- application:which_applications(),lists:member(N,Names)],
    [application:stop(N) || N<-lists:reverse(Names)],
    [ok=application:unload(N) || N<-lists:reverse(Names)],
    %% Concurrent success order is intentionally unspecified; verify membership.
    Result=case R of {ok,Started} when Mode=:=concurrent -> {ok,lists:sort(Started)}; _ -> R end,
    {Result,lists:sort(Running)};
scenario({many,Mode}) ->
    Names=[list_to_atom("port_many_"++integer_to_list(N)) || N<-lists:seq(1,40)],
    [ok=application:load(spec(N,[],{application_port_callback,many})) || N<-Names],
    {ok,Started}=application:ensure_all_started(Names++Names,temporary,Mode),
    Again=application:ensure_all_started(Names,temporary,Mode),
    [ok=application:stop(N) || N<-lists:reverse(Started)],
    [ok=application:unload(N) || N<-Names],
    {case Mode of concurrent -> lists:sort(Started); serial -> Started end,Again};
scenario({optional,Mode}) ->
    {application,port_optional,Props}=spec(port_optional,[port_missing_optional],[]),
    ok=application:load({application,port_optional,[{optional_applications,[port_missing_optional]}|Props]}),
    R=application:ensure_all_started(port_optional,temporary,Mode),
    application:stop(port_optional),ok=application:unload(port_optional),R;
scenario({missing,Mode}) ->
    ok=application:load(spec(port_missing_parent,[port_missing_required],[])),
    R=application:ensure_all_started(port_missing_parent,temporary,Mode),
    ok=application:unload(port_missing_parent),R;
scenario(permission) ->
    ok=application:load(spec(port_permission,[],[])),
    ok=application:permit(port_permission,false),
    ok=application:start(port_permission),
    R1=lists:keymember(port_permission,1,application:which_applications()),
    ok=application:permit(port_permission,true),
    R2=lists:keymember(port_permission,1,application:which_applications()),
    ok=application:stop(port_permission),ok=application:unload(port_permission),{R1,R2};
scenario(inventory) ->
    {lists:sort([K || {K,_} <- application:info()]),is_list(application:loaded_applications()),
     application:get_application(self())}.

write(Dir) ->
    logger:set_primary_config(level,none),
    true=code:add_patha("tools/Validation/Erlang.ApplicationCompatibility/Upstream"),
    Prerequisites=[begin {ok,[Spec]}=file:consult("tools/Validation/Erlang.ApplicationCompatibility/Upstream/"++N++".app"),Spec end
        || N<-["appinc1","appinc2"]],
    persistent_term:put(application_port_prerequisites,Prerequisites),
    ok=file:write_file(filename:join(Dir,"prerequisites.etf"),term_to_binary(Prerequisites)),
    Rows=[{F,A,observe(F,A)} || {F,A} <- cases()],
    Scenarios=[lifecycle,persistence,permission,inventory] ++
        [{graph,M,F} || M<-[serial,concurrent],F<-[false,true]] ++
        [{optional,M} || M<-[serial,concurrent]] ++ [{missing,M} || M<-[serial,concurrent]] ++
        [{many,M} || M<-[serial,concurrent]] ++
        [{upstream,F} || F<-[get_env,set_env,set_env_persistent,persistent_env,set_env_errors]],
    Effects=[{S,scenario(S)} || S<-Scenarios],
    ok=file:write_file(filename:join(Dir,"original.etf"),term_to_binary(Rows)),
    ok=file:write_file(filename:join(Dir,"effects.etf"),term_to_binary(Effects)),
    ok=file:write_file(filename:join(Dir,"exports.etf"),term_to_binary(application:module_info(exports))),
    ok=file:write_file(filename:join(Dir,"feature-bootstrap.etf"),term_to_binary(
        [E || {K,_}=E<-persistent_term:get(),K=:=erl_features orelse K=:={erl_features,keywords}])),ok.
