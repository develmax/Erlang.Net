using System.Text.Json;
using Erlang;

var json=new JsonSerializerOptions{WriteIndented=true,PropertyNameCaseInsensitive=true};
var input=JsonSerializer.Deserialize<DriverParentHarnessInput423>(File.ReadAllText(args[0]),json)!;
DriverParentHarnessAccess423.VerifyInputs(input);Directory.CreateDirectory(input.OutputDirectory);
using var oracle=new Peer448(int.Parse(args[1]));using var peer=new Peer448(int.Parse(args[2]));
int depth=int.Parse(args[3]),port=int.Parse(args[4]);string peerNode=args[5],oracleNode=args[6],managedNode=args[7];
var results=new List<object>();var errors=new List<string>();var bindings=new List<(ReferenceTerm Expected,ReferenceTerm Actual)>();
DriverParentHarnessFixture423? fixture=null;ErlDriverHostChild420? child=null;DistributionTcp448? link=null;int sequence=0;
try
{
 fixture=await DriverParentHarnessFixture423.Create(input,"distribution448",false);var runtime=fixture.Runtime;
 child=(ErlDriverHostChild420)DriverParentHarnessAccess423.Field(DriverParentHarnessAccess423.Field(runtime,"driverFrontend422"),"child");
 await runtime.InitializeModulesAsync(registry=>
 {
  var image=registry.PrepareBeam(File.ReadAllBytes(Path.Combine(input.OriginalDirectory,"observer-ebin/beam_ddll_distribution448.beam")),new BeamDependencySelection(input.DeclaredModules.Concat(new[]{"beam_ddll_distribution448"})));
  DriverParentHarnessAccess423.Require(registry.PublishPreparedBeam([image]).Status==BeamPublicationStatus.Published);
  foreach(var e in image.Code.Exports)DriverParentHarnessAccess423.Require(ReferenceEquals(registry.SourceOwner(image.Name,e.Name,e.Arity),image));
 });
 Pid worker=(Pid)await fixture.Actor!.Invoke(async c=>await runtime.Modules.Call(c,"beam_ddll_distribution448","new"));
 async Task<Term> Managed(string f,params Term[] a)=>Unwrap(await fixture.Actor.Invoke(async c=>await runtime.Modules.Call(c,"beam_ddll_distribution448","request",worker,Term.A(f),Term.List(a))));
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
 Term managedIdentity=await Managed("start_node",Term.A(managedNode));Save("own-identities",Original("identity"),managedIdentity,false);
 DriverParentHarnessAccess423.Require(managedIdentity.Equals(Term.Tuple(Term.A(managedNode),Term.I(4),Term.A("true"))));
 await using(var bad=new DistributionTcp448(runtime,Path.Combine(input.OutputDirectory,"wrong-cookie")))
 {
  await bad.Connect(port,peerNode,"ddll448bad@127.0.0.1",4,true);DriverParentHarnessAccess423.Require(!bad.Authenticated);
 }
 link=new(runtime,Path.Combine(input.OutputDirectory,"connection1"));await link.Connect(port,peerNode,managedNode,4);
 DriverParentHarnessAccess423.Require(link.Authenticated);
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
 await Step("monitor-held-across-disconnect","save_ref",Term.A("held"));var held=await Wires("held");await Step("queue-one-before-disconnect","waiting");
 var beforeDisconnect=await RemotePair("actual-reference-crosses-before-disconnect","decode",[held.Item1],[held.Item2]);
 await RemotePair("peer-retains-before-disconnect","store_term",[Term.A("held_oracle"),beforeDisconnect.Item1],[Term.A("held_managed"),beforeDisconnect.Item2]);
 peer.Call("watch_node",Term.A(managedNode));peer.Call("watch_node",Term.A(oracleNode));
 var oldHandle=link.Handle;uint creation=link.RemoteCreation;uint oldConnection=runtime.DistributionPending.ConnectedSnapshot(oldHandle).ConnectionId;
 Save("oracle-disconnect",Original("disconnect_peer",Term.A(peerNode)),Term.A("true"));
 await link.DisposeAsync();DriverParentHarnessAccess423.Require(link.FinalSnapshot is{AcceptedInputPackets:>0,ConsumedOutputPackets:>0});link=null;
 Save("physical-peer-node-down",peer.Call("await_down",Term.A(oracleNode)),peer.Call("await_down",Term.A(managedNode)));
 Save("peer-observes-both-absent",peer.Call("seen_node",Term.A(oracleNode)),peer.Call("seen_node",Term.A(managedNode)));DriverParentHarnessAccess423.Require(peer.Call("seen_node",Term.A(oracleNode)).Equals(Term.A("false"))&&peer.Call("seen_node",Term.A(managedNode)).Equals(Term.A("false")));
 await Step("queue-one-after-physical-disconnect","waiting");
 Save("remote-driver-monitor-survives",peer.Call("waiting"),Term.I(1));
 await RejectOld("old-handle-before-reconnect");
 link=new(runtime,Path.Combine(input.OutputDirectory,"connection2"));await link.Connect(port,peerNode,managedNode,4);
 DriverParentHarnessAccess423.Require(link.RemoteCreation==creation&&runtime.DistributionPending.ConnectedSnapshot(link.Handle).ConnectionId!=oldConnection);
 await RejectOld("old-handle-after-reconnect");
 var stale=await link.ProbeStale(oldHandle,File.ReadAllBytes(Path.Combine(input.OutputDirectory,"connection1/input-0001.bin")));File.WriteAllText(Path.Combine(input.OutputDirectory,"old-handle-current-input-handler.json"),JsonSerializer.Serialize(stale,json));
 long beforePositive=runtime.DistributionPending.ConnectedSnapshot(link.Handle).AcceptedInputPackets;
 await RemotePair("real-node-visible-after-reconnect","seen_node",[Term.A(oracleNode)],[Term.A(managedNode)]);
 DriverParentHarnessAccess423.Require(runtime.DistributionPending.ConnectedSnapshot(link.Handle).AcceptedInputPackets>beforePositive);
 Save("foreign-reject-after-reconnect",Original("demon",foreignOriginal),await Managed("demon",foreignManaged));
 var heldDecoded=await RemotePair("actual-peer-retained-ref-after-reconnect","stored",[Term.A("held_oracle")],[Term.A("held_managed")]);
 await RemotePair("held-ref-peer-foreign-reject","demon",[heldDecoded.Item1],[heldDecoded.Item2]);
 await Step("queue-one-reconnect-peer-refusal","waiting");
 var heldReturn=await RemotePair("held-ref-wire-after-reconnect","encode",[heldDecoded.Item1],[heldDecoded.Item2],false);
 DriverParentHarnessAccess423.Require(((BitString)heldReturn.Item1).ToArray().AsSpan().SequenceEqual(held.Item1.ToArray())&&((BitString)heldReturn.Item2).ToArray().AsSpan().SequenceEqual(held.Item2.ToArray()));
 Save("held-ref-return-cancels-owner",Original("demon_binary",heldReturn.Item1),await Managed("demon_binary",heldReturn.Item2));await Step("queue-zero-reconnect-return","waiting");
 await Step("repeat-completed-ref-after-reconnect","demon_saved",Term.A("held"));
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
 try{oracle.Stop();}catch(Exception error){errors.Add("oracle stop: "+error);}try{peer.Stop();}catch(Exception error){errors.Add("peer stop: "+error);}
}
bool settled=child?.PhysicalExit.IsCompletedSuccessfully==true&&child.PhysicalCleanup.IsCompletedSuccessfully&&child.Completion.IsCompletedSuccessfully;
File.WriteAllText(Path.Combine(input.OutputDirectory,"comparison.json"),JsonSerializer.Serialize(new{Passed=errors.Count==0&&settled,Depth=depth,Results=results,Failures=errors,PhysicalCleanup=settled,Transport="Authenticated real Erlang distribution v6 and physical TCP disconnect/reconnect; actual runtime input/output queues",WholeModuleClaim=false},json));
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
