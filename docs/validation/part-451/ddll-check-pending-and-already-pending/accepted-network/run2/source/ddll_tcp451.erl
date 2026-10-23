-module(ddll_tcp451).
-export([start/1]).
start([Ready]) ->
    {ok,L}=gen_tcp:listen(0,[binary,{packet,4},{active,false},{ip,{127,0,0,1}},{reuseaddr,false}]),
    {ok,{_,Port}}=inet:sockname(L),
    ok=file:write_file(Ready,integer_to_binary(Port),[exclusive]),
    {ok,S}=gen_tcp:accept(L,60000),ok=gen_tcp:close(L),
    Worker=beam_ddll_distribution451:new(),true=register(ddll451_worker,Worker),loop(S,Worker).
loop(S,Worker) ->
    case gen_tcp:recv(S,0,60000) of
        {ok,Bytes} ->
            case binary_to_term(Bytes) of
                stop -> M=monitor(process,Worker),Worker!stop,
                    receive {'DOWN',M,process,Worker,normal}->ok after 5000->error(worker_stop_timeout) end,
                    ok=gen_tcp:send(S,term_to_binary(stopped)),gen_tcp:close(S),halt();
                {Function,Arguments} ->
                    Result=case {Function,Arguments} of
                        {connect_peer,[Node]} -> {ok,net_kernel:connect_node(Node)};
                        {set_peer_cookie,[Node,Cookie]} -> {ok,erlang:set_cookie(Node,Cookie)};
                        {connection_row,[Node]} -> {ok,wait_up(Node,5000)};
                        {raw_connection,[Node]} -> {ok,ets:lookup(sys_dist,Node)};
                        {check_start,[Node]} -> {ok,check_start(Node)};
                        {check_finish,[Node]} -> {ok,check_finish(Node)};
                        {trace_start,[]} -> {ok,trace_start()};
                        {trace_finish,[]} -> {ok,trace_finish()};
                        _ -> beam_ddll_distribution451:request(Worker,Function,Arguments)
                    end,
                    ok=gen_tcp:send(S,term_to_binary(Result,[{minor_version,2}])),loop(S,Worker)
            end;
        {error,closed}->halt(2);
        Other->error({socket_failure,Other})
    end.

wait_up(Node,0)->error({connection_not_up,Node,ets:lookup(sys_dist,Node)});
wait_up(Node,N)->case ets:lookup(sys_dist,Node) of
 [Row] when element(4,Row)=:=up->Row;
 _->receive after 1->wait_up(Node,N-1) end end.

check_start(Node) ->
    Previous=application:get_env(kernel,epmd_module),put(previous_epmd451,Previous),
    {module,ddll451_epmd}=code:ensure_loaded(ddll451_epmd),
    persistent_term:put({ddll451_epmd,barrier},{Node,self()}),
    ok=application:set_env(kernel,epmd_module,ddll451_epmd),
    Parent=self(),Connector=spawn(fun()->Parent!{connect_result451,Node,net_kernel:connect_node(Node)} end),
    receive {select_entered451,CheckPid,Node}->
        Ref=monitor(process,CheckPid),put(check451,{CheckPid,Ref,Connector}),
        Row=wait_state451(Node,check_pending,5000),true=(element(5,Row)=:=CheckPid),
        {Row,CheckPid,Connector}
    after 5000->error(check_barrier_timeout) end.
check_finish(Node) ->
    {CheckPid,Ref,_}=erase(check451),
    Reason=receive {'DOWN',Ref,process,CheckPid,R}->R after 5000->error(checker_not_stopped) end,
    Result=receive {connect_result451,Node,V}->V after 5000->error(connect_waiter_not_replied) end,
    persistent_term:erase({ddll451_epmd,barrier}),
    case erase(previous_epmd451) of undefined->application:unset_env(kernel,epmd_module);{ok,V0}->application:set_env(kernel,epmd_module,V0) end,
    {Reason,Result,wait_up(Node,5000)}.
wait_state451(Node,State,0)->error({state_timeout,State,ets:lookup(sys_dist,Node)});
wait_state451(Node,State,N)->case ets:lookup(sys_dist,Node) of
 [Row] when element(4,Row)=:=State->Row;
 _->receive after 1->wait_state451(Node,State,N-1) end end.
trace_start()->
    Tracer=spawn(fun()->trace_loop451([]) end),put(tracer451,Tracer),
    1=erlang:trace(whereis(net_kernel),true,[send,{tracer,Tracer}]),ok.
trace_finish()->
    K=whereis(net_kernel),1=erlang:trace(K,false,[send]),Ref=erlang:trace_delivered(K),
    receive {trace_delivered,K,Ref}->ok after 5000->error(trace_delivery_timeout) end,
    Tracer=erase(tracer451),Tracer!{collect451,self()},
    receive {traces451,Rows}->Rows after 5000->error(trace_collect_timeout) end.
trace_loop451(Rows)->receive
    {trace,K,send,{K,{accept_pending,_}}=Message,To}->trace_loop451([{Message,To}|Rows]);
    {collect451,From}->From!{traces451,lists:reverse(Rows)};
    _->trace_loop451(Rows)
end.
