from pathlib import Path
import shutil,json
r=Path.cwd();old=r/'artifacts/library449-probe7';out=r/'artifacts/library450-probe1';assert not out.exists();out.mkdir()
for f in old.glob('*'):
 if f.is_file():(out/f.name.replace('449','450')).write_text(f.read_text().replace('449','450'))
p=out/'run-root.py';s=p.read_text().replace("p.add_argument('--depths',default='0,1,2,8');", "p.add_argument('--depths',default='0,1,2,8');p.add_argument('--orders',default='incoming,outgoing');")
s=s.replace("for depth in map(int,a.depths.split(',')):","for depth,order in [(d,o) for d in map(int,a.depths.split(',')) for o in a.orders.split(',')]:")
s=s.replace("group=out/('depth'+str(depth))", "group=out/('depth'+str(depth)+'-'+order)")
s=s.replace("nodes[role]='ddll450'+('oracle' if role=='primary' else 'peer')+str(depth)+'@127.0.0.1'", "nodes[role]=('ddll450oracle' if role=='primary' else ('ddll450z_peer' if order=='incoming' else 'ddll450a_peer'))+str(depth)+order+'@127.0.0.1'")
s=s.replace("'ddll450managed'+str(depth)+'@127.0.0.1',str(epmd_port)]", "('ddll450a_managed' if order=='incoming' else 'ddll450z_managed')+str(depth)+order+'@127.0.0.1',str(epmd_port),order]")
s=s.replace("'compare-depth'+str(depth)","'compare-depth'+str(depth)+'-'+order")
s=s.replace("receipt={'Pid':process.pid", "fatal=group/'observations/fatal-peer-stop.json';fatal_body=json.loads(fatal.read_bytes()) if fatal.exists() else {};expected_kill=role=='peer' and fatal_body.get('Passed') is True and fatal_body.get('OldPid')==process.pid and fatal_body.get('KillRequested') is True and process.returncode!=0\n    receipt={'ExpectedScenarioTermination':expected_kill,'ForcedCleanup':forced,'Pid':process.pid")
s=s.replace("if forced or process.returncode!=0:group_errors.append(role+' did not exit normally')", "if forced or (process.returncode!=0 and not expected_kill):group_errors.append(role+' did not exit normally or by its validated scenario kill')")
s=s.replace("groups.append({'Depth':depth,", "groups.append({'Depth':depth,'Order':order,")
s=s.replace("len(groups)==len(a.depths.split(','))", "len(groups)==len(a.depths.split(','))*len(a.orders.split(','))")
s=s.replace("'Transport':'two real distributed OTP OS nodes, authenticated Erlang distribution v6, physical TCP disconnect/reconnect and actual runtime data queues'", "'Transport':'actual original pending collisions/alive status, runtime data queues and deliberate owned OS peer-tree termination/restart'")
p.write_text(s)
p=out/'ddll_tcp450.erl';s=p.read_text().replace('{set_peer_cookie,[Node,Cookie]} -> {ok,erlang:set_cookie(Node,Cookie)};', '{set_peer_cookie,[Node,Cookie]} -> {ok,erlang:set_cookie(Node,Cookie)};\n                        {connection_row,[Node]} -> {ok,wait_up(Node,5000)};')
s+='\nwait_up(Node,0)->error({connection_not_up,Node,ets:lookup(sys_dist,Node)});\nwait_up(Node,N)->case ets:lookup(sys_dist,Node) of\n [Row] when element(4,Row)=:=up->Row;\n _->receive after 1->wait_up(Node,N-1) end end.\n';p.write_text(s)
p=out/'DistributionTcp450.cs';s=p.read_text().replace('internal sealed class DistributionTcp450','internal sealed partial class DistributionTcp450')
s=s.replace('internal bool Authenticated {get;private set;}','internal bool Authenticated {get;private set;}\n internal TaskCompletionSource<bool> HelloReceived {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);')
s=s.replace('bool wrongCookie=false)','bool wrongCookie=false,Task<string>? statusDecision=null)')
s=s.replace('  byte[] status="sok"u8.ToArray();','''  HelloReceived.TrySetResult(true);string selected=statusDecision is null?"ok":await statusDecision.WaitAsync(deadline.Token);
  if(selected=="retired")
  {
   bool closed=false;try{await Read(2,deadline.Token);}catch(EndOfStreamException){closed=true;}
   DriverParentHarnessAccess423.Require(closed);client.Dispose();File.WriteAllText(Path.Combine(directory,"candidate-refusal.json"),JsonSerializer.Serialize(new{OriginalOutgoingSetupClosed=true,Adopted=false}));return;
  }
  byte[] status=Encoding.Latin1.GetBytes("s"+selected);''')
