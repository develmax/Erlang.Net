using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text.Json;
using Erlang;

var json=new JsonSerializerOptions{WriteIndented=true,PropertyNameCaseInsensitive=true};
var input=JsonSerializer.Deserialize<DriverParentHarnessInput423>(File.ReadAllText(args[0]),json)!;
DriverParentHarnessAccess423.VerifyInputs(input);
Directory.CreateDirectory(input.OutputDirectory);
var results=new List<object>();var failures=new List<string>();
DriverParentHarnessFixture423? fixture=null;ErlDriverHostChild420? child=null;
using var primary=new Peer447(int.Parse(args[1]));using var peer=new Peer447(int.Parse(args[2]));
int depth=int.Parse(args[3]);
var bindings=new List<(ReferenceTerm Expected,ReferenceTerm Actual)>();int sequence=0;
try
{
 fixture=await DriverParentHarnessFixture423.Create(input,"identity447",false);
 var runtime=fixture.Runtime;child=(ErlDriverHostChild420)DriverParentHarnessAccess423.Field(DriverParentHarnessAccess423.Field(runtime,"driverFrontend422"),"child");
 await runtime.InitializeModulesAsync(registry=>
 {
  var image=registry.PrepareBeam(File.ReadAllBytes(Path.Combine(input.OriginalDirectory,"observer-ebin/beam_ddll_identity447.beam")),new BeamDependencySelection(input.DeclaredModules.Concat(new[]{"beam_ddll_identity447"})));
  DriverParentHarnessAccess423.Require(registry.PublishPreparedBeam([image]).Status==BeamPublicationStatus.Published);
  foreach(var e in image.Code.Exports)DriverParentHarnessAccess423.Require(ReferenceEquals(registry.SourceOwner(image.Name,e.Name,e.Arity),image));
 });
 Pid worker=(Pid)await fixture.Actor!.Invoke(async c=>await runtime.Modules.Call(c,"beam_ddll_identity447","new"));
 async Task<Term> Managed(string f,params Term[] a)=>Unwrap(await fixture.Actor.Invoke(async c=>await runtime.Modules.Call(c,"beam_ddll_identity447","request",worker,Term.A(f),Term.List(a))));
 Term Original(string f,params Term[] a)=>primary.Call(f,a);
 void Save(string name,Term expected,Term actual,bool compare=true)
 {
  string stem=Path.Combine(input.OutputDirectory,$"{++sequence:D3}-{name}");
  File.WriteAllBytes(stem+"-original.etf",ExternalTermFormat.Encode(expected));
  File.WriteAllBytes(stem+"-managed.etf",ExternalTermFormat.Encode(actual));
  File.WriteAllText(stem+"-original.json",JsonSerializer.Serialize(DriverParentHarnessTerms423.Typed(expected),json));
  File.WriteAllText(stem+"-managed.json",JsonSerializer.Serialize(DriverParentHarnessTerms423.Typed(actual),json));
  if(!compare)return;
  ValidateOutcome(name,expected,depth);ValidateOutcome(name,actual,depth);
  bool exact=expected.Equals(actual),raw=File.ReadAllBytes(stem+"-original.etf").AsSpan().SequenceEqual(File.ReadAllBytes(stem+"-managed.etf"));
  bool protocol=Equal(expected,actual,bindings);
  results.Add(new{Name=name,Passed=protocol,WholeTypedEqual=exact,WholeRawEqual=raw,OwnReferenceBindings=bindings.Count});
  if(!protocol)failures.Add(name);
 }
 async Task Step(string name,string f,params Term[] a)=>Save(name,Original(f,a),await Managed(f,a));
 async Task<(BitString,BitString)> Wires(string key)
 {
  var x=(BitString)Original("wire",Term.A(key));var y=(BitString)await Managed("wire",Term.A(key));
  Save("wire-"+key,x,y,false);
  bindings.Add(((ReferenceTerm)ExternalTermFormat.DecodeOne(x.ToArray(),new()).Term,(ReferenceTerm)ExternalTermFormat.DecodeOne(y.ToArray(),new()).Term));
  return(x,y);
 }
 Save("peer-setup",peer.Call("setup",Term.String(input.DriverDirectory)),Term.A("ok"));
 Save("peer-identity",peer.Call("start_node",Term.A("ddll447peer@loopback")),Term.Tuple(Term.A("ddll447peer@loopback"),Term.I(4),Term.A("true")));
 peer.Call("depth",Term.I(depth));
 await Step("setup","setup",Term.String(input.DriverDirectory));
 await Step("depth","depth",Term.I(depth));
 Term foreign=peer.Call("new_ref");
 Save("genuine-peer-reference",foreign,foreign,false);
 DriverParentHarnessAccess423.Require(foreign is ReferenceTerm foreignRef&&foreignRef.Node=="ddll447peer@loopback");
 await Step("foreign-reference-full-stack","demon",foreign);
 Save("peer-monitor-still-queued",peer.Call("waiting"),Term.I(1));
 await Step("local-queue","save_ref",Term.A("roundtrip"));
 var round=await Wires("roundtrip");
 Term ox=peer.Call("decode",round.Item1),mx=peer.Call("decode",round.Item2);
 Save("peer-rejects-primary-reference",peer.Call("demon",ox),peer.Call("demon",mx));
 Save("peer-returned-reference",ox,mx);
 var ob=(BitString)peer.Call("encode",ox);var mb=(BitString)peer.Call("encode",mx);
 DriverParentHarnessAccess423.Require(ob.ToArray().AsSpan().SequenceEqual(round.Item1.ToArray())&&mb.ToArray().AsSpan().SequenceEqual(round.Item2.ToArray()));
 await Step("queue-one-foreign-caller-preserves","waiting");
 Save("returned-reference-cancels-owner",Original("demon_binary",ob),await Managed("demon_binary",mb));
 await Step("queue-zero-after-return","waiting");
 await Step("repeat-completed-reference","demon_saved",Term.A("roundtrip"));
 await Step("pre-admission-ref","save_ref",Term.A("inactive"));
 await Wires("inactive");await Step("pre-admission-wire","save_wire",Term.A("inactive"));
 await Step("public-node-admission","start_node",Term.A("ddll447local@loopback"));
 await Step("pre-admission-wire-after-admission","demon_wire",Term.A("inactive"));
 await Step("queue-one-after-admission-reject","waiting");
 await Step("retained-internal-after-admission","demon_saved",Term.A("inactive"));
 await Step("queue-zero-after-internal-admission","waiting");
 await Step("active-ref","save_ref",Term.A("active"));
 await Wires("active");await Step("active-wire","save_wire",Term.A("active"));
 await Step("active-wire-local-roundtrip","demon_wire",Term.A("active"));
 await Step("queue-zero-after-active-roundtrip","waiting");
 await Step("active-ref-two","save_ref",Term.A("restart"));
 await Wires("restart");await Step("active-wire-two","save_wire",Term.A("restart"));
 await Step("kernel-death-node-reset","stop_node");
 await Step("prior-epoch-wire-after-reset","demon_wire",Term.A("restart"));
 await Step("queue-one-after-reset-reject","waiting");
 await Step("retained-internal-after-reset","demon_saved",Term.A("restart"));
 await Step("queue-zero-after-internal-reset","waiting");
 await Step("next-node-admission","start_node",Term.A("ddll447next@loopback"));
 await Step("prior-epoch-wire-after-next-admission","demon_wire",Term.A("restart"));
 await Step("retained-internal-after-next-admission","demon_saved",Term.A("restart"));
 await Step("final-kernel-reset","stop_node");
 await Step("retain-real-prior-external-epoch","retain_wire",Term.A("restart"));
 await Step("same-name-epoch-collision","start_node",Term.A("ddll447local@loopback"));
 await Step("prior-epoch-wire-after-same-name","demon_wire",Term.A("restart"));
 await Step("retained-internal-after-same-name","demon_saved",Term.A("restart"));
 await Step("queue-zero-after-same-name","waiting");
 await Step("same-name-new-ref","save_ref",Term.A("new_epoch"));
 await Wires("new_epoch");await Step("same-name-new-wire","save_wire",Term.A("new_epoch"));
 await Step("same-name-new-roundtrip","demon_wire",Term.A("new_epoch"));
 await Step("queue-zero-after-new-epoch","waiting");
 await Step("last-kernel-reset","stop_node");
 await Step("physical-unload-notification","cleanup");
 Save("peer-kernel-reset",peer.Call("stop_node"),Term.Tuple(Term.A("nonode@nohost"),Term.I(0),Term.A("false")));
 Save("peer-cleanup",peer.Call("cleanup"),Term.Tuple(Term.Tuple(Term.A("ok"),Term.A("pending_driver")),Term.A("true"),Term.A("unloaded")));
 runtime.Send(worker,Term.A("stop"));
}
catch(Exception error){failures.Add(error.ToString());}
finally
{
 if(fixture is not null)
 {
  try{await fixture.DisposeAsync();DriverParentHarnessAccess423.ZeroLeases(fixture.Runtime,fixture.Environment,true);}catch(Exception error){failures.Add("cleanup: "+error);}
 }
 try{primary.Stop();}catch(Exception error){failures.Add("primary stop: "+error);}
 try{peer.Stop();}catch(Exception error){failures.Add("peer stop: "+error);}
}
bool settled=child?.PhysicalExit.IsCompletedSuccessfully==true&&child.PhysicalCleanup.IsCompletedSuccessfully&&child.Completion.IsCompletedSuccessfully;
File.WriteAllText(Path.Combine(input.OutputDirectory,"comparison.json"),JsonSerializer.Serialize(new{Passed=failures.Count==0&&settled,Depth=depth,Results=results,Failures=failures,PhysicalCleanup=settled,Transport="Actual loopback TCP packet4 ETF; not Erlang distribution handshake",FullStackComparison="All frames and fields; explicitly bound own-reference values compared by source protocol, no expected rewriting"},json));
DriverParentHarnessAccess423.VerifyInputs(input);
return failures.Count==0&&settled?0:1;

