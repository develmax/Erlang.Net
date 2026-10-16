using Erlang;
using Erlang.Compiler;
using Erlang.Differential;
using System.Text.Json;
using System.Security.Cryptography;

Console.WriteLine(JsonSerializer.Serialize(new {TermsAssemblyPath=typeof(Term).Assembly.Location,TermsAssemblySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(Term).Assembly.Location)))}));
var paths=Directory.GetFiles(args[0],"*.erl").Order().ToArray();
foreach(var path in paths) new Parser(File.ReadAllText(path)).ParseModule();
Console.WriteLine("PARSED "+paths.Length);
foreach(var path in paths)
{
    using var artifact=GeneratedModuleCompiler.Compile(File.ReadAllText(path));
    Term result=await CompiledModuleExecution.Run(artifact);
    bool passed=result.Equals(Term.Tuple(Term.A("ok"),Term.A("done")));
    Console.WriteLine(JsonSerializer.Serialize(new {Name=Path.GetFileName(path),Passed=passed,Result=result.ToString()}));
    if(!passed) return 1;
}
return 0;
