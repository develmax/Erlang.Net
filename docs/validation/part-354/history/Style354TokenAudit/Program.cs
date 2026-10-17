using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

string root = Path.GetFullPath(args[0]);
string view = Path.Combine(root, "artifacts/size-bif354-owned-style-view");
string output = Path.GetFullPath(args[1]);
if (File.Exists(output))
    throw new InvalidOperationException("Output already exists");
using var inputs = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root, "artifacts/size-bif354-permanent-test-proposal2/style-inputs.json")));
var rows = new List<object>();
foreach (var row in inputs.RootElement.GetProperty("Files").EnumerateArray())
{
    string path = row.GetProperty("Path").GetString()!;
    string beforePath = Path.Combine(root, row.GetProperty("SourcePath").GetString()!);
    string afterPath = Path.Combine(view, path);
    byte[] before = File.ReadAllBytes(beforePath), after = File.ReadAllBytes(afterPath);
    string beforeHash = Convert.ToHexString(SHA256.HashData(before));
    if (beforeHash != row.GetProperty("Sha256").GetString())
        throw new InvalidOperationException("Input changed: " + path);
    var options = new CSharpParseOptions(LanguageVersion.Preview);
    var oldTree = CSharpSyntaxTree.ParseText(System.Text.Encoding.UTF8.GetString(before), options);
    var newTree = CSharpSyntaxTree.ParseText(System.Text.Encoding.UTF8.GetString(after), options);
    if (oldTree.GetDiagnostics().Concat(newTree.GetDiagnostics()).Any(value => value.Severity == DiagnosticSeverity.Error))
        throw new InvalidOperationException("Syntax diagnostics: " + path);
    var oldTokens = oldTree.GetRoot().DescendantTokens().Select(token => (token.RawKind, token.Text)).ToArray();
    var newTokens = newTree.GetRoot().DescendantTokens().Select(token => (token.RawKind, token.Text)).ToArray();
    if (!oldTokens.SequenceEqual(newTokens))
        throw new InvalidOperationException("Token changed: " + path);
    string[] Directives(SyntaxTree tree) => tree.GetRoot().DescendantTrivia(descendIntoTrivia: true)
        .Where(trivia => trivia.GetStructure() is DirectiveTriviaSyntax)
        .Select(trivia => trivia.ToString()).ToArray();
    if (!Directives(oldTree).SequenceEqual(Directives(newTree)))
        throw new InvalidOperationException("Directive changed: " + path);
    rows.Add(new { Path = path, BeforeSha256 = beforeHash, AfterSha256 = Convert.ToHexString(SHA256.HashData(after)), Tokens = oldTokens.Length });
}
if (rows.Count != 50)
    throw new InvalidOperationException("Wrong owned file count");
await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { Status = "PASS_ROSLYN_TOKEN_AND_DIRECTIVE_IDENTITY_FOR_OWNED_STYLE", Files = rows, FixtureLiteralTokensIncluded = true, RuntimeOrGuestExecuted = false }, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("PASS 50 owned C# token/directive identities");
