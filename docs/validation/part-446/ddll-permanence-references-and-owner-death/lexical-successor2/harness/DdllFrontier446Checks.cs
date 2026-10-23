using System.Text.Json;
namespace Erlang;
internal static class DdllFrontier446Checks
{
 internal static async Task<object> Run(DriverParentHarnessInput423 input)
 {
  string folder=Path.Combine(input.OutputDirectory,DdllFrontier446Names.Phase);Directory.CreateDirectory(folder);
  var groups=new List<object>();bool passed=true;
  foreach(var group in DdllFrontier446Names.Groups)
  {
   object report=await RunGroup(input,folder,group.Name,group.Profile);
   groups.Add(report);passed &=(bool)report.GetType().GetProperty("Passed")!.GetValue(report)!;
  }
  var result=new{Passed=passed,WholeMainRows=DdllFrontier446Names.Rows,Groups=groups,FreshActualRuntimeAndChildPerGroup=true,RawOwnRetained=true,OpaqueOwnEqualityClaim=false,OriginalRewritten=false};
  File.WriteAllText(Path.Combine(folder,DdllFrontier446Names.Receipt),JsonSerializer.Serialize(result,DriverParentHarnessJson423.Options));
  DriverParentHarnessAccess423.Require(passed);return result;
 }
 private static async Task<object> RunGroup(DriverParentHarnessInput423 input,string folder,string name,Term profile)
 {
  string output=Path.Combine(folder,name);Directory.CreateDirectory(output);
  DriverParentHarnessFixture423? fixture=null;ProcessRuntime? runtime=null;RuntimeOsEnvironment? environment=null;ErlDriverHostChild420? child=null;
  Term? actual=null;byte[]? bytes=null;bool ownValid=false;var failures=new List<string>();
  try
  {
   fixture=await DriverParentHarnessFixture423.Create(input,DdllFrontier446Names.Phase+"-"+name,false);runtime=fixture.Runtime;environment=fixture.Environment;
   child=(ErlDriverHostChild420)DriverParentHarnessAccess423.Field(DriverParentHarnessAccess423.Field(runtime,"driverFrontend422"),"child");
   await runtime.InitializeModulesAsync(registry=>
   {
    var declared=new BeamDependencySelection(input.DeclaredModules.Concat(new[]{DdllFrontier446Names.Module,"file"}).Distinct());
    var image=registry.PrepareBeam(File.ReadAllBytes(Path.Combine(input.OriginalDirectory,"observer-ebin",DdllFrontier446Names.Beam)),declared);
    DriverParentHarnessAccess423.Require(registry.PublishPreparedBeam([image]).Status==BeamPublicationStatus.Published);
    foreach(var export in image.Code.Exports)DriverParentHarnessAccess423.Require(ReferenceEquals(registry.SourceOwner(image.Name,export.Name,export.Arity),image));
   });
   var captured=await fixture.Actor!.Invoke(async context=>
   {
    Term value=await runtime.Modules.Call(context,DdllFrontier446Names.Module,DdllFrontier446Names.Capture,Term.String(Path.Combine(input.OriginalDirectory,"drivers")),profile);
    DriverParentHarnessAccess423.Require(value is TupleTerm{Items.Count:2});var pair=(TupleTerm)value;
    var main=(BitString)await runtime.Modules.Call(context,"erlang","term_to_binary",pair.Items[0],Term.List(Term.Tuple(Term.A("minor_version"),Term.I(2))));
    var own=(BitString)await runtime.Modules.Call(context,"erlang","term_to_binary",pair.Items[1],Term.List(Term.Tuple(Term.A("minor_version"),Term.I(2))));
    File.WriteAllBytes(Path.Combine(output,"actual-"+DdllFrontier446Names.Own),own.ToArray());
    File.WriteAllText(Path.Combine(output,"actual-own.json"),JsonSerializer.Serialize(DriverParentHarnessTerms423.Typed(pair.Items[1]),DriverParentHarnessJson423.Options));
    actual=pair.Items[0];bytes=main.ToArray();
    File.WriteAllBytes(Path.Combine(output,"actual-"+DdllFrontier446Names.Main),bytes);
    File.WriteAllText(Path.Combine(output,"actual-main.json"),JsonSerializer.Serialize(DriverParentHarnessTerms423.Typed(actual),DriverParentHarnessJson423.Options));
    // Durable complete actual files precede every positive row-shape assertion.
    Validate(pair.Items[1],profile,true);ownValid=true;Validate(pair.Items[0],profile,false);
    return(Value:pair.Items[0],Bytes:main.ToArray());
   });
   actual=captured.Value;bytes=captured.Bytes;ownValid=true;
   File.WriteAllBytes(Path.Combine(output,"actual-"+DdllFrontier446Names.Main),bytes);
   File.WriteAllText(Path.Combine(output,"actual-main.json"),JsonSerializer.Serialize(DriverParentHarnessTerms423.Typed(actual),DriverParentHarnessJson423.Options));
  }
  catch(Exception error){failures.Add("Capture: "+error);}
  finally
  {
   if(fixture is not null)try{await fixture.DisposeAsync();}catch(Exception error){failures.Add("Physical cleanup: "+error);}
   if(runtime is not null&&environment is not null)try{DriverParentHarnessAccess423.ZeroLeases(runtime,environment,true);}catch(Exception error){failures.Add("Actual leases: "+error);}
  }
  bool physicalExit=child?.PhysicalExit.IsCompletedSuccessfully==true,physicalCleanup=child?.PhysicalCleanup.IsCompletedSuccessfully==true,completion=child?.Completion.IsCompletedSuccessfully==true;
  bool typed=false,raw=false;
  try
  {
   byte[] expectedRaw=File.ReadAllBytes(Path.Combine(input.OriginalDirectory,name,DdllFrontier446Names.Main));Term expected=ExternalTermFormat.DecodeOne(expectedRaw,new()).Term;Validate(expected,profile,false);
   File.WriteAllBytes(Path.Combine(output,"expected-"+DdllFrontier446Names.Main),expectedRaw);
   File.WriteAllText(Path.Combine(output,"expected-main.json"),JsonSerializer.Serialize(DriverParentHarnessTerms423.Typed(expected),DriverParentHarnessJson423.Options));
   byte[] expectedOwn=File.ReadAllBytes(Path.Combine(input.OriginalDirectory,name,DdllFrontier446Names.Own));
   File.WriteAllBytes(Path.Combine(output,"expected-"+DdllFrontier446Names.Own),expectedOwn);
   typed=actual is not null&&expected.Equals(actual);raw=bytes is not null&&expectedRaw.AsSpan().SequenceEqual(bytes);
  }
  catch(Exception error){failures.Add("Whole comparison: "+error);}
  bool passed=failures.Count==0&&typed&&raw&&ownValid&&physicalExit&&physicalCleanup&&completion;
  var report=new{Passed=passed,Group=name,WholeTypedEqual=typed,WholeRawEqual=raw,OwnProvenanceValid=ownValid,PhysicalExit=physicalExit,PhysicalCleanup=physicalCleanup,ChildCompletion=completion,Failures=failures};
  File.WriteAllText(Path.Combine(output,DdllFrontier446Names.Receipt),JsonSerializer.Serialize(report,DriverParentHarnessJson423.Options));return report;
 }
 private static void Validate(Term value,Term requested,bool own)
 {
  DriverParentHarnessAccess423.Require(value is TupleTerm{Items.Count:3});var header=(TupleTerm)value;
  DriverParentHarnessAccess423.Require(header.Items[0].Equals(requested)&&header.Items[1] is Integer);
  Term[] rows=Cons.Items(header.Items[2]).ToArray();DriverParentHarnessAccess423.Require(rows.Length==DdllFrontier446Names.Cases);
  for(int i=0;i<rows.Length;i++)
  {
   DriverParentHarnessAccess423.Require(rows[i] is TupleTerm{Items.Count:2});var row=(TupleTerm)rows[i];
   DriverParentHarnessAccess423.Require(row.Items[0] is Atom key&&key.Name==DdllFrontier446Names.CaseNames[i]);
   if(own)
   {
    Term[] events=Cons.Items(row.Items[1]).ToArray();DriverParentHarnessAccess423.Require(events.Length>0);
    DriverParentHarnessAccess423.Require(events[0] is TupleTerm{Items.Count:2} first&&first.Items[0].Equals(Term.A("observer"))&&first.Items[1] is Pid);
   }
   else DriverParentHarnessAccess423.Require(row.Items[1] is TupleTerm{Items.Count:2} result&&result.Items[0].Equals(Term.A("value")));
  }
 }
}
