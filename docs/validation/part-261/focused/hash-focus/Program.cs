using Erlang;
using Erlang.Compiler;
using Erlang.Differential;
using System.Reflection;
using System.Runtime.Loader;
using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
var method=typeof(ProcessRuntime).Assembly.GetType("Erlang.ErlangNativeHash",true)!.GetMethod("Portable2",BindingFlags.Static|BindingFlags.Public)!;
var current=method.CreateDelegate<Func<Term,uint>>();
string oldDir=Path.GetFullPath(args[1]);
var oldContext=new AssemblyLoadContext("PublishedOldRuntime",isCollectible:true);oldContext.Resolving+=(context,name)=>File.Exists(Path.Combine(oldDir,name.Name+".dll"))?context.LoadFromAssemblyPath(Path.Combine(oldDir,name.Name+".dll")):null;
var oldRuntime=oldContext.LoadFromAssemblyPath(Path.Combine(oldDir,"Erlang.Runtime.dll"));var oldTerms=oldContext.LoadFromAssemblyPath(Path.Combine(oldDir,"Erlang.Terms.dll"));var oldTerm=oldTerms.GetType("Erlang.Term",true)!;var oldHash=oldRuntime.GetType("Erlang.ErlangNativeHash",true)!.GetMethod("Portable2")!;
var oldSerialization=oldContext.LoadFromAssemblyPath(Path.Combine(oldDir,"Erlang.Serialization.dll"));var decode=oldSerialization.GetType("Erlang.ExternalTermFormat",true)!.GetMethod("Decode",new[]{typeof(ReadOnlySpan<byte>),typeof(int)})!.CreateDelegate<DecodeOld>();
uint Old(Term term)=>(uint)oldHash.Invoke(null,new[]{decode(ExternalTermFormat.Encode(term),16*1024*1024)})!;
int[] sizes=[0,1,2,3,4,7,8,11,12,13,15,16,23,24,25,4096];
var rows=new List<object>();
foreach(int size in sizes) foreach(int tail in Enumerable.Range(0,8)) foreach(bool zero in new[]{true,false}) {
 byte[] bytes=new byte[size+(tail==0?0:1)]; if(!zero) for(int i=0;i<bytes.Length;i++)bytes[i]=(byte)(i*37+181);
 var binary=new BitString(bytes,size*8+tail);
 foreach(Term term in new Term[]{binary,Term.Tuple(Term.A("seed"),binary),Term.List(Term.I(17),binary),new MapTerm([new KeyValuePair<Term,Term>(Term.A("key"),binary)])}) {
 uint old=Old(term), now=current(term); if(old!=now)throw new Exception($"Hash mismatch {size}/{tail}/{zero}/{term.GetType()}");
 rows.Add(new{size,tail,zero,Kind=term.GetType().Name,Hash=now});
 }
}
var large=new BitString(new byte[4*1024*1024]);if(current(large)!=Old(large))throw new Exception("4MiB hash changed");
var oldLarge=decode(ExternalTermFormat.Encode(large),16*1024*1024);var oldCall=new System.Reflection.Emit.DynamicMethod("OldHashDirect",typeof(uint),[typeof(object)]);var il=oldCall.GetILGenerator();il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);il.Emit(System.Reflection.Emit.OpCodes.Castclass,oldTerm);il.Emit(System.Reflection.Emit.OpCodes.Call,oldHash);il.Emit(System.Reflection.Emit.OpCodes.Ret);var oldDirect=oldCall.CreateDelegate<Func<object,uint>>();
var samples=new List<object>();foreach(var entry in new[]{("old",(Func<Term,uint>)(_=>oldDirect(oldLarge))),("new",current)}){
 entry.Item2(large);for(int i=0;i<16;i++){int thread=Environment.CurrentManagedThreadId;long before=GC.GetAllocatedBytesForCurrentThread();uint value=entry.Item2(large);long allocation=GC.GetAllocatedBytesForCurrentThread()-before;if(thread!=Environment.CurrentManagedThreadId)throw new Exception("Thread changed");samples.Add(new{Implementation=entry.Item1,Sample=i,Bytes=allocation,Hash=value,Thread=thread});}
}
int vectors=0;foreach(var fixture in ErlangHashCases.Modules){using var generated=GeneratedModuleCompiler.Compile(fixture.Source);Term result=await CompiledModuleExecution.Run(generated,fixture.CallerContext);if(!result.Equals(fixture.Expected))throw new Exception("Independent vector "+fixture.Name+" "+result);vectors++;}
int interpreted=0;foreach(var fixture in ErlangHashCases.Modules){await using var runtime=new ProcessRuntime();var definition=new Parser(fixture.Source).ParseModule();definition.Register(runtime.Modules);Term? result=null;Exception? failure=null;var actor=runtime.Spawn(async context=>{try{result=Term.Tuple(Term.A("ok"),await runtime.Modules.Call(context,definition.Name,"run"));}catch(ErlangException ex){result=Term.Tuple(Term.A("exception"),Term.A(ex.ExceptionClass),ex.Reason);}catch(Exception ex){failure=ex;}return Nil.Value;});await actor.Completion;if(failure is not null)throw failure;if(result is null||!result.Equals(fixture.Expected))throw new Exception("Interpreted vector "+fixture.Name+" "+result);interpreted++;}
var checks=Assembly.Load("Erlang.Tests").GetType("Erlang.Tests.ErlangHashHostChecks",true)!;await (Task)checks.GetMethod("Resources",BindingFlags.Public|BindingFlags.Static)!.Invoke(null,null)!;
var assembly=typeof(ProcessRuntime).Assembly;var report=new{Status="Passed",SyntheticComparisons=rows.Count,IndependentExistingHashCases=vectors,InterpretedIndependentHashCases=interpreted,ExistingAllocationBudget=4096,HostResourcesPassed=true,NativeExecuted=false,OldRuntimePath=oldRuntime.Location,OldRuntimeSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(oldRuntime.Location))),OldMvid=oldRuntime.ManifestModule.ModuleVersionId,RuntimePath=assembly.Location,RuntimeSha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(assembly.Location))),assembly.ManifestModule.ModuleVersionId,Rows=rows,Samples=samples};await File.WriteAllTextAsync(args[0],JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine($"PASS {rows.Count} old/new comparisons; {vectors} independent hashcases; unchanged HOST budget/deep traversal; 32 allocation samples");

delegate object DecodeOld(ReadOnlySpan<byte> bytes,int maxBytes);
