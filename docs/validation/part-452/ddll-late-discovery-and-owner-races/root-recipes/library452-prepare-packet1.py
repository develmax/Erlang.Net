from pathlib import Path
import json
r=Path.cwd();old=r/'artifacts/library451-probe2';out=r/'artifacts/library452-probe1';assert not out.exists();out.mkdir()
for f in old.iterdir():
 if f.is_file():(out/f.name.replace('451','452')).write_bytes(f.read_bytes().replace(b'451',b'452'))
p=out/'run-root.py';s=p.read_text();s=s.replace("p.add_argument('--orders',default='incoming,outgoing');","p.add_argument('--orders',default='incoming,outgoing');p.add_argument('--modes',default='late_success,late_error,old_timer');")
s=s.replace("for depth,order in [(d,o) for d in map(int,a.depths.split(',')) for o in a.orders.split(',')]:","for depth,order,mode in [(d,o,m) for d in map(int,a.depths.split(',')) for o in a.orders.split(',') for m in a.modes.split(',')]:")
s=s.replace("group=out/('depth'+str(depth)+'-'+order)","group=out/('depth'+str(depth)+'-'+order+'-'+mode)")
s=s.replace("str(depth)+order+'@127.0.0.1'","str(depth)+order+mode+'@127.0.0.1'")
s=s.replace("'{127,0,0,1}','-pa'","'{127,0,0,1}','net_setuptime','5','-pa'")
s=s.replace("str(epmd_port),order]","str(epmd_port),order,mode]")
s=s.replace("'compare-depth'+str(depth)+'-'+order,","'compare-depth'+str(depth)+'-'+order+'-'+mode,")
s=s.replace("{'Depth':depth,'Order':order,","{'Depth':depth,'Order':order,'Mode':mode,")
s=s.replace("len(a.depths.split(','))*len(a.orders.split(','))","len(a.depths.split(','))*len(a.orders.split(','))*len(a.modes.split(','))")
s=s.replace("'Transport':'actual original pending collisions/alive status, runtime data queues and deliberate owned OS peer-tree termination/restart'","'Transport':'actual queued late discovery success/error and naturally fired old-checker timer across authentic owner replacement; actual runtime data queues'")
p.write_bytes(s.encode())
p=out/'DistributionTcp452.cs';s=p.read_text();needle=' internal TaskCompletionSource<bool> HelloReceived';assert needle in s;s=s.replace(needle,' internal TaskCompletionSource<bool> TcpConnected {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);\n'+needle);p.write_bytes(s.encode())
p=out/'DistributionTcp452.Initiator.cs';s=p.read_text().replace('bool expectSilentRefusal=false)','bool expectSilentRefusal=false,Task? helloGate=null)');needle='await client.ConnectAsync("127.0.0.1",port,deadline.Token);stream=client.GetStream();';assert needle in s;s=s.replace(needle,needle+'TcpConnected.TrySetResult(true);if(helloGate is not null)await helloGate.WaitAsync(deadline.Token);');p.write_bytes(s.encode())
p=out/'ddll452_epmd.erl';s=p.read_text();s=s.replace('receive {release452,Owner}->erl_epmd:address_please(Name,Host,Family)','receive {release452,Owner,Mode}->case Mode of late_success->erl_epmd:address_please(Name,Host,Family);late_error->{error,nxdomain} end');p.write_bytes(s.encode())
p=out/'ddll_tcp452.erl';s=p.read_text();needle='{trace_finish,[]} -> {ok,trace_finish()};';assert needle in s
s=s.replace(needle,needle+'''
                        {last_events,[]} -> {ok,get(last_events452)};
                        {wait_acceptor,[]} -> {ok,wait_acceptor452(5000)};
                        {kernel_hold,[]} -> {ok,kernel_hold452()};
                        {kernel_resume,[]} -> {ok,kernel_resume452()};
                        {wait_queued_accept,[Node]} -> {ok,wait_queue452(Node,accept,5000)};
                        {wait_queued_setup,[Node]} -> {ok,wait_queue452(Node,setup,5000)};
                        {release_checker,[Mode]} -> {ok,release_checker452(Mode)};
                        {wait_old_timer,[Node]} -> {ok,wait_old_timer452(Node,10000)};
                        {race_cleanup,[]} -> {ok,race_cleanup452()};''')
