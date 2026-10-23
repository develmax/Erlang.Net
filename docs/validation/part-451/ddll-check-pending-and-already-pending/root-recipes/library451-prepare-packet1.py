from pathlib import Path
import json
r=Path.cwd();old=r/'artifacts/library450-probe1';out=r/'artifacts/library451-probe1';assert not out.exists();out.mkdir()
for f in old.iterdir():
 if f.is_file():(out/f.name.replace('450','451')).write_text(f.read_text().replace('450','451'))
p=out/'DistributionTcp451.Initiator.cs';s=p.read_text();s=s.replace('Action<string>? statusObserved=null)','Action<string>? statusObserved=null,Task? statusGate=null,bool expectSilentRefusal=false)')
needle='byte[] status=await Read(2,deadline.Token);File.WriteAllBytes(Path.Combine(directory,"status.bin"),status);'
assert needle in s
s=s.replace(needle,'''byte[] status;
  try{status=await Read(2,deadline.Token);}
  catch(EndOfStreamException) when(expectSilentRefusal)
  {
   client.Dispose();File.WriteAllText(Path.Combine(directory,"candidate-refusal.json"),JsonSerializer.Serialize(new{OriginalClosedWithoutStatus=true,Adopted=false,ChallengeReceived=false}));return false;
  }
  DriverParentHarnessAccess423.Require(!expectSilentRefusal);File.WriteAllBytes(Path.Combine(directory,"status.bin"),status);''')
s=s.replace('statusObserved?.Invoke(code);','statusObserved?.Invoke(code);if(statusGate is not null)await statusGate.WaitAsync(deadline.Token);');p.write_text(s)
p=out/'ddll_tcp451.erl';s=p.read_text();needle='{connection_row,[Node]} -> {ok,wait_up(Node,5000)};';assert needle in s
s=s.replace(needle,needle+'''
                        {raw_connection,[Node]} -> {ok,ets:lookup(sys_dist,Node)};
                        {check_start,[Node]} -> {ok,check_start(Node)};
                        {check_finish,[Node]} -> {ok,check_finish(Node)};
                        {trace_start,[]} -> {ok,trace_start()};
                        {trace_finish,[]} -> {ok,trace_finish()};''')
