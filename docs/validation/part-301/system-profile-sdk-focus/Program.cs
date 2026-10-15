using Erlang.Differential;
var cases=SystemProfileCases.Modules;
if(cases.Count!=23)throw new InvalidOperationException("Case count changed");
foreach(var fixture in cases.Concat(Enumerable.Repeat(cases.Single(x=>x.Name=="erlang-system-profile-dead-receiver"),40))) {
 using var artifact=GeneratedModuleCompiler.Compile(fixture.Source);
 var result=await CompiledModuleExecution.Run(artifact,fixture.CallerContext).WaitAsync(TimeSpan.FromSeconds(15));
 if(!result.Equals(fixture.Expected))throw new InvalidOperationException(fixture.Name+": "+result);
 Console.WriteLine("PASS "+fixture.Name);
}
