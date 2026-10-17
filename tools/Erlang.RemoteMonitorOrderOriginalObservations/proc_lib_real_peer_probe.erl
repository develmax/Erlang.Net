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
    true = Remote =/= node(),
    CustomLast = lists:member(Case,[spawn_custom_last,request_custom_last]),
    Request = lists:member(Case,[request_plain_last,request_custom_last]),
    Custom = {monitor,[{tag,custom_monitor_order}]},
    Options = case CustomLast of true -> [monitor,Custom]; false -> [Custom,monitor] end,
    {Child,Primary,Returned,Reply} = case Request of
        false ->
            Started = erlang:spawn_opt(Remote,proc_lib_remote_payload,child,[self()],Options),
            {P,R} = Started,
            {P,R,Started,none};
        true ->
            Requested = erlang:spawn_request(Remote,proc_lib_remote_payload,child,[self()],Options),
            Received = receive
                {spawn_reply,Requested,_,_}=Message -> Message
            after 7000 -> error(spawn_reply_timeout) end,
            save("request-reply.etf",Received),
            {spawn_reply,Requested,ok,P} = Received,
            {P,Requested,Requested,Received}
    end,
    save("spawn-result.etf",Returned),
    true = is_pid(Child),
    true = is_reference(Primary),
    true = node(Child) =:= Remote,
    true = node(Child) =/= node(),
    Cleanup = monitor(process,Child),
    try
        Ready = receive
            {remote_monitor_ready,Child,ChildNode} -> {Child,ChildNode}
        after 7000 -> error(remote_child_ready_timeout) end,
        {Child,Remote} = Ready,
        save("child-ready.etf",Ready),
        Child ! release_monitor_child,
        Down = receive
            {_,Primary,process,Child,_}=Observed -> Observed
        after 7000 -> error(primary_monitor_timeout) end,
        save("primary-raw-down.etf",Down),
        receive {'DOWN',Cleanup,process,Child,Reason} ->
            save("child-cleanup.etf",{observed,Reason})
        after 7000 -> error(child_cleanup_timeout) end,
        save("monitor-order.etf",#{api=>case Request of true->spawn_request;false->spawn_opt end,
            custom_last=>CustomLast,observer_node=>node(),remote_node=>Remote,
            child=>Child,primary_ref=>Primary,spawn_return=>Returned,
            request_reply=>Reply,down=>Down,options=>Options,
            connected_nodes=>nodes(connected)})
    after
        case peer:call(Peer,erlang,is_process_alive,[Child],5000) of
            true -> Child ! release_monitor_child,
                    receive {'DOWN',Cleanup,process,Child,FinalReason} ->
                        save("failure-child-cleanup.etf",{observed,FinalReason})
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
