using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Differential;

bool passed = false;
string? actual = null, expected = null, error = null, source = null, generated = null;
try
{
    var fixture = CompiledModuleCases.All.Single(value => value.Name == args[0]);
    source = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(fixture.Source)));
    using var artifact = GeneratedModuleCompiler.Compile(fixture.Source);
    generated = artifact.CSharpSha256;
    var result = await CompiledModuleExecution.Run(artifact, fixture.CallerContext).WaitAsync(TimeSpan.FromSeconds(30));
    actual = result.ToString();
    expected = fixture.Expected.ToString();
    passed = result.Equals(fixture.Expected);
}
catch (Exception exception)
{
    error = exception.ToString();
}
File.WriteAllText(args[1], JsonSerializer.Serialize(new
{
    Name = args[0], Passed = passed, Actual = actual, Expected = expected, Error = error,
    SourceSha256 = source, GeneratedSha256 = generated,
    Loaded = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic && a.GetName().Name!.StartsWith("Erlang.")).Select(a => new { a.Location, Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(a.Location))) })
}, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(passed ? "PASS" : "FAIL");
return passed ? 0 : 1;
