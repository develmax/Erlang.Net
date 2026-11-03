
using Erlang;
using System.Security.Cryptography;
using System.Text.Json;
string manifest=args[0], fixture=args[1], observer=args[2], ddll=args[3], output=args[4];
string hash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(manifest)));
await using var runtime=await ProcessRuntime.CreateWithOptionsAsync(new RuntimeLibrarySelection(Array.Empty<RuntimeModuleSelection>()),new RuntimeLibraryStartupOptions(RuntimeNativePrimitives.None,false,null,new RuntimeDriverFrontendOptions(manifest,hash,asyncThreads:1)),default);
await runtime.InitializeModulesAsync(registry=>RuntimeNativePrimitiveProviders.RegisterCatalog(runtime,registry));
await Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(runtime,(registry,scope)=>
{
 var dependencies=new BeamDependencySelection(new[]{"erlang","erts_internal","erl_ddll","lists","maps","binary","async_force_unload1320","file","io_lib","string","init","io"});
 var a=scope.Prepare(()=>registry.PrepareBeam(File.ReadAllBytes(ddll),dependencies));
 var b=scope.Prepare(()=>registry.PrepareBeam(File.ReadAllBytes(observer),dependencies));
 return ValueTask.FromResult<Action<ModuleRegistry>>(r=>{if(r.PublishPreparedBeam(a,b).Status!=BeamPublicationStatus.Published)throw new InvalidOperationException("Real observer publication failed");});
});
var done=new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
var actor=runtime.Spawn(async context=>
{
 try
 {
  Term loaded=await runtime.Modules.Call(context,"erl_ddll","load_driver",Term.String(fixture),Term.String("async_msacc1127_drv"));
  if(!loaded.Equals(Term.A("ok")))throw new InvalidOperationException("Genuine fixture load failed");
  Term outcome;
  Exception? primary=null,unloadFailure=null;
  try
  {
   try {outcome=Term.Tuple(Term.A("return"),await runtime.Modules.Call(context,"async_force_unload1320","run"));}
   catch(ErlangException error){outcome=Term.Tuple(Term.A("exception"),Term.A(error.ExceptionClass),error.Reason,error.StackTraceTerm);}
   File.WriteAllBytes(output,ExternalTermFormat.Encode(outcome));
  }
  catch(Exception error){primary=error;throw;}
  finally
  {
   try {Term u=await runtime.Modules.Call(context,"erl_ddll","try_unload",Term.String("async_msacc1127_drv"),Nil.Value);if(u is not TupleTerm t || t.Items.Count!=2 || !t.Items[0].Equals(Term.A("error")) || !t.Items[1].Equals(Term.A("not_loaded")))throw new InvalidOperationException("Actual force observer failed to finish driver unload");}
   catch(Exception error){unloadFailure=error;}
   if(unloadFailure is not null)throw primary is null?unloadFailure:new AggregateException(primary,unloadFailure);
  }
  done.TrySetResult(outcome);
 }
 catch(Exception error){done.TrySetException(error);}
 return Nil.Value;
});
Term result=await done.Task.WaitAsync(TimeSpan.FromSeconds(30));
await actor.Completion.WaitAsync(TimeSpan.FromSeconds(15));
File.WriteAllBytes(output,ExternalTermFormat.Encode(result));
Console.WriteLine(JsonSerializer.Serialize(new {Outcome=result.ToString(),Etf=Convert.ToBase64String(ExternalTermFormat.Encode(result))}));
