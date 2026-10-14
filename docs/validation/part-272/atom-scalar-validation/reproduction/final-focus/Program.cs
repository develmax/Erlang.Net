using Erlang;
using Erlang.Compiler;
using Erlang.Differential;
using Erlang.Tests;
using System.Text.Json;
await AtomNameValidationChecks.Run();
var rows = new List<object>();
int failures = 0;
foreach (var fixture in AtomScalarValidationCases.Modules.Concat(IdentifierOrderCases.Modules))
{
    using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
    Term result = await CompiledModuleExecution.Run(artifact, fixture.CallerContext);
    bool pass = result.Equals(fixture.Expected);
    if (!pass) failures++;
    rows.Add(new { fixture.Name, Pass = pass, Result = result.ToString() });
}
var diagnoses = new List<object>();
foreach (var fixture in AtomNameDiagnosticCases.All)
{
    try
    {
        using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
        throw new Exception("Lexer accepted overlong identifier");
    }
    catch (CompileException exception)
    {
        bool pass = exception.Code == AtomNameDiagnosticExpectations.Code && exception.Message == AtomNameDiagnosticExpectations.Message(fixture) && exception.Offset == AtomNameDiagnosticExpectations.Offset(fixture);
        if (!pass) failures++;
        diagnoses.Add(new { fixture.Name, Pass = pass, exception.Code, exception.Message, exception.Offset });
    }
}
Console.WriteLine(JsonSerializer.Serialize(new { HostGroupPassed = true, AssemblyMvid = typeof(Atom).Assembly.ManifestModule.ModuleVersionId, Generated = rows, Diagnoses = diagnoses, Failed = failures, NativeExecuted = false }, new JsonSerializerOptions { WriteIndented = true }));
return failures == 0 ? 0 : 1;
