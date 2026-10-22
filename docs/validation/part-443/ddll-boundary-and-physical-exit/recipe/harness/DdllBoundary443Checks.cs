using System.Text.Json;
namespace Erlang;
internal static class DdllBoundary443Checks
{
 internal static async Task<object> Run(DriverParentHarnessInput423 input)
 {
  string outPath=Path.Combine(input.OutputDirectory,DdllBoundary443Names.Phase);Directory.CreateDirectory(outPath);
  DriverParentHarnessFixture423? fixture=null;var failures=new List<string>();
  var actual=new Dictionary<string,(Term Value,byte[] Bytes)>();ProcessRuntime? runtime=null;RuntimeOsEnvironment? environment=null;ErlDriverHostChild420? child=null;
  try
  {
   fixture=await DriverParentHarnessFixture423.Create(input,DdllBoundary443Names.Phase,false);runtime=fixture.Runtime;environment=fixture.Environment;
   child=(ErlDriverHostChild420)DriverParentHarnessAccess423.Field(DriverParentHarnessAccess423.Field(runtime,"driverFrontend422"),"child");
   await runtime.InitializeModulesAsync(registry=>
   {
    var declared=new BeamDependencySelection(input.DeclaredModules.Concat(new[]{DdllBoundary443Names.Module,"file"}).Distinct());
    var image=registry.PrepareBeam(File.ReadAllBytes(Path.Combine(input.OriginalDirectory,"observer-ebin",DdllBoundary443Names.Beam)),declared);
    DriverParentHarnessAccess423.Require(registry.PublishPreparedBeam([image]).Status==BeamPublicationStatus.Published);
    foreach(var export in image.Code.Exports)DriverParentHarnessAccess423.Require(ReferenceEquals(registry.SourceOwner(image.Name,export.Name,export.Arity),image));
   });
   foreach(var route in new[]{(DdllBoundary443Names.Run,DdllBoundary443Names.Values),(DdllBoundary443Names.RunStacks,DdllBoundary443Names.Stacks)})
   {
    try
    {
     var row=await fixture.Actor!.Invoke(async context=>
     {
      Term value=await runtime.Modules.Call(context,DdllBoundary443Names.Module,route.Item1,Term.String(Path.Combine(input.OriginalDirectory,"drivers")));
      var binary=(BitString)await runtime.Modules.Call(context,DdllBoundary443Names.Runtime,DdllBoundary443Names.Encode,value,Term.List(Term.Tuple(Term.A("minor_version"),Term.I(2))));
      return (Value:value,Bytes:binary.ToArray());
     });
     actual.Add(route.Item2,row);File.WriteAllBytes(Path.Combine(outPath,"actual-"+route.Item2),row.Bytes);
     File.WriteAllText(Path.Combine(outPath,"actual-"+route.Item2+".json"),JsonSerializer.Serialize(DriverParentHarnessTerms423.Typed(row.Value),DriverParentHarnessJson423.Options));
    }
    catch(Exception error) {failures.Add(route.Item2+": "+error);}
   }
  }
  catch(Exception error) {failures.Add("Preparation: "+error);}
  finally
  {
   if(fixture is not null)try {await fixture.DisposeAsync();}catch(Exception error){failures.Add("Physical cleanup: "+error);}
   if(runtime is not null&&environment is not null)try {DriverParentHarnessAccess423.ZeroLeases(runtime,environment,true);}catch(Exception error){failures.Add("Real owner lease cleanup: "+error);}
  }
  bool physicalExit=child?.PhysicalExit.IsCompletedSuccessfully==true,physicalCleanup=child?.PhysicalCleanup.IsCompletedSuccessfully==true,childCompletion=child?.Completion.IsCompletedSuccessfully==true;
  var rows=new List<object>();bool passed=failures.Count==0&&physicalExit&&physicalCleanup&&childCompletion;
  foreach(string file in new[]{DdllBoundary443Names.Values,DdllBoundary443Names.Stacks})
  {
   byte[] raw=File.ReadAllBytes(Path.Combine(input.OriginalDirectory,file));Term expected=ExternalTermFormat.DecodeOne(raw,new()).Term;
   bool present=actual.TryGetValue(file,out var found),typed=present&&expected.Equals(found.Value),bytes=present&&raw.AsSpan().SequenceEqual(found.Bytes);
   File.WriteAllBytes(Path.Combine(outPath,"expected-"+file),raw);
   File.WriteAllText(Path.Combine(outPath,"expected-"+file+".json"),JsonSerializer.Serialize(DriverParentHarnessTerms423.Typed(expected),DriverParentHarnessJson423.Options));
   rows.Add(new{File=file,WholeTypedEqual=typed,WholeRawEqual=bytes});passed &=typed&&bytes;
  }
  var report=new{Passed=passed,ExpectedRows=DdllBoundary443Names.Rows,Files=rows,Failures=failures,ActualNativeDrivers=true,OriginalRewritten=false,Original228Rerun=false,NativeInitBoundaryObservation=true,ActualNativeStartCallback=true,LinkedCleanupInStop=true,PlainDynamicInitFailureSeparate=true,PhysicalExit=physicalExit,PhysicalCleanup=physicalCleanup,ChildCompletion=childCompletion};
  File.WriteAllText(Path.Combine(outPath,DdllBoundary443Names.Receipt),JsonSerializer.Serialize(report,DriverParentHarnessJson423.Options));
  DriverParentHarnessAccess423.Require(passed);return report;
 }
}
