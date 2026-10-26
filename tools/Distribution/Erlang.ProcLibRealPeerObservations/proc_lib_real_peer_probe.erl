-module(proc_lib_real_peer_probe).
-export([run/2, identity/0]).

run(Case, Output) ->
    put(output, Output),
    OldTrap = process_flag(trap_exit, true),
    try
        PeerName = os:getenv("ERLANG_NET_PEER_NAME"),
        Erl = os:getenv("ERLANG_NET_EXACT_ERL"),
        Port = os:getenv("ERL_EPMD_PORT"),
        Beam = filename:dirname(code:which(?MODULE)),
        Extra = case Case of labels_hidden -> ["-hidden"]; _ -> [] end,
        Options = #{name => PeerName, host => "127.0.0.1", longnames => true,
                    connection => standard_io, exec => Erl, wait_boot => 10000,
                    shutdown => 5000,
                    env => [{"ERL_EPMD_PORT",Port}],
                    args => ["+S","2:2","+pc","latin1","-setcookie",atom_to_list(erlang:get_cookie()),
                             "-start_epmd","false","-kernel","inet_dist_use_interface","{127,0,0,1}",
                             "-pa",Beam] ++ Extra},
        save("peer-options.etf",Options),
        {ok,Peer,Remote} = peer:start_link(Options),
        PeerMonitor = monitor(process,Peer),
        save("peer-identity.etf",peer:call(Peer,?MODULE,identity,[],5000)),
        save("origin-identity.etf",identity()),
        pong = net_adm:ping(Remote),
        true = monitor_node(Remote,true),
        save("nodes-before.etf",#{visible=>nodes(),hidden=>nodes(hidden),connected=>nodes(connected)}),
        try
            scenario(Case,Peer,Remote),
            save("scenario.etf",{observed,ok})
        after
            Stop = peer:stop(Peer), save("peer-stop.etf",Stop),
            receive {'DOWN',PeerMonitor,process,Peer,PeerReason} ->
                save("peer-controller-cleanup.etf",{observed,PeerReason})
            after 7000 -> error(peer_controller_cleanup_timeout) end,
            receive {nodedown,Remote} -> save("peer-node-cleanup.etf",{observed,nodedown,Remote})
            after 7000 -> error(peer_node_cleanup_timeout) end
        end,
        save("stage.etf",{complete,Case}),save("outer.etf",{result,ok})
    catch Class:Reason:Stack ->
        save("outer.etf",{exception,Class,Reason,Stack}),save("stage.etf",{failed,Case})
    after process_flag(trap_exit,OldTrap) end.

scenario(Case,Peer,Remote) ->
    Marker = list_to_atom(os:getenv("ERLANG_NET_MAKER_MARKER")),
    Label = #{marker=>Marker,nested=>{<<0,255,128>>,[-123456789,{arbitrary,label}]}},
    Capture = #{parent=>self(),marker=>Marker,label=>Label,
                payload=>{<<7:3>>,#{values=>[1,2,3]},"captured"}},
    put('$ancestors',[original_ancestor]),
    IsFun = lists:member(Case,[fun_spawn,fun_spawn_link,fun_spawn_opt]),
    Fun = case IsFun of
        true -> F=proc_lib_remote_payload:maker(Capture),save("origin-fun.etf",erlang:fun_info(F)),F;
        false -> true=register(Marker,self()),undefined
    end,
    save("receiver-marker-before.etf",peer:call(Peer,erlang,whereis,[Marker],5000)),
    Result = case Case of
        fun_spawn -> proc_lib:spawn(Remote,Fun);
        fun_spawn_link -> proc_lib:spawn_link(Remote,Fun);
        fun_spawn_opt -> proc_lib:spawn_opt(Remote,Fun,[monitor]);
        mfa_spawn_link -> proc_lib:spawn_link(Remote,proc_lib_remote_payload,child,[Capture]);
        mfa_spawn_opt -> proc_lib:spawn_opt(Remote,proc_lib_remote_payload,child,[Capture],[monitor]);
        _ -> proc_lib:spawn(Remote,proc_lib_remote_payload,child,[Capture])
    end,
    {Child,Primary} = case Result of {P,R} when is_pid(P),is_reference(R) -> {P,R};P when is_pid(P) -> {P,none} end,
    save("spawn-result.etf",Result),
    true = node(Child)=:=Remote,
    Cleanup = monitor(process,Child),
    try
        Snapshot = receive {remote_ready,Child,State} -> State after 7000 -> error(child_ready_timeout) end,
        save("child-state.etf",Snapshot),
        Capture = maps:get(captured,Snapshot),
        undefined = maps:get(constructor_marker,Snapshot),
        Label = maps:get(label,Snapshot),
        save("receiver-marker-after.etf",peer:call(Peer,erlang,whereis,[Marker],5000)),
        save("remote-label-dictionary.etf",peer:call(Peer,erlang,process_info,[Child,{dictionary,'$process_label'}],5000)),
        AliveLabel=proc_lib:get_label(Child),save("get-label-alive.etf",AliveLabel),
        case Case of labels_hidden -> undefined=AliveLabel; _ -> Label=AliveLabel end,
        Child ! {finish,self()},
        receive {'DOWN',Cleanup,process,Child,Reason} -> save("child-cleanup.etf",{observed,Reason})
        after 7000 -> error(child_cleanup_timeout) end,
        case Primary of
            none -> ok;
            _ -> receive {'DOWN',Primary,process,Child,PrimaryReason}->save("primary-down.etf",{observed,PrimaryReason})
                 after 7000 -> error(primary_down_timeout) end
        end,
        case lists:member(Case,[mfa_spawn_link,fun_spawn_link]) of
            true -> receive {'EXIT',Child,ExitReason}->save("linked-exit.etf",{observed,ExitReason})
                    after 7000 -> error(linked_exit_timeout) end;
            false -> ok
        end,
        Dead=proc_lib:get_label(Child),undefined=Dead,save("get-label-dead.etf",Dead)
    after
        %% If a failed scenario left its child alive, retain that separate forced cleanup outcome.
        case peer:call(Peer,erlang,is_process_alive,[Child],5000) of
            true -> Child ! {finish,self()},
                    receive {'DOWN',Cleanup,process,Child,FinalReason}->save("failure-child-cleanup.etf",{observed,FinalReason})
                    after 7000 -> error(failure_child_cleanup_timeout) end;
            false -> ok
        end
    end.

identity() ->
    {module,proc_lib_remote_payload}=code:ensure_loaded(proc_lib_remote_payload),
    #{node=>node(),os_pid=>os:getpid(),otp=>erlang:system_info(otp_release),
      erts=>erlang:system_info(version),root=>code:root_dir(),printable=>io:printable_range(),
      modules=>[module_identity(M)||M<-[?MODULE,proc_lib_remote_payload,proc_lib,peer]]}.
module_identity(M) ->
    LoadedMd5=M:module_info(md5),
    {M,Image,ObjectPath}=code:get_object_code(M),
    {ok,{M,CodeMd5}}=beam_lib:md5(Image),
    {M,code:which(M),ObjectPath,LoadedMd5,CodeMd5,erlang:md5(Image)}.
save(Name,Term)->
    File=filename:join(get(output),Name),Temp=File++".tmp",
    ok=file:write_file(Temp,term_to_binary(Term)),ok=file:rename(Temp,File).
