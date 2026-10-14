using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Erlang;
using Erlang.Differential;
using System.Text.Json;
using (var pe = new PEReader(File.OpenRead(args[0])))
{
    var metadata = pe.GetMetadataReader();
    Console.Error.WriteLine(metadata.GetGuid(metadata.GetModuleDefinition().Mvid));
}
Console.Error.WriteLine(typeof(TermOrder).Assembly.ManifestModule.ModuleVersionId);
var rows = new List<object>();
int failures = 0;
foreach (var fixture in IdentifierOrderCases.Modules)
{
    using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
    Term result = await CompiledModuleExecution.Run(artifact, fixture.CallerContext);
    bool pass = TermOrder.Compare(result, fixture.Expected, true) == 0;
    if (!pass) failures++;
    rows.Add(new { fixture.Name, Pass = pass, Result = result.ToString(), Expected = fixture.Expected.ToString() });
}
Console.WriteLine(JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
return failures == 0 ? 0 : 1;