static Term Unwrap(Term value)
{
 if(value is TupleTerm{Items.Count:2} t&&t.Items[0].Equals(Term.A("ok")))return t.Items[1];
 throw new Exception("Worker failed: "+value);
}
static void ValidateOutcome(string name,Term value,int depth)
{
 if(name=="same-name-epoch-collision")DriverParentHarnessAccess423.Require(value.Equals(Term.Tuple(Term.A("ddll447local@loopback"),Term.I(5),Term.A("true"))));
 if(name.StartsWith("queue-one-"))DriverParentHarnessAccess423.Require(value.Equals(Term.I(1)));
 if(name.StartsWith("queue-zero-"))DriverParentHarnessAccess423.Require(value.Equals(Term.I(0)));
 if(name is "foreign-reference-full-stack" or "peer-rejects-primary-reference" or "pre-admission-wire-after-admission" or "prior-epoch-wire-after-reset" or "prior-epoch-wire-after-next-admission" or "prior-epoch-wire-after-same-name")
 {
  DriverParentHarnessAccess423.Require(value is TupleTerm{Items.Count:4});var failure=(TupleTerm)value;
  DriverParentHarnessAccess423.Require(failure.Items[0].Equals(Term.A("exception"))&&failure.Items[1].Equals(Term.A("error"))&&failure.Items[2].Equals(Term.A("badarg")));
  var frames=Cons.Items(failure.Items[3]).ToArray();DriverParentHarnessAccess423.Require(frames.Length==Math.Clamp(depth,1,3));
  if(frames.Length>0)DriverParentHarnessAccess423.Require(frames[0] is TupleTerm frame&&frame.Items[0].Equals(Term.A("erl_ddll"))&&frame.Items[1].Equals(Term.A("demonitor"))&&Cons.Items(frame.Items[2]).Single() is ReferenceTerm);
 }
 if(name is "returned-reference-cancels-owner" or "repeat-completed-reference" or "retained-internal-after-admission" or "active-wire-local-roundtrip" or "retained-internal-after-reset" or "retained-internal-after-next-admission" or "retained-internal-after-same-name" or "same-name-new-roundtrip")DriverParentHarnessAccess423.Require(value.Equals(Term.Tuple(Term.A("value"),Term.A("true"))));
 if(name is "kernel-death-node-reset" or "final-kernel-reset" or "last-kernel-reset")DriverParentHarnessAccess423.Require(value.Equals(Term.Tuple(Term.A("nonode@nohost"),Term.I(0),Term.A("false"))));
}
static bool Equal(Term expected,Term actual,List<(ReferenceTerm Expected,ReferenceTerm Actual)> bindings)
{
 if(expected.Equals(actual))return true;
 if(expected is ReferenceTerm e&&actual is ReferenceTerm a)return bindings.Any(b=>b.Expected.Equals(e)&&b.Actual.Equals(a));
 if(expected is TupleTerm et&&actual is TupleTerm at)return et.Items.Count==at.Items.Count&&et.Items.Zip(at.Items).All(p=>Equal(p.First,p.Second,bindings));
 if(expected is Cons ec&&actual is Cons ac)return Equal(ec.Head,ac.Head,bindings)&&Equal(ec.Tail,ac.Tail,bindings);
 return false;
}
