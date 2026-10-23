using System.Text.Json;
namespace Erlang;
internal static class DdllModes444Checks
{
 internal static async Task<object> Run(DriverParentHarnessInput423 input)
 {
  string outPath=Path.Combine(input.OutputDirectory,DdllModes444Names.Phase);Directory.CreateDirectory(outPath);
  DriverParentHarnessFixture423? fixture=null;var failures=new List<string>();
  var actual=new Dictionary<string,(Term Value,byte[] Bytes)>();ProcessRuntime? runtime=null;RuntimeOsEnvironment? environment=null;ErlDriverHostChild420? child=null;
  try
  {
   fixture=await DriverParentHarnessFixture423.Create(input,DdllModes444Names.Phase,false);runtime=fixture.Runtime;environment=fixture.Environment;
   child=(ErlDriverHostChild420)DriverParentHarnessAccess423.Field(DriverParentHarnessAccess423.Field(runtime,"driverFrontend422"),"child");
   await runtime.InitializeModulesAsync(registry=>
   {
    var declared=new BeamDependencySelection(input.DeclaredModules.Concat(new[]{DdllModes444Names.Module,"file"}).Distinct());
    var image=registry.PrepareBeam(File.ReadAllBytes(Path.Combine(input.OriginalDirectory,"observer-ebin",DdllModes444Names.Beam)),declared);
    DriverParentHarnessAccess423.Require(registry.PublishPreparedBeam([image]).Status==BeamPublicationStatus.Published);
    foreach(var export in image.Code.Exports)DriverParentHarnessAccess423.Require(ReferenceEquals(registry.SourceOwner(image.Name,export.Name,export.Arity),image));
   });
   foreach(var route in new[]{(DdllModes444Names.Run,DdllModes444Names.Values),(DdllModes444Names.RunStacks,DdllModes444Names.Stacks)})
   {
    try
    {
     var row=await fixture.Actor!.Invoke(async context=>
     {
      Term value=await runtime.Modules.Call(context,DdllModes444Names.Module,route.Item1,Term.String(Path.Combine(input.OriginalDirectory,"drivers")));
      DriverParentHarnessAccess423.Require(value is TupleTerm { Items.Count:2 });
      var pair=(TupleTerm)value;Term own=pair.Items[1];value=pair.Items[0];
      ValidateOwn(own,route.Item2==DdllModes444Names.Stacks);
      var ownBinary=(BitString)await runtime.Modules.Call(context,DdllModes444Names.Runtime,DdllModes444Names.Encode,own,Term.List(Term.Tuple(Term.A("minor_version"),Term.I(2))));
      File.WriteAllBytes(Path.Combine(outPath,"actual-own-"+route.Item2),ownBinary.ToArray());
      File.WriteAllText(Path.Combine(outPath,"actual-own-"+route.Item2+".json"),JsonSerializer.Serialize(DriverParentHarnessTerms423.Typed(own),DriverParentHarnessJson423.Options));
      var binary=(BitString)await runtime.Modules.Call(context,DdllModes444Names.Runtime,DdllModes444Names.Encode,value,Term.List(Term.Tuple(Term.A("minor_version"),Term.I(2))));
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
  foreach(string file in new[]{DdllModes444Names.Values,DdllModes444Names.Stacks})
  {
   byte[] ownExpected=File.ReadAllBytes(Path.Combine(input.OriginalDirectory,"own-"+file));
   File.WriteAllBytes(Path.Combine(outPath,"expected-own-"+file),ownExpected);
   passed &=File.Exists(Path.Combine(outPath,"actual-own-"+file));
   byte[] raw=File.ReadAllBytes(Path.Combine(input.OriginalDirectory,file));Term expected=ExternalTermFormat.DecodeOne(raw,new()).Term;
   bool present=actual.TryGetValue(file,out var found),typed=present&&expected.Equals(found.Value),bytes=present&&raw.AsSpan().SequenceEqual(found.Bytes);
   File.WriteAllBytes(Path.Combine(outPath,"expected-"+file),raw);
   File.WriteAllText(Path.Combine(outPath,"expected-"+file+".json"),JsonSerializer.Serialize(DriverParentHarnessTerms423.Typed(expected),DriverParentHarnessJson423.Options));
   rows.Add(new{File=file,WholeTypedEqual=typed,WholeRawEqual=bytes});passed &=typed&&bytes;
  }
  var report=new{Passed=passed,ExpectedRows=DdllModes444Names.Rows,Files=rows,Failures=failures,ActualNativeDrivers=true,OriginalRewritten=false,Original228Rerun=false,WholeModesSourceObserverComparison=true,OpaqueOwnFilesRetained=true,OpaqueIdentityBytesCompared=false,ActualNativeStartCallback=true,ReusedExactEchoDriver=true,PhysicalExit=physicalExit,PhysicalCleanup=physicalCleanup,ChildCompletion=childCompletion};
  File.WriteAllText(Path.Combine(outPath,DdllModes444Names.Receipt),JsonSerializer.Serialize(report,DriverParentHarnessJson423.Options));
  DriverParentHarnessAccess423.Require(passed);return report;
 }
 private static void ValidateOwn(Term value,bool stacks)
 {
  if(!stacks){ValidateOwnRows(value);return;}
  Term[] profiles=Cons.Items(value).ToArray();DriverParentHarnessAccess423.Require(profiles.Length==5);
  Term[] depths=[Term.A("default"),Term.I(0),Term.I(1),Term.I(2),Term.I(8)];
  for(int i=0;i<profiles.Length;i++)
  {
   DriverParentHarnessAccess423.Require(profiles[i] is TupleTerm { Items.Count:3 });
   var profile=(TupleTerm)profiles[i];DriverParentHarnessAccess423.Require(profile.Items[0].Equals(depths[i])&&profile.Items[1] is Integer);
   ValidateOwnRows(profile.Items[2]);
  }
 }
 private static void ValidateOwnRows(Term value)
 {
  Term[] rows=Cons.Items(value).ToArray();DriverParentHarnessAccess423.Require(rows.Length==DdllModes444Names.Cases);
  for(int i=0;i<rows.Length;i++)
  {
   DriverParentHarnessAccess423.Require(rows[i] is TupleTerm { Items.Count:2 });
   var row=(TupleTerm)rows[i];DriverParentHarnessAccess423.Require(row.Items[0] is Atom name&&name.Name==DdllModes444Names.CaseNames[i]);
   Term[] entries=Cons.Items(row.Items[1]).ToArray();DriverParentHarnessAccess423.Require(entries.Length>0);
   DriverParentHarnessAccess423.Require(entries[0] is TupleTerm { Items.Count:2 } first&&first.Items[0].Equals(Term.A("observer"))&&first.Items[1] is Pid);
  }
 }
}
