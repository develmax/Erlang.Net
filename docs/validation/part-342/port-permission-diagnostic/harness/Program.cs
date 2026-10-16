using Erlang; using Erlang.Differential;
var results = new List<object>();
foreach(var fixture in PortNativeOptionCases.Modules) {
 using var artifact=GeneratedModuleCompiler.Compile(fixture.Source);
 var actual=await CompiledModuleExecution.Run(artifact,fixture.CallerContext).WaitAsync(TimeSpan.FromSeconds(15));
 bool passed=actual.Equals(fixture.Expected);
 results.Add(new{fixture.Name,Passed=passed,Expected=fixture.Expected.ToString(),Actual=actual.ToString()});
 Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(results[^1]));
 if(!passed) throw new InvalidOperationException(fixture.Name);
}
File.WriteAllText(args[0],System.Text.Json.JsonSerializer.Serialize(results,new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
