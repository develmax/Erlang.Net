using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Compiler;
if (args.Length != 3) return 2;
string repository = Path.GetFullPath(args[0]);
string tool = Path.GetFullPath(args[1]);
string output = Path.GetFullPath(args[2]);
if (File.Exists(output)) return 2;
using var profile = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(tool, "profile.json")));
var paths = new List<string>();
foreach (var source in profile.RootElement.GetProperty(ParseGateNames.FixtureSources).EnumerateArray())
{
    paths.Add(Path.GetFullPath(Path.Combine(source.GetProperty(ParseGateNames.Location).GetString() == ParseGateNames.Tool ? tool : repository, source.GetProperty(ParseGateNames.Path).GetString()!)));
}
if (paths.Count != paths.Distinct(StringComparer.Ordinal).Count() || paths.Count != profile.RootElement.GetProperty(ParseGateNames.ExpectedUniqueSourceCount).GetInt32())
    throw new InvalidOperationException(ParseGateNames.SourceCountMismatch);
var results = new List<object>();
int failed = 0;
foreach (string path in paths)
{
    string hash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path)));
    try
    {
        new Parser(await File.ReadAllTextAsync(path)).ParseModule();
        results.Add(new { Path = path, Sha256 = hash, Passed = true });
    }
    catch (Exception error)
    {
        failed++;
        results.Add(new { Path = path, Sha256 = hash, Passed = false, Error = error.ToString() });
    }
}
await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { Status = failed == 0 ? "PASS_FIXTURE_PARSE_GATE" : "FAIL_FIXTURE_PARSE_GATE", Fixtures = results }, new JsonSerializerOptions { WriteIndented = true }));
return failed == 0 ? 0 : 1;