p.write_text(s)
p=out/'Program.cs';s=p.read_text();start=s.index(' await using(var bad=');end=s.index(' DriverParentHarnessAccess423.Require(link.Authenticated);',start)
s=s[:start]+''' var decision=new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
 var incoming=new DistributionTcp450(runtime,Path.Combine(input.OutputDirectory,"simultaneous-incoming"));
 var accepting=incoming.Accept(listener,peerNode,managedNode,listener.Creation,statusDecision:decision.Task);
 var actualOriginalConnect=Task.Run(()=>peer.Call("connect_peer",Term.A(managedNode)));
 await incoming.HelloReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
 var outgoing=new DistributionTcp450(runtime,Path.Combine(input.OutputDirectory,"simultaneous-outgoing"));string? originalStatus=null;
 bool outgoingWon=await outgoing.Connect(port,peerNode,managedNode,listener.Creation,statusObserved:status=>
 {
  originalStatus=status;decision.TrySetResult(status=="nok"?"ok_simultaneous":"retired");
 });
 await accepting;DriverParentHarnessAccess423.Require((await actualOriginalConnect).Equals(Term.A("true")));
 bool incomingExpected=string.CompareOrdinal(managedNode,peerNode)<0;
 DriverParentHarnessAccess423.Require(originalStatus==(incomingExpected?"nok":"ok_simultaneous")&&outgoingWon!=incomingExpected&&incoming.Authenticated==incomingExpected);
 link=incomingExpected?incoming:outgoing;await (incomingExpected?outgoing:incoming).DisposeAsync();
 File.WriteAllText(Path.Combine(input.OutputDirectory,"simultaneous-selection.json"),JsonSerializer.Serialize(new{Passed=true,Order=args[9],Managed=managedNode,Peer=peerNode,OriginalStatus=originalStatus,IncomingSelected=incomingExpected,ActualOriginalConnect=true,LoserAdopted=false},json));
''' +s[end:]
needle=' await Step("monitor-held-across-disconnect","save_ref",Term.A("held"));'
insert=''' await Step("monitor-held-across-alive","save_ref",Term.A("handoff"));var handoff=await Wires("handoff");
 var kept=await RemotePair("peer-stores-before-alive","decode",[handoff.Item1],[handoff.Item2]);
 await RemotePair("peer-retains-across-alive","store_term",[Term.A("handoff_oracle"),kept.Item1],[Term.A("handoff_managed"),kept.Item2]);
 await Step("queue-one-before-alive","waiting");
 var live=runtime.DistributionPending.ConnectedSnapshot(link.Handle);
 Save("original-controller-before-alive",peer.Call("connection_row",Term.A(managedNode)),Term.A("captured"),false);
 await using(var refused=new DistributionTcp450(runtime,Path.Combine(input.OutputDirectory,"alive-false")))
 {
  DriverParentHarnessAccess423.Require(!await refused.Connect(port,peerNode,managedNode,listener.Creation,aliveReply:"false"));
 }
 peer.Call("connection_row",Term.A(managedNode));
 DriverParentHarnessAccess423.Require(runtime.DistributionPending.ConnectedSnapshot(link.Handle).ConnectionId==live.ConnectionId);
 await RemotePair("current-data-after-alive-false","seen_node",[Term.A(oracleNode)],[Term.A(managedNode)]);
 await using(var refused=new DistributionTcp450(runtime,Path.Combine(input.OutputDirectory,"alive-wrong-cookie")))
 {
  DriverParentHarnessAccess423.Require(!await refused.Connect(port,peerNode,managedNode,listener.Creation,aliveReply:"true",wrongCookie:true));
 }
 peer.Call("connection_row",Term.A(managedNode));
 DriverParentHarnessAccess423.Require(runtime.DistributionPending.ConnectedSnapshot(link.Handle).ConnectionId==live.ConnectionId);
 await RemotePair("current-data-after-alive-bad-cookie","seen_node",[Term.A(oracleNode)],[Term.A(managedNode)]);
 var priorLink=link;priorLink.ExpectRemoteExit=true;var handoffOldHandle=priorLink.Handle;
 var replacement=new DistributionTcp450(runtime,Path.Combine(input.OutputDirectory,"alive-handoff"));
 DriverParentHarnessAccess423.Require(await replacement.Connect(port,peerNode,managedNode,listener.Creation,aliveReply:"true",retireOld:async()=>
 {
  await priorLink.RemoteClosed.WaitAsync(TimeSpan.FromSeconds(10));await priorLink.DisposeAsync();
 }));
 link=replacement;var afterHandoff=runtime.DistributionPending.ConnectedSnapshot(link.Handle);
 DriverParentHarnessAccess423.Require(afterHandoff.ConnectionId!=live.ConnectionId&&afterHandoff.RemoteCreation==live.RemoteCreation&&priorLink.FinalSnapshot is{QueuedPackets:0,QueuedPacketBytes:0,QueuedSegments:0});
 File.WriteAllText(Path.Combine(input.OutputDirectory,"alive-handoff-proof.json"),JsonSerializer.Serialize(new{Passed=true,Old=live,Current=afterHandoff,OldControllerJoined=true,RetiredBeforeHostAckRead=true,SamePeerVmCreation=true},json));
 Save("original-controller-after-alive",peer.Call("connection_row",Term.A(managedNode)),Term.A("captured"),false);
 var handoffStale=await link.ProbeStale(handoffOldHandle,File.ReadAllBytes(Path.Combine(input.OutputDirectory,(incomingExpected?"simultaneous-incoming":"simultaneous-outgoing")+"/input-0001.bin")));
 File.WriteAllText(Path.Combine(input.OutputDirectory,"alive-old-handler.json"),JsonSerializer.Serialize(handoffStale,json));
 var retrieved=await RemotePair("peer-keeps-memory-across-alive","stored",[Term.A("handoff_oracle")],[Term.A("handoff_managed")]);
 var back=await RemotePair("held-reference-after-alive","encode",[retrieved.Item1],[retrieved.Item2],false);
 DriverParentHarnessAccess423.Require(((BitString)back.Item1).ToArray().AsSpan().SequenceEqual(handoff.Item1.ToArray())&&((BitString)back.Item2).ToArray().AsSpan().SequenceEqual(handoff.Item2.ToArray()));
 Save("alive-return-cancels-owner",Original("demon_binary",back.Item1),await Managed("demon_binary",back.Item2));
 await Step("queue-zero-after-alive-return","waiting");
'''
assert needle in s;s=s.replace(needle,insert+needle)
s=s.replace('link.ExpectRemoteExit=true;peer.Stop();peerStopped=true;await link.RemoteClosed.WaitAsync(TimeSpan.FromSeconds(10));','link.ExpectRemoteExit=true;peerStopped=true;FatalPeer450.Terminate(ownedOldPeer,Path.Combine(input.OutputDirectory,"fatal-peer-stop.json"));await link.RemoteClosed.WaitAsync(TimeSpan.FromSeconds(10));')
s=s.replace('"connection1/input-0001.bin"','"alive-handoff/input-0001.bin"')
s=s.replace('Incoming authenticated Erlang distribution v6 and actual remote OS VM halt/restart','Original simultaneous/alive handoff and deliberate original OS peer-tree termination/restart')
p.write_text(s)
struct=(r/'tools/Erlang.HaltProbe/HaltWindowsDumpProcessEntry437.cs').read_text().replace('namespace Erlang.HaltProbe;','').replace('HaltWindowsDumpProcessEntry437','FatalProcessEntry450').replace('HaltWindowsDumpNames437.MaxPath','260');(out/'FatalProcessEntry450.cs').write_text(struct)
(out/'reviewed-successor.json').write_text(json.dumps({'Previous':'artifacts/library449-probe7','Scope':'Actual original pending collision in both lexical orders; alive false/badcookie/true; expected owned OS peer-tree kill and restart. Fixed fixture cookie is nonsecret. All original data paths remain actual runtime queues.'},indent=2)+'\n')
print('Part450 packet prepared')