s=s.replace('    {Reason,Result,wait_up(Node,5000)}.','    put(last_checker452,CheckPid),{Reason,Result,wait_up(Node,5000)}.')
s=s.replace('Tracer=spawn(fun()->trace_loop452([]) end)','Tracer=spawn(fun()->trace_loop452([],[]) end)').replace('[send,{tracer,Tracer}]',"[send,'receive',{tracer,Tracer}]").replace('erlang:trace(K,false,[send])',"erlang:trace(K,false,[send,'receive'])")
s=s.replace('receive {traces452,Rows}->Rows','receive {traces452,Rows,Events}->put(last_events452,Events),Rows')
start=s.index('trace_loop452(Rows)->receive')
s=s[:start]+'''trace_loop452(Rows,Events)->receive
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
    case erase(previous_epmd452) of undefined->ok;{ok,Previous}->application:set_env(kernel,epmd_module,Previous) end,
    ok.
'''
p.write_bytes(s.encode())
p=out/'Program.cs';s=p.read_text();s=s.replace('int depth=int.Parse(args[3]),','string raceMode=args[10];\nint depth=int.Parse(args[3]),')
start=s.index(' var outgoing=new DistributionTcp452');end=s.index(' var checkFinished=',start)
s=s[:start]+''' var outgoing=new DistributionTcp452(runtime,Path.Combine(input.OutputDirectory,"simultaneous-outgoing"));string? originalStatus=null;
 var helloGate=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
 var connecting=outgoing.Connect(port,peerNode,managedNode,listener.Creation,statusObserved:status=>originalStatus=status,helloGate:helloGate.Task);
 await outgoing.TcpConnected.Task.WaitAsync(TimeSpan.FromSeconds(10));
 if(raceMode!="old_timer")
 {
  Save("original-real-acceptor-created",peer.Call("wait_acceptor"),Term.A("captured"),false);
  peer.Call("kernel_hold");helloGate.TrySetResult(true);
  Save("original-incoming-queued-first",peer.Call("wait_queued_accept",Term.A(managedNode)),Term.A("captured"),false);
  peer.Call("release_checker",Term.A(raceMode));
  var ordered=(TupleTerm)peer.Call("wait_queued_setup",Term.A(managedNode));
  DriverParentHarnessAccess423.Require(((Integer)ordered.Items[1]).Value<((Integer)ordered.Items[2]).Value);
  var late=(TupleTerm)ordered.Items[4];DriverParentHarnessAccess423.Require(late.Items[2].Equals(checking.Items[1]));
  var result=(TupleTerm)late.Items[4];DriverParentHarnessAccess423.Require(result.Items[0].Equals(Term.A(raceMode=="late_success"?"ok":"error")));
  Save("original-actual-mailbox-order",ordered,Term.A("captured"),false);peer.Call("kernel_resume");
 }
 else helloGate.TrySetResult(true);
 DriverParentHarnessAccess423.Require(await connecting&&originalStatus=="ok_simultaneous");link=outgoing;bool incomingExpected=false;
'''+s[end:]
needle=' Save("original-check-pending-traces",peer.Call("trace_finish"),Term.A("captured"),false);';assert needle in s;s=s.replace(needle,needle+'\n Save("original-discovery-receive-events",peer.Call("last_events"),Term.A("captured"),false);')
needle=' Save("original-controller-after-alive",peer.Call("connection_row",Term.A(managedNode)),Term.A("captured"),false);';assert needle in s
s=s.replace(needle,needle+'''
 if(raceMode=="old_timer")
 {
  peer.Call("trace_start");
  Term beforeData=await Managed("remote",Term.A(peerNode),Term.A("seen_node"),Term.List(Term.A(managedNode)));
  DriverParentHarnessAccess423.Require(beforeData.Equals(Term.A("true")));Save("actual-data-before-old-timer",beforeData,beforeData,false);
  Term timerBefore=peer.Call("raw_connection",Term.A(managedNode));var liveBefore=runtime.DistributionPending.ConnectedSnapshot(link.Handle);
  Save("original-row-before-natural-old-timer",timerBefore,Term.A("captured"),false);
  var timer=(TupleTerm)peer.Call("wait_old_timer",Term.A(managedNode));
  DriverParentHarnessAccess423.Require(((TupleTerm)timer.Items[0]).Items[2].Equals(checking.Items[1])&&timer.Items[2].Equals(Term.A("false"))&&timer.Items[3].Equals(Term.I(5000)));
  Save("original-natural-old-timer",timer,Term.A("captured"),false);
  Term timerAfter=peer.Call("raw_connection",Term.A(managedNode));Save("original-row-after-natural-old-timer",timerAfter,Term.A("captured"),false);
  DriverParentHarnessAccess423.Require(timerBefore.Equals(timerAfter)&&liveBefore.Equals(runtime.DistributionPending.ConnectedSnapshot(link.Handle)));
  Save("original-old-timer-send-traces",peer.Call("trace_finish"),Term.A("captured"),false);
  Save("original-natural-timer-receive-events",peer.Call("last_events"),Term.A("captured"),false);
  Term afterData=await Managed("remote",Term.A(peerNode),Term.A("seen_node"),Term.List(Term.A(managedNode)));
  DriverParentHarnessAccess423.Require(afterData.Equals(Term.A("true")));Save("actual-data-after-old-timer",afterData,afterData,false);
 }
 File.WriteAllText(Path.Combine(input.OutputDirectory,"late-discovery-owner-proof.json"),JsonSerializer.Serialize(new{Passed=true,Mode=raceMode,ActualSourceMessages=true,InjectedSetupMessage=false,InjectedTimeoutMessage=false,CurrentChannelWorks=true,NaturalTimerAfterActualHandoff=raceMode=="old_timer",QueuedLateResultAfterIncoming=raceMode!="old_timer"},json));
''')
s=s.replace(' try{oracle.Stop();}', ' try{if(!peerStopped)DriverParentHarnessAccess423.Require(peer.Call("race_cleanup").Equals(Term.A("ok")));}catch(Exception error){errors.Add("original race cleanup: "+error);}\n try{oracle.Stop();}')
s=s.replace('Depth=depth,Results=results','Depth=depth,Mode=raceMode,Results=results').replace('Original check_pending/already_pending with controlled discovery and actual connection handoff/restart','Original queued late discovery and actual old timer across owner replacement');p.write_bytes(s.encode())
(out/'reviewed-successor.json').write_text(json.dumps({'Previous':str(old),'Scope':'Actual queued late success/error after genuine incoming request; separate naturally fired five-second checker timer after genuine alive owner handoff. Explicit bounded original kernel suspension and actual mailbox fences, no forged setup/timeout messages. All existing DDLL/owner/reference/lifecycle evidence retained. Stack on verified Part451 without merging it.'},indent=2)+'\n')
print('Prepared452 immutable observer packet')
