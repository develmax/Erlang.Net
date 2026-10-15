using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang.Compiler;

if (args.Length != 2)
    throw new ArgumentException("Expected original parent differential.json and output guard paths");

using var report = JsonDocument.Parse(File.ReadAllBytes(args[0]));
var rows = report.RootElement.GetProperty("ModuleResults").EnumerateArray().ToArray();
if (rows.Length != 4895)
    throw new InvalidOperationException("Expected exactly 4895 historical module rows");

var results = new List<object>();
int failed = 0;
var seen = new HashSet<string>(StringComparer.Ordinal);
foreach (var row in rows)
{
    string name = row.GetProperty("Name").GetString()!;
    if (!seen.Add(name))
        throw new InvalidOperationException("Duplicate historical module name: " + name);

    string source = row.GetProperty("Source").GetString()!;
    string expectedSource = row.GetProperty("SourceSha256").GetString()!;
    string expectedGenerated = row.GetProperty("CSharpSha256").GetString()!;
    string actualSource = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    string generated = CodeGeneration.CompileModule(source, "differential.erl");
    string actualGenerated = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(generated)));
    bool passed = actualSource == expectedSource && actualGenerated == expectedGenerated;
    if (!passed)
        failed++;

    results.Add(new { Name = name, Passed = passed, ExpectedSourceSha256 = expectedSource, SourceSha256 = actualSource, ExpectedCSharpSha256 = expectedGenerated, CSharpSha256 = actualGenerated });
}

File.WriteAllText(args[1], JsonSerializer.Serialize(new
{
    Status = failed == 0 ? "HISTORICAL4895_SOURCE_AND_GENERATED_CSHARP_EXACT" : "HISTORICAL_GENERATED_CSHARP_MISMATCH",
    HistoricalReportSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))),
    HistoricalModules = rows.Length,
    Passed = rows.Length - failed,
    Failed = failed,
    CompilerAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(CodeGeneration).Assembly.Location))),
    SourceFile = "differential.erl",
    RuntimeExecution = false,
    OriginalOtpExecution = false,
    Results = results
}, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
Console.WriteLine($"Historical generated C#: {rows.Length - failed}/{rows.Length}; failed {failed}");

return failed == 0 ? 0 : 1;
