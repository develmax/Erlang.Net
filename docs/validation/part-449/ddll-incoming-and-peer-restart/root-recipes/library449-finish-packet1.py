from pathlib import Path
p=Path('artifacts/library449-probe1/Program.cs');s=p.read_text()
s=s.replace('using System.Text.Json;','using System.Text.Json;\nusing System.Diagnostics;')
s=s.replace('using var oracle=new Peer449(int.Parse(args[1]));using var peer=new Peer449(int.Parse(args[2]));','using var oracle=new Peer449(int.Parse(args[1]));var peer=new Peer449(int.Parse(args[2]));\nRestartPeer449? restarted=null;DistributionListener449? listener=null;bool peerStopped=false;')
s=s.replace('Term managedIdentity=await Managed("start_node",Term.A(managedNode));','listener=new(int.Parse(args[8]),managedNode,Path.Combine(input.OutputDirectory,"listener"));\n Term managedIdentity=await Managed("start_node_creation",Term.A(managedNode),Term.I(listener.Creation));')
s=s.replace('Term.I(4),Term.A("true")','Term.I(listener.Creation),Term.A("true")')
s=s.replace('  await bad.Connect(port,peerNode,"ddll449bad@127.0.0.1",4,true);DriverParentHarnessAccess423.Require(!bad.Authenticated);','  peer.Call("set_peer_cookie",Term.A(managedNode),Term.A(DistributionTcp449.FixtureCookie+"_wrong"));\n  var badAccept=bad.Accept(listener,peerNode,managedNode,listener.Creation,true);\n  DriverParentHarnessAccess423.Require(peer.Call("ping_node",Term.A(managedNode)).Equals(Term.A("pang")));await badAccept;\n  DriverParentHarnessAccess423.Require(!bad.Authenticated);\n  peer.Call("set_peer_cookie",Term.A(managedNode),Term.A(DistributionTcp449.FixtureCookie));')
s=s.replace('link=new(runtime,Path.Combine(input.OutputDirectory,"connection1"));await link.Connect(port,peerNode,managedNode,4);','link=new(runtime,Path.Combine(input.OutputDirectory,"connection1"));var firstAccept=link.Accept(listener,peerNode,managedNode,listener.Creation);\n DriverParentHarnessAccess423.Require(peer.Call("ping_node",Term.A(managedNode)).Equals(Term.A("pong")));await firstAccept;')
start=s.index(' peer.Call("watch_node",Term.A(managedNode));');end=s.index(' await RejectOld("old-handle-after-reconnect");',start)
s=s[:start]+''' await Step("watch-peer-before-death","watch_node",Term.A(peerNode));
 var oldHandle=link.Handle;uint creation=link.RemoteCreation;uint oldConnection=runtime.DistributionPending.ConnectedSnapshot(oldHandle).ConnectionId;
 byte[] frozenForeign=ExternalTermFormat.Encode(foreignManaged);uint frozenCreation=((ReferenceTerm)foreignManaged).Creation;
 using var descriptor=JsonDocument.Parse(File.ReadAllText(Path.Combine(Path.GetDirectoryName(input.OutputDirectory)!,"peer.live.json")));
 using var ownedOldPeer=Process.GetProcessById(descriptor.RootElement.GetProperty("Pid").GetInt32());
 link.ExpectRemoteExit=true;peer.Stop();peerStopped=true;await link.RemoteClosed.WaitAsync(TimeSpan.FromSeconds(10));
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
 DriverParentHarnessAccess423.Require(peer.Call("ping_node",Term.A(managedNode)).Equals(Term.A("pong")));await nextAccept;
 DriverParentHarnessAccess423.Require(link.RemoteCreation!=creation&&runtime.DistributionPending.ConnectedSnapshot(link.Handle).ConnectionId!=oldConnection&&restartedIdentity.Equals(Term.Tuple(Term.A(peerNode),Term.I(link.RemoteCreation),Term.A("true")))));
 DriverParentHarnessAccess423.Require(((ReferenceTerm)foreignManaged).Creation==frozenCreation&&ExternalTermFormat.Encode(foreignManaged).AsSpan().SequenceEqual(frozenForeign));
 File.WriteAllText(Path.Combine(input.OutputDirectory,"actual-peer-restart.json"),JsonSerializer.Serialize(new{Passed=true,OldPid=ownedOldPeer.Id,NewPid=restarted.ProcessId,OldCreation=creation,NewCreation=link.RemoteCreation,OldConnection=oldConnection,NewConnection=runtime.DistributionPending.ConnectedSnapshot(link.Handle).ConnectionId,OldReferenceCreation=frozenCreation,OldReferenceWireUnchanged=true},json));
''' +s[end:]
s=s.replace('var heldDecoded=await RemotePair("actual-peer-retained-ref-after-reconnect","stored",[Term.A("held_oracle")],[Term.A("held_managed")]);','var lost=await RemotePair("peer-memory-lost-after-actual-restart","stored",[Term.A("held_oracle")],[Term.A("held_managed")]);\n DriverParentHarnessAccess423.Require(lost.Item1.Equals(Term.A("undefined"))&&lost.Item2.Equals(Term.A("undefined")));\n var heldDecoded=await RemotePair("surviving-local-reference-sent-to-new-vm","decode",[held.Item1],[held.Item2]);')
needle=' await Step("physical-local-unload","cleanup");'
s=s.replace(needle,''' peer.Call("save_ref",Term.A("new_epoch"));
 var freshWire=await RemotePair("new-peer-reference-wire","wire",[Term.A("new_epoch")],[Term.A("new_epoch")]);
 Term freshOriginal=Original("decode",freshWire.Item1),freshManaged=await Managed("decode",freshWire.Item2);
 DriverParentHarnessAccess423.Require(((ReferenceTerm)freshManaged).Creation==link.RemoteCreation&&((ReferenceTerm)freshOriginal).Creation==link.RemoteCreation);
 Save("new-peer-reference","identity",Term.A("identity"),false);
 var stalePeer=await RemotePair("old-peer-ref-rejected-by-new-peer","demon",[foreignOriginal],[foreignManaged]);
 foreach(var term in new[]{stalePeer.Item1,stalePeer.Item2})DriverParentHarnessAccess423.Require(term is TupleTerm t&&t.Items[0].Equals(Term.A("exception"))&&t.Items[2].Equals(Term.A("badarg")));
 var q=await RemotePair("new-peer-queue-survives-old-ref","waiting",[],[]);DriverParentHarnessAccess423.Require(q.Item1.Equals(Term.I(1))&&q.Item2.Equals(Term.I(1)));
 await RemotePair("new-peer-reference-cancels-new-owner","demon",[freshOriginal],[freshManaged]);
 var z=await RemotePair("new-peer-queue-zero-after-new-ref","waiting",[],[]);DriverParentHarnessAccess423.Require(z.Item1.Equals(Term.I(0))&&z.Item2.Equals(Term.I(0)));
''' +needle)
# Preserve raw fresh identities separately; compared references must have explicit captured correspondence.
s=s.replace(' Save("new-peer-reference","identity",Term.A("identity"),false);',' Save("new-peer-reference",freshOriginal,freshManaged,false);')
s=s.replace(' try{oracle.Stop();}catch(Exception error){errors.Add("oracle stop: "+error);}try{peer.Stop();}catch(Exception error){errors.Add("peer stop: "+error);}', ' try{oracle.Stop();}catch(Exception error){errors.Add("oracle stop: "+error);}try{if(!peerStopped)peer.Stop();}catch(Exception error){errors.Add("peer stop: "+error);}\n peer.Dispose();try{restarted?.Dispose();}catch(Exception error){errors.Add("restarted peer cleanup: "+error);}listener?.Dispose();')
s=s.replace('Authenticated real Erlang distribution v6 and physical TCP disconnect/reconnect','Incoming authenticated Erlang distribution v6 and actual remote OS VM halt/restart')
p.write_text(s)
p=Path('artifacts/library449-probe1/RestartPeer449.cs');s=p.read_text().replace('RestartPeer449(string groupDirectory)','RestartPeer449(string groupDirectory, Process old)').replace('using var old = Process.GetProcessById(oldId);','if (old.Id != oldId) throw new IOException("Owned peer identity mismatch");');p.write_text(s)
p=Path('artifacts/library449-probe1/DistributionTcp449.cs');s=p.read_text().replace('internal Task RemoteClosed=>remoteClosed.Task;','internal Task RemoteClosed=>remoteClosed.Task;\n internal bool ExpectRemoteExit {get;set;}').replace('catch(EndOfStreamException){remoteClosed.TrySetResult(true);}','catch(Exception error) when(ExpectRemoteExit&&error is EndOfStreamException or IOException){remoteClosed.TrySetResult(true);}');p.write_text(s)