s+='''
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
''';p.write_text(s)
(out/'ddll451_epmd.erl').write_text('''-module(ddll451_epmd).
-export([address_please/3,port_please/2]).
address_please(Name,Host,Family)->
    Node=list_to_atom(Name++"@"++Host),
    case persistent_term:get({?MODULE,barrier},undefined) of
        {Node,Owner}->Owner!{select_entered451,self(),Node},
            receive {release451,Owner}->erl_epmd:address_please(Name,Host,Family)
            after 10000->error(discovery_barrier_timeout) end;
        _->erl_epmd:address_please(Name,Host,Family)
    end.
port_please(Name,Host)->erl_epmd:port_please(Name,Host).
''')
p=out/'Program.cs';s=p.read_text();start=s.index(' var simultaneousHandle=');end=s.index(' Term peerIdentity=',start)
s=s[:start]+''' var simultaneousHandle=runtime.DistributionPending.BeginPending(Term.A(peerNode));
 using var pendingLease=runtime.DistributionPending.Acquire(simultaneousHandle);
 DriverParentHarnessAccess423.Require(pendingLease.Snapshot().State==DistributionPendingState.Pending);
 peer.Call("trace_start");
 var checking=(TupleTerm)peer.Call("check_start",Term.A(managedNode));
 var checkRow=(TupleTerm)checking.Items[0];
 DriverParentHarnessAccess423.Require(checkRow.Items[3].Equals(Term.A("check_pending"))&&checkRow.Items[4].Equals(checking.Items[1]));
 Save("original-check-pending-row",checking,Term.A("captured"),false);
 var outgoing=new DistributionTcp451(runtime,Path.Combine(input.OutputDirectory,"simultaneous-outgoing"));string? originalStatus=null;
 DriverParentHarnessAccess423.Require(await outgoing.Connect(port,peerNode,managedNode,listener.Creation,statusObserved:status=>originalStatus=status));
 DriverParentHarnessAccess423.Require(originalStatus=="ok_simultaneous");link=outgoing;bool incomingExpected=false;
 var checkFinished=(TupleTerm)peer.Call("check_finish",Term.A(managedNode));
 DriverParentHarnessAccess423.Require(checkFinished.Items[0].Equals(Term.A("remarked"))&&checkFinished.Items[1].Equals(Term.A("true")));
 Save("original-check-pending-finish",checkFinished,Term.A("captured"),false);
 Save("original-check-pending-traces",peer.Call("trace_finish"),Term.A("captured"),false);
 DriverParentHarnessAccess423.Require(link.Handle.Equals(simultaneousHandle)&&pendingLease.Snapshot().State==DistributionPendingState.Connected);
 File.WriteAllText(Path.Combine(input.OutputDirectory,"check-pending-proof.json"),JsonSerializer.Serialize(new{Passed=true,LexicalOrder=args[9],OriginalStatus=originalStatus,ActualDiscoveryBarrier=true,ActualCheckPendingRow=true,ActualCheckerExit="remarked",OriginalConnectWaiter=true,SameRuntimePendingHandle=true,ManagedCheckPendingStateClaim=false},json));
'''+s[end:]
needle=' var priorLink=link;priorLink.ExpectRemoteExit=true;';assert needle in s
held='''
 async Task<(DistributionTcp451 Candidate,Task<bool> Task,TaskCompletionSource<bool> Release)> HoldCandidate(string name,string reply,Func<Task>? retire=null)
 {
  var candidate=new DistributionTcp451(runtime,Path.Combine(input.OutputDirectory,name));
  var release=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
  var status=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
  var task=candidate.Connect(port,peerNode,managedNode,listener.Creation,aliveReply:reply,retireOld:retire,statusObserved:value=>status.TrySetResult(value),statusGate:release.Task);
  DriverParentHarnessAccess423.Require(await status.Task.WaitAsync(TimeSpan.FromSeconds(10))=="alive");
  return(candidate,task,release);
 }
 async Task RejectExtra(string phase)
 {
  Term before=peer.Call("raw_connection",Term.A(managedNode));
  DriverParentHarnessAccess423.Require(Cons.Items(before).Single() is TupleTerm row&&row.Items[3].Equals(Term.A("up_pending")));
  Save("original-"+phase+"-before",before,Term.A("captured"),false);
  var old=runtime.DistributionPending.ConnectedSnapshot(link!.Handle);
  await using(var extra=new DistributionTcp451(runtime,Path.Combine(input.OutputDirectory,phase+"-extra")))
   DriverParentHarnessAccess423.Require(!await extra.Connect(port,peerNode,managedNode,listener.Creation,expectSilentRefusal:true));
  Term after=peer.Call("raw_connection",Term.A(managedNode));Save("original-"+phase+"-after",after,Term.A("captured"),false);
  DriverParentHarnessAccess423.Require(before.Equals(after)&&old.Equals(runtime.DistributionPending.ConnectedSnapshot(link.Handle)));
  File.WriteAllText(Path.Combine(input.OutputDirectory,phase+"-proof.json"),JsonSerializer.Serialize(new{Passed=true,OriginalRowUnchanged=true,ManagedSnapshotUnchanged=true,NoStatus=true,NoChallenge=true,NoAdoption=true,FirstCandidateStillPending=true},json));
 }
 peer.Call("trace_start");
 var heldFalse=await HoldCandidate("already-pending-first-false","false");
 await RejectExtra("already-pending-false");
 await RemotePair("current-data-while-false-candidate-held","seen_node",[Term.A(oracleNode)],[Term.A(managedNode)]);
 heldFalse.Release.TrySetResult(true);DriverParentHarnessAccess423.Require(!await heldFalse.Task);await heldFalse.Candidate.DisposeAsync();
 peer.Call("connection_row",Term.A(managedNode));
 Save("original-already-pending-false-traces",peer.Call("trace_finish"),Term.A("captured"),false);
 await RemotePair("current-data-after-extra-refused-first-false","seen_node",[Term.A(oracleNode)],[Term.A(managedNode)]);
'''
s=s.replace(needle,held+needle)
start=s.index(' var replacement=new DistributionTcp451');end=s.index(' link=replacement;',start)
s=s[:start]+''' peer.Call("trace_start");
 var heldTrue=await HoldCandidate("alive-handoff","true",async()=>
 {
  await priorLink.RemoteClosed.WaitAsync(TimeSpan.FromSeconds(10));await priorLink.DisposeAsync();
 });
 await RejectExtra("already-pending-true");
 await RemotePair("current-data-while-true-candidate-held","seen_node",[Term.A(oracleNode)],[Term.A(managedNode)]);
 heldTrue.Release.TrySetResult(true);DriverParentHarnessAccess423.Require(await heldTrue.Task);var replacement=heldTrue.Candidate;
 Save("original-already-pending-true-traces",peer.Call("trace_finish"),Term.A("captured"),false);
'''+s[end:]
s=s.replace('Original simultaneous/alive handoff and deliberate original OS peer-tree termination/restart','Original check_pending/already_pending with controlled discovery and actual connection handoff/restart');p.write_text(s)
(out/'reviewed-successor.json').write_text(json.dumps({'Previous':str(old),'Scope':'Actual check_pending via documented temporary discovery callback barrier; already_pending silent extra-candidate close while first alive candidate is held, false and true completions; actual existing data/DDLL/restart retained. Host pending primitives are not a full managed net_kernel claim.'},indent=2)+'\n')
print('Prepared immutable451 observer packet')
