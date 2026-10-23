using System.Text.Json;
using System.Diagnostics;
using Erlang;

var json=new JsonSerializerOptions{WriteIndented=true,PropertyNameCaseInsensitive=true};
var input=JsonSerializer.Deserialize<DriverParentHarnessInput423>(File.ReadAllText(args[0]),json)!;
DriverParentHarnessAccess423.VerifyInputs(input);Directory.CreateDirectory(input.OutputDirectory);
using var oracle=new Peer452(int.Parse(args[1]));var peer=new Peer452(int.Parse(args[2]));
RestartPeer452? restarted=null;DistributionListener452? listener=null;bool peerStopped=false;
string raceMode=args[10];
int depth=int.Parse(args[3]),port=int.Parse(args[4]);string peerNode=args[5],oracleNode=args[6],managedNode=args[7];
var results=new List<object>();var errors=new List<string>();var bindings=new List<(ReferenceTerm Expected,ReferenceTerm Actual)>();
DriverParentHarnessFixture423? fixture=null;ErlDriverHostChild420? child=null;DistributionTcp452? link=null;int sequence=0;
try
{
 fixture=await DriverParentHarnessFixture423.Create(input,"distribution452",false);var runtime=fixture.Runtime;
 child=(ErlDriverHostChild420)DriverParentHarnessAccess423.Field(DriverParentHarnessAccess423.Field(runtime,"driverFrontend422"),"child");
 await runtime.InitializeModulesAsync(registry=>
 {
  var image=registry.PrepareBeam(File.ReadAllBytes(Path.Combine(input.OriginalDirectory,"observer-ebin/beam_ddll_distribution452.beam")),new BeamDependencySelection(input.DeclaredModules.Concat(new[]{"beam_ddll_distribution452"})));
  DriverParentHarnessAccess423.Require(registry.PublishPreparedBeam([image]).Status==BeamPublicationStatus.Published);
  foreach(var e in image.Code.Exports)DriverParentHarnessAccess423.Require(ReferenceEquals(registry.SourceOwner(image.Name,e.Name,e.Arity),image));
 });
 Pid worker=(Pid)await fixture.Actor!.Invoke(async c=>await runtime.Modules.Call(c,"beam_ddll_distribution452","new"));
 async Task<Term> Managed(string f,params Term[] a)=>Unwrap(await fixture.Actor.Invoke(async c=>await runtime.Modules.Call(c,"beam_ddll_distribution452","request",worker,Term.A(f),Term.List(a))));
 Term Original(string f,params Term[] a)=>oracle.Call(f,a);
 void Save(string name,Term expected,Term actual,bool compare=true)
 {
  string stem=Path.Combine(input.OutputDirectory,$"{++sequence:D3}-{name}");
  File.WriteAllBytes(stem+"-original.etf",ExternalTermFormat.Encode(expected));File.WriteAllBytes(stem+"-managed.etf",ExternalTermFormat.Encode(actual));
  File.WriteAllText(stem+"-original.json",JsonSerializer.Serialize(DriverParentHarnessTerms423.Typed(expected),json));File.WriteAllText(stem+"-managed.json",JsonSerializer.Serialize(DriverParentHarnessTerms423.Typed(actual),json));
  if(!compare)return;
  bool exact=expected.Equals(actual),raw=File.ReadAllBytes(stem+"-original.etf").AsSpan().SequenceEqual(File.ReadAllBytes(stem+"-managed.etf")),protocol=Equal(expected,actual,bindings);
  results.Add(new{Name=name,Passed=protocol,WholeTypedEqual=exact,WholeRawEqual=raw,OwnReferenceBindings=bindings.Count});
  if(!protocol)errors.Add(name);
  if(name.StartsWith("queue-one"))DriverParentHarnessAccess423.Require(expected.Equals(Term.I(1))&&actual.Equals(Term.I(1)));
  if(name.StartsWith("queue-zero"))DriverParentHarnessAccess423.Require(expected.Equals(Term.I(0))&&actual.Equals(Term.I(0)));
  if(name.Contains("cancels-owner")||name.StartsWith("repeat-completed"))DriverParentHarnessAccess423.Require(expected.Equals(Term.Tuple(Term.A("value"),Term.A("true")))&&actual.Equals(expected));
  if(name.Contains("foreign-reject"))foreach(Term value in new[]{expected,actual})
  {
   DriverParentHarnessAccess423.Require(value is TupleTerm{Items.Count:4});var t=(TupleTerm)value;
   DriverParentHarnessAccess423.Require(t.Items[0].Equals(Term.A("exception"))&&t.Items[1].Equals(Term.A("error"))&&t.Items[2].Equals(Term.A("badarg"))&&Cons.Items(t.Items[3]).Count()==Math.Clamp(depth,1,3));
  }
 }
 async Task Step(string n,string f,params Term[] a)=>Save(n,Original(f,a),await Managed(f,a));
 async Task<(Term,Term)> RemotePair(string n,string f,Term[] originalArgs,Term[] managedArgs,bool compare=true)
 {
  Term x=Original("remote",Term.A(peerNode),Term.A(f),Term.List(originalArgs));Term y=await Managed("remote",Term.A(peerNode),Term.A(f),Term.List(managedArgs));Save(n,x,y,compare);return(x,y);
 }
 async Task<(BitString,BitString)> Wires(string key)
 {
  var x=(BitString)Original("wire",Term.A(key));var y=(BitString)await Managed("wire",Term.A(key));Save("wire-"+key,x,y,false);
  bindings.Add(((ReferenceTerm)ExternalTermFormat.DecodeOne(x.ToArray(),new()).Term,(ReferenceTerm)ExternalTermFormat.DecodeOne(y.ToArray(),new()).Term));return(x,y);
 }
 Save("peer-setup",peer.Call("setup",Term.String(input.DriverDirectory)),Term.A("ok"));peer.Call("depth",Term.I(depth));
 await Step("setup","setup",Term.String(input.DriverDirectory));await Step("depth","depth",Term.I(depth));
 listener=new(int.Parse(args[8]),managedNode,Path.Combine(input.OutputDirectory,"listener"));
 Term managedIdentity=await Managed("start_node_creation",Term.A(managedNode),Term.I(listener.Creation));Save("own-identities",Original("identity"),managedIdentity,false);
 DriverParentHarnessAccess423.Require(managedIdentity.Equals(Term.Tuple(Term.A(managedNode),Term.I(listener.Creation),Term.A("true"))));
 var simultaneousHandle=runtime.DistributionPending.BeginPending(Term.A(peerNode));
 using var pendingLease=runtime.DistributionPending.Acquire(simultaneousHandle);
 DriverParentHarnessAccess423.Require(pendingLease.Snapshot().State==DistributionPendingState.Pending);
 peer.Call("trace_start");
 var checking=(TupleTerm)peer.Call("check_start",Term.A(managedNode));
 var checkRow=(TupleTerm)checking.Items[0];
 DriverParentHarnessAccess423.Require(checkRow.Items[3].Equals(Term.A("check_pending"))&&checkRow.Items[4].Equals(checking.Items[1]));
 Save("original-check-pending-row",checking,Term.A("captured"),false);
 var outgoing=new DistributionTcp452(runtime,Path.Combine(input.OutputDirectory,"simultaneous-outgoing"));string? originalStatus=null;
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
 var checkFinished=(TupleTerm)peer.Call("check_finish",Term.A(managedNode));
 DriverParentHarnessAccess423.Require(checkFinished.Items[0].Equals(Term.A("remarked"))&&checkFinished.Items[1].Equals(Term.A("true")));
 Save("original-check-pending-finish",checkFinished,Term.A("captured"),false);
 Save("original-check-pending-traces",peer.Call("trace_finish"),Term.A("captured"),false);
 Save("original-discovery-receive-events",peer.Call("last_events"),Term.A("captured"),false);
 DriverParentHarnessAccess423.Require(link.Handle.Equals(simultaneousHandle)&&pendingLease.Snapshot().State==DistributionPendingState.Connected);
 File.WriteAllText(Path.Combine(input.OutputDirectory,"check-pending-proof.json"),JsonSerializer.Serialize(new{Passed=true,LexicalOrder=args[9],OriginalStatus=originalStatus,ActualDiscoveryBarrier=true,ActualCheckPendingRow=true,ActualCheckerExit="remarked",OriginalConnectWaiter=true,SameRuntimePendingHandle=true,ManagedCheckPendingStateClaim=false},json));
 Term peerIdentity=peer.Call("identity");DriverParentHarnessAccess423.Require(peerIdentity.Equals(Term.Tuple(Term.A(peerNode),Term.I(link.RemoteCreation),Term.A("true"))));Save("peer-handshake-identity",peerIdentity,peerIdentity);
 await RemotePair("real-node-visible","seen_node",[Term.A(oracleNode)],[Term.A(managedNode)]);
 Save("peer-sees-managed",peer.Call("seen_node",Term.A(managedNode)),Term.A("true"));
 peer.Call("save_ref",Term.A("foreign"));
 var foreignWire=await RemotePair("genuine-peer-wire-via-distribution","wire",[Term.A("foreign")],[Term.A("foreign")]);
 Term foreignOriginal=Original("decode",foreignWire.Item1),foreignManaged=await Managed("decode",foreignWire.Item2);Save("genuine-peer-reference-via-distribution",foreignOriginal,foreignManaged);
 Save("foreign-reject-before-disconnect",Original("demon",foreignOriginal),await Managed("demon",foreignManaged));
 await Step("local-queued","save_ref",Term.A("roundtrip"));var first=await Wires("roundtrip");
 var decoded=await RemotePair("decode-on-real-peer","decode",[first.Item1],[first.Item2]);
 await RemotePair("peer-foreign-reject","demon",[decoded.Item1],[decoded.Item2]);
 await Step("queue-one-after-peer-refusal","waiting");
 var returned=await RemotePair("wire-from-real-peer","encode",[decoded.Item1],[decoded.Item2],false);
 DriverParentHarnessAccess423.Require(((BitString)returned.Item1).ToArray().AsSpan().SequenceEqual(first.Item1.ToArray())&&((BitString)returned.Item2).ToArray().AsSpan().SequenceEqual(first.Item2.ToArray()));
 Save("returned-ref-cancels-owner",Original("demon_binary",returned.Item1),await Managed("demon_binary",returned.Item2));await Step("queue-zero-after-roundtrip","waiting");
 await Step("monitor-held-across-alive","save_ref",Term.A("handoff"));var handoff=await Wires("handoff");
 var kept=await RemotePair("peer-stores-before-alive","decode",[handoff.Item1],[handoff.Item2]);
 await RemotePair("peer-retains-across-alive","store_term",[Term.A("handoff_oracle"),kept.Item1],[Term.A("handoff_managed"),kept.Item2]);
 await Step("queue-one-before-alive","waiting");
 var live=runtime.DistributionPending.ConnectedSnapshot(link.Handle);
 Save("original-controller-before-alive",peer.Call("connection_row",Term.A(managedNode)),Term.A("captured"),false);
 await using(var refused=new DistributionTcp452(runtime,Path.Combine(input.OutputDirectory,"alive-false")))
 {
  DriverParentHarnessAccess423.Require(!await refused.Connect(port,peerNode,managedNode,listener.Creation,aliveReply:"false"));
 }
 peer.Call("connection_row",Term.A(managedNode));
 DriverParentHarnessAccess423.Require(runtime.DistributionPending.ConnectedSnapshot(link.Handle).ConnectionId==live.ConnectionId);
 await RemotePair("current-data-after-alive-false","seen_node",[Term.A(oracleNode)],[Term.A(managedNode)]);
 await using(var refused=new DistributionTcp452(runtime,Path.Combine(input.OutputDirectory,"alive-wrong-cookie")))
 {
  DriverParentHarnessAccess423.Require(!await refused.Connect(port,peerNode,managedNode,listener.Creation,aliveReply:"true",wrongCookie:true));
 }
 peer.Call("connection_row",Term.A(managedNode));
 DriverParentHarnessAccess423.Require(runtime.DistributionPending.ConnectedSnapshot(link.Handle).ConnectionId==live.ConnectionId);
 await RemotePair("current-data-after-alive-bad-cookie","seen_node",[Term.A(oracleNode)],[Term.A(managedNode)]);

 async Task<(DistributionTcp452 Candidate,Task<bool> Task,TaskCompletionSource<bool> Release)> HoldCandidate(string name,string reply,Func<Task>? retire=null)
 {
  var candidate=new DistributionTcp452(runtime,Path.Combine(input.OutputDirectory,name));
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
  await using(var extra=new DistributionTcp452(runtime,Path.Combine(input.OutputDirectory,phase+"-extra")))
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
 var priorLink=link;priorLink.ExpectRemoteExit=true;var handoffOldHandle=priorLink.Handle;
 peer.Call("trace_start");
 var heldTrue=await HoldCandidate("alive-handoff","true",async()=>
 {
  await priorLink.RemoteClosed.WaitAsync(TimeSpan.FromSeconds(10));await priorLink.DisposeAsync();
 });
 await RejectExtra("already-pending-true");
 await RemotePair("current-data-while-true-candidate-held","seen_node",[Term.A(oracleNode)],[Term.A(managedNode)]);
 heldTrue.Release.TrySetResult(true);DriverParentHarnessAccess423.Require(await heldTrue.Task);var replacement=heldTrue.Candidate;
 Save("original-already-pending-true-traces",peer.Call("trace_finish"),Term.A("captured"),false);
 link=replacement;var afterHandoff=runtime.DistributionPending.ConnectedSnapshot(link.Handle);
 DriverParentHarnessAccess423.Require(afterHandoff.ConnectionId!=live.ConnectionId&&afterHandoff.RemoteCreation==live.RemoteCreation&&priorLink.FinalSnapshot is{QueuedPackets:0,QueuedPacketBytes:0,QueuedSegments:0});
 File.WriteAllText(Path.Combine(input.OutputDirectory,"alive-handoff-proof.json"),JsonSerializer.Serialize(new{Passed=true,Old=live,Current=afterHandoff,OldControllerJoined=true,RetiredBeforeHostAckRead=true,SamePeerVmCreation=true},json));
 Save("original-controller-after-alive",peer.Call("connection_row",Term.A(managedNode)),Term.A("captured"),false);
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

 var handoffStale=await link.ProbeStale(handoffOldHandle,File.ReadAllBytes(Path.Combine(input.OutputDirectory,(incomingExpected?"simultaneous-incoming":"simultaneous-outgoing")+"/input-0001.bin")));
 File.WriteAllText(Path.Combine(input.OutputDirectory,"alive-old-handler.json"),JsonSerializer.Serialize(handoffStale,json));
 var retrieved=await RemotePair("peer-keeps-memory-across-alive","stored",[Term.A("handoff_oracle")],[Term.A("handoff_managed")]);
 var back=await RemotePair("held-reference-after-alive","encode",[retrieved.Item1],[retrieved.Item2],false);
 DriverParentHarnessAccess423.Require(((BitString)back.Item1).ToArray().AsSpan().SequenceEqual(handoff.Item1.ToArray())&&((BitString)back.Item2).ToArray().AsSpan().SequenceEqual(handoff.Item2.ToArray()));
 Save("alive-return-cancels-owner",Original("demon_binary",back.Item1),await Managed("demon_binary",back.Item2));
 await Step("queue-zero-after-alive-return","waiting");
 await Step("monitor-held-across-disconnect","save_ref",Term.A("held"));var held=await Wires("held");await Step("queue-one-before-disconnect","waiting");
 var beforeDisconnect=await RemotePair("actual-reference-crosses-before-disconnect","decode",[held.Item1],[held.Item2]);
 await RemotePair("peer-retains-before-disconnect","store_term",[Term.A("held_oracle"),beforeDisconnect.Item1],[Term.A("held_managed"),beforeDisconnect.Item2]);
 await Step("watch-peer-before-death","watch_node",Term.A(peerNode));
 var oldHandle=link.Handle;uint creation=link.RemoteCreation;uint oldConnection=runtime.DistributionPending.ConnectedSnapshot(oldHandle).ConnectionId;
 byte[] frozenForeign=ExternalTermFormat.Encode(foreignManaged);uint frozenCreation=((ReferenceTerm)foreignManaged).Creation;
 using var descriptor=JsonDocument.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(input.OutputDirectory)!,"peer.live.json")));
 using var ownedOldPeer=Process.GetProcessById(descriptor.RootElement.GetProperty("Pid").GetInt32());
 link.ExpectRemoteExit=true;peerStopped=true;FatalPeer452.Terminate(ownedOldPeer,Path.Combine(input.OutputDirectory,"fatal-peer-stop.json"));await link.RemoteClosed.WaitAsync(TimeSpan.FromSeconds(10));
 await link.DisposeAsync();DriverParentHarnessAccess423.Require(link.FinalSnapshot is{AcceptedInputPackets:>0,ConsumedOutputPackets:>0});link=null;
 await Step("actual-node-down-after-peer-halt","await_down",Term.A(peerNode));
 await Step("actual-peer-absent-after-halt","seen_node",Term.A(peerNode));
 DriverParentHarnessAccess423.Require(Original("seen_node",Term.A(peerNode)).Equals(Term.A("false"))&&(await Managed("seen_node",Term.A(peerNode))).Equals(Term.A("false")));
 await Step("queue-one-after-actual-peer-death","waiting");
 await RejectOld("old-handle-before-reconnect");
 peer.Dispose();restarted=new(Path.GetDirectoryName(input.OutputDirectory)!,ownedOldPeer);peer=new(restarted.Port);peerStopped=false;
 Term restartedIdentity=peer.Call("identity");DriverParentHarnessAccess423.Require(restartedIdentity is TupleTerm ri&&ri.Items[0].Equals(Term.A(peerNode))&&!ri.Items[1].Equals(Term.I(creation))&&ri.Items[2].Equals(Term.A("true")));
 Save("restarted-peer-identity",restartedIdentity,restartedIdentity);
 Save("restarted-peer-setup",peer.Call("setup",Term.String(input.DriverDirectory)),Term.A("ok"));peer.Call("depth",Term.I(depth));
 link=new(runtime,Path.Combine(input.OutputDirectory,"connection2"));var nextAccept=link.Accept(listener,peerNode,managedNode,listener.Creation);
 DriverParentHarnessAccess423.Require(peer.Call("connect_peer",Term.A(managedNode)).Equals(Term.A("true")));await nextAccept;
 DriverParentHarnessAccess423.Require(link.RemoteCreation!=creation&&runtime.DistributionPending.ConnectedSnapshot(link.Handle).ConnectionId!=oldConnection&&restartedIdentity.Equals(Term.Tuple(Term.A(peerNode),Term.I(link.RemoteCreation),Term.A("true"))));
 DriverParentHarnessAccess423.Require(((ReferenceTerm)foreignManaged).Creation==frozenCreation&&ExternalTermFormat.Encode(foreignManaged).AsSpan().SequenceEqual(frozenForeign));
 File.WriteAllText(Path.Combine(input.OutputDirectory,"actual-peer-restart.json"),JsonSerializer.Serialize(new{Passed=true,OldPid=ownedOldPeer.Id,NewPid=restarted.ProcessId,OldCreation=creation,NewCreation=link.RemoteCreation,OldConnection=oldConnection,NewConnection=runtime.DistributionPending.ConnectedSnapshot(link.Handle).ConnectionId,OldReferenceCreation=frozenCreation,OldReferenceWireUnchanged=true},json));
 await RejectOld("old-handle-after-reconnect");
 var stale=await link.ProbeStale(oldHandle,File.ReadAllBytes(Path.Combine(input.OutputDirectory,"alive-handoff/input-0001.bin")));File.WriteAllText(Path.Combine(input.OutputDirectory,"old-handle-current-input-handler.json"),JsonSerializer.Serialize(stale,json));
 long beforePositive=runtime.DistributionPending.ConnectedSnapshot(link.Handle).AcceptedInputPackets;
 await RemotePair("real-node-visible-after-reconnect","seen_node",[Term.A(oracleNode)],[Term.A(managedNode)]);
 DriverParentHarnessAccess423.Require(runtime.DistributionPending.ConnectedSnapshot(link.Handle).AcceptedInputPackets>beforePositive);
 Save("foreign-reject-after-reconnect",Original("demon",foreignOriginal),await Managed("demon",foreignManaged));
 var lost=await RemotePair("peer-memory-lost-after-actual-restart","stored",[Term.A("held_oracle")],[Term.A("held_managed")]);
 DriverParentHarnessAccess423.Require(lost.Item1.Equals(Term.A("undefined"))&&lost.Item2.Equals(Term.A("undefined")));
 var heldDecoded=await RemotePair("surviving-local-reference-sent-to-new-vm","decode",[held.Item1],[held.Item2]);
 await RemotePair("held-ref-peer-foreign-reject","demon",[heldDecoded.Item1],[heldDecoded.Item2]);
 await Step("queue-one-reconnect-peer-refusal","waiting");
 var heldReturn=await RemotePair("held-ref-wire-after-reconnect","encode",[heldDecoded.Item1],[heldDecoded.Item2],false);
 DriverParentHarnessAccess423.Require(((BitString)heldReturn.Item1).ToArray().AsSpan().SequenceEqual(held.Item1.ToArray())&&((BitString)heldReturn.Item2).ToArray().AsSpan().SequenceEqual(held.Item2.ToArray()));
 Save("held-ref-return-cancels-owner",Original("demon_binary",heldReturn.Item1),await Managed("demon_binary",heldReturn.Item2));await Step("queue-zero-reconnect-return","waiting");
 await Step("repeat-completed-ref-after-reconnect","demon_saved",Term.A("held"));
 peer.Call("save_ref",Term.A("new_epoch"));
 var freshWire=await RemotePair("new-peer-reference-wire","wire",[Term.A("new_epoch")],[Term.A("new_epoch")]);
 Term freshOriginal=Original("decode",freshWire.Item1),freshManaged=await Managed("decode",freshWire.Item2);
 DriverParentHarnessAccess423.Require(((ReferenceTerm)freshManaged).Creation==link.RemoteCreation&&((ReferenceTerm)freshOriginal).Creation==link.RemoteCreation);
 Save("new-peer-reference",freshOriginal,freshManaged,false);
 var stalePeer=await RemotePair("old-peer-ref-rejected-by-new-peer","demon",[foreignOriginal],[foreignManaged]);
 foreach(var term in new[]{stalePeer.Item1,stalePeer.Item2})DriverParentHarnessAccess423.Require(term is TupleTerm t&&t.Items[0].Equals(Term.A("exception"))&&t.Items[2].Equals(Term.A("badarg")));
 var q=await RemotePair("new-peer-queue-survives-old-ref","waiting",[],[]);DriverParentHarnessAccess423.Require(q.Item1.Equals(Term.I(1))&&q.Item2.Equals(Term.I(1)));
 await RemotePair("new-peer-reference-cancels-new-owner","demon",[freshOriginal],[freshManaged]);
 var z=await RemotePair("new-peer-queue-zero-after-new-ref","waiting",[],[]);DriverParentHarnessAccess423.Require(z.Item1.Equals(Term.I(0))&&z.Item2.Equals(Term.I(0)));
 await Step("physical-local-unload","cleanup");Save("physical-peer-unload",peer.Call("cleanup"),Term.Tuple(Term.Tuple(Term.A("ok"),Term.A("pending_driver")),Term.A("true"),Term.A("unloaded")));
 runtime.Send(worker,Term.A("stop"));
 async Task RejectOld(string name)
 {
  bool rejected=false;string? failure=null;
  try{_ = runtime.DistributionPending.ConnectedSnapshot(oldHandle);}
  catch(Exception error) when(error is ArgumentException or InvalidOperationException){rejected=true;failure=error.ToString();}
  File.WriteAllText(Path.Combine(input.OutputDirectory,name+".json"),JsonSerializer.Serialize(new{Passed=rejected,Failure=failure,OldConnection=oldConnection},json));DriverParentHarnessAccess423.Require(rejected);
 }
}
catch(Exception error){errors.Add(error.ToString());}
finally
{
 if(link is not null)try{await link.DisposeAsync();}catch(Exception error){errors.Add("distribution cleanup: "+error);}
 if(fixture is not null)try{await fixture.DisposeAsync();DriverParentHarnessAccess423.ZeroLeases(fixture.Runtime,fixture.Environment,true);}catch(Exception error){errors.Add("native cleanup: "+error);}
 try{if(!peerStopped)DriverParentHarnessAccess423.Require(peer.Call("race_cleanup").Equals(Term.A("ok")));}catch(Exception error){errors.Add("original race cleanup: "+error);}
 try{oracle.Stop();}catch(Exception error){errors.Add("oracle stop: "+error);}try{if(!peerStopped)peer.Stop();}catch(Exception error){errors.Add("peer stop: "+error);}
 peer.Dispose();try{restarted?.Dispose();}catch(Exception error){errors.Add("restarted peer cleanup: "+error);}listener?.Dispose();
}
bool settled=child?.PhysicalExit.IsCompletedSuccessfully==true&&child.PhysicalCleanup.IsCompletedSuccessfully&&child.Completion.IsCompletedSuccessfully;
File.WriteAllText(Path.Combine(input.OutputDirectory,"comparison.json"),JsonSerializer.Serialize(new{Passed=errors.Count==0&&settled,Depth=depth,Mode=raceMode,Results=results,Failures=errors,PhysicalCleanup=settled,Transport="Original queued late discovery and actual old timer across owner replacement; actual runtime input/output queues",WholeModuleClaim=false},json));
DriverParentHarnessAccess423.VerifyInputs(input);return errors.Count==0&&settled?0:1;
static Term Unwrap(Term value)
{
 if(value is TupleTerm{Items.Count:2} t&&t.Items[0].Equals(Term.A("ok")))return t.Items[1];throw new Exception("Worker failed: "+value);
}
static bool Equal(Term e,Term a,List<(ReferenceTerm Expected,ReferenceTerm Actual)> bindings)
{
 if(e.Equals(a))return true;
 if(e is ReferenceTerm er&&a is ReferenceTerm ar)return bindings.Any(b=>b.Expected.Equals(er)&&b.Actual.Equals(ar));
 if(e is TupleTerm et&&a is TupleTerm at)return et.Items.Count==at.Items.Count&&et.Items.Zip(at.Items).All(p=>Equal(p.First,p.Second,bindings));
 if(e is Cons ec&&a is Cons ac)return Equal(ec.Head,ac.Head,bindings)&&Equal(ec.Tail,ac.Tail,bindings);
 return false;
}
