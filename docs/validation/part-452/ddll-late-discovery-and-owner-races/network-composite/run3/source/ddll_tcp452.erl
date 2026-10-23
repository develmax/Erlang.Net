-module(ddll_tcp452).
-export([start/1]).
start([Ready]) ->
    {ok,L}=gen_tcp:listen(0,[binary,{packet,4},{active,false},{ip,{127,0,0,1}},{reuseaddr,false}]),
    {ok,{_,Port}}=inet:sockname(L),
    ok=file:write_file(Ready,integer_to_binary(Port),[exclusive]),
    {ok,S}=gen_tcp:accept(L,60000),ok=gen_tcp:close(L),
    Worker=beam_ddll_distribution452:new(),true=register(ddll452_worker,Worker),loop(S,Worker).
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
                        {last_events,[]} -> {ok,get(last_events452)};
                        {wait_acceptor,[]} -> {ok,wait_acceptor452(5000)};
                        {kernel_hold,[]} -> {ok,kernel_hold452()};
                        {kernel_resume,[]} -> {ok,kernel_resume452()};
                        {wait_queued_accept,[Node]} -> {ok,wait_queue452(Node,accept,5000)};
                        {wait_queued_setup,[Node]} -> {ok,wait_queue452(Node,setup,5000)};
                        {release_checker,[Mode]} -> {ok,release_checker452(Mode)};
                        {wait_old_timer,[Node]} -> {ok,wait_old_timer452(Node,10000)};
                        {race_cleanup,[]} -> {ok,race_cleanup452()};
                        _ -> beam_ddll_distribution452:request(Worker,Function,Arguments)
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
    Previous=application:get_env(kernel,epmd_module),put(previous_epmd452,Previous),put(race_env_saved452,true),
    {module,ddll452_epmd}=code:ensure_loaded(ddll452_epmd),
    persistent_term:put({ddll452_epmd,barrier},{Node,self()}),
    ok=application:set_env(kernel,epmd_module,ddll452_epmd),
    Parent=self(),Connector=spawn(fun()->Parent!{connect_result452,Node,net_kernel:connect_node(Node)} end),
    receive {select_entered452,CheckPid,Node}->
        Ref=monitor(process,CheckPid),put(check452,{CheckPid,Ref,Connector}),
        Row=wait_state452(Node,check_pending,5000),true=(element(5,Row)=:=CheckPid),
        {Row,CheckPid,Connector}
    after 5000->error(check_barrier_timeout) end.
check_finish(Node) ->
    {CheckPid,Ref,_}=erase(check452),
    Reason=receive {'DOWN',Ref,process,CheckPid,R}->R after 5000->error(checker_not_stopped) end,
    Result=receive {connect_result452,Node,V}->V after 5000->error(connect_waiter_not_replied) end,
    persistent_term:erase({ddll452_epmd,barrier}),
    case erase(previous_epmd452) of undefined->application:unset_env(kernel,epmd_module);{ok,V0}->application:set_env(kernel,epmd_module,V0) end,
    erase(race_env_saved452),put(last_checker452,CheckPid),{Reason,Result,wait_up(Node,5000)}.
wait_state452(Node,State,0)->error({state_timeout,State,ets:lookup(sys_dist,Node)});
wait_state452(Node,State,N)->case ets:lookup(sys_dist,Node) of
 [Row] when element(4,Row)=:=State->Row;
 _->receive after 1->wait_state452(Node,State,N-1) end end.
trace_start()->
    Tracer=spawn(fun()->trace_loop452([],[]) end),put(tracer452,Tracer),
    1=erlang:trace(whereis(net_kernel),true,[send,'receive',{tracer,Tracer}]),ok.
trace_finish()->
    K=whereis(net_kernel),1=erlang:trace(K,false,[send,'receive']),Ref=erlang:trace_delivered(K),
    receive {trace_delivered,K,Ref}->ok after 5000->error(trace_delivery_timeout) end,
    Tracer=erase(tracer452),Tracer!{collect452,self()},
    receive {traces452,Rows,Events}->put(last_events452,Events),Rows after 5000->error(trace_collect_timeout) end.
trace_loop452(Rows,Events)->receive
    {trace,K,send,{K,{accept_pending,_}}=Message,To}->trace_loop452([{Message,To}|Rows],Events);
    {trace,K,send,{K,controller,_}=Message,To}->trace_loop452(Rows,[{controller_sent,Message,To}|Events]);
    {trace,_K,'receive',{setup_check,_,_,_,_}=Message}->trace_loop452(Rows,[{received,Message}|Events]);
    {trace,_K,'receive',{setup_check_timeout,_,_}=Message}->trace_loop452(Rows,[{received,Message}|Events]);
    {peek452,From}->From!{events452,lists:reverse(Events)},trace_loop452(Rows,Events);
    {collect452,From}->From!{traces452,lists:reverse(Rows),lists:reverse(Events)};
    _->trace_loop452(Rows,Events)
end.
events452()->get(tracer452)!{peek452,self()},receive {events452,E}->E after 5000->error(trace_peek_timeout) end.
wait_acceptor452(0)->error(acceptor_not_created);
wait_acceptor452(N)->case [E||{controller_sent,_,_}=E<-events452()] of
 [E|_]->E;
 []->receive after 1->wait_acceptor452(N-1) end
end.
kernel_hold452()->K=whereis(net_kernel),true=erlang:suspend_process(K),put(kernel_held452,K),K.
kernel_resume452()->K=erase(kernel_held452),true=erlang:resume_process(K),true.
wait_queue452(Node,Kind,0)->error({kernel_queue_timeout,Node,Kind,process_info(whereis(net_kernel),messages)});
wait_queue452(Node,Kind,N)->
    {messages,Messages}=process_info(whereis(net_kernel),messages),
    Accept=[{I,M}||{I,{_Pid,{accept_pending,_Mine,Other,_Type}}=M}<-lists:enumerate(Messages),Other=:=Node],
    Setup=[{I,M}||{I,{setup_check,Other,_Pid,_Timer,_Res}=M}<-lists:enumerate(Messages),Other=:=Node],
    case {Kind,Accept,Setup} of
        {accept,[{I,M}|_],_}->{Messages,I,M};
        {setup,[{AI,AM}|_],[{SI,SM}|_]} when AI<SI->{Messages,AI,SI,AM,SM};
        _->receive after 1->wait_queue452(Node,Kind,N-1) end
    end.
release_checker452(Mode)->{CheckPid,_,_}=get(check452),CheckPid!{release452,self(),Mode},true.
wait_old_timer452(Node,0)->error({actual_old_timer_not_seen,Node});
wait_old_timer452(Node,N)->
    CheckPid=get(last_checker452),Events=events452(),
    case [M||{received,{setup_check_timeout,Other,Pid}=M}<-Events,Other=:=Node,Pid=:=CheckPid] of
        [Message|_]->_=sys:get_state(net_kernel),{Message,wait_up(Node,5000),is_process_alive(CheckPid),net_kernel:connecttime()};
        []->receive after 1->wait_old_timer452(Node,N-1) end
    end.
race_cleanup452()->
    case erase(kernel_held452) of undefined->ok;K->true=erlang:resume_process(K) end,
    persistent_term:erase({ddll452_epmd,barrier}),
    case erase(race_env_saved452) of
        true->case erase(previous_epmd452) of undefined->ok=application:unset_env(kernel,epmd_module);{ok,Previous}->ok=application:set_env(kernel,epmd_module,Previous) end;
        _->ok
    end,
    ok.
