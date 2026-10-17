using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var options = new CSharpParseOptions(LanguageVersion.Preview);
var before = CSharpSyntaxTree.ParseText(File.ReadAllText(args[0]), options);
var after = CSharpSyntaxTree.ParseText(File.ReadAllText(args[1]), options);
if (before.GetDiagnostics().Concat(after.GetDiagnostics()).Any(x => x.Severity == DiagnosticSeverity.Error))
    throw new InvalidOperationException("Invalid C# source");
var oldTokens = before.GetRoot().DescendantTokens().ToArray();
var newTokens = after.GetRoot().DescendantTokens().ToArray();
if (oldTokens.Length != newTokens.Length)
    throw new InvalidOperationException("Token count changed");
int changed = 0;
for (int i = 0; i < oldTokens.Length; i++)
{
    var oldToken = oldTokens[i];
    var newToken = newTokens[i];
    if (oldToken.RawKind != newToken.RawKind || oldToken.ValueText != newToken.ValueText)
        throw new InvalidOperationException("Token value changed");
    if (oldToken.Text != newToken.Text)
    {
        if (!oldToken.IsKind(SyntaxKind.MultiLineRawStringLiteralToken))
            throw new InvalidOperationException("Non-raw token changed");
        changed++;
    }
}
string[] Directives(SyntaxTree tree) => tree.GetRoot().DescendantTrivia(descendIntoTrivia: true)
    .Where(t => t.GetStructure() is DirectiveTriviaSyntax).Select(t => t.ToString()).ToArray();
if (!Directives(before).SequenceEqual(Directives(after)))
    throw new InvalidOperationException("Directives changed");
var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(args[2]));
var type = assembly.GetType("Erlang.Differential.SizeErrorCaseSources", true)!;
var rows = new List<object>();
foreach (var variable in after.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>())
{
    if (variable.Initializer?.Value is not LiteralExpressionSyntax literal)
        throw new InvalidOperationException("Unexpected fixture initializer");
    string actual = (string)type.GetField(variable.Identifier.ValueText)!.GetRawConstantValue()!;
    if (actual != literal.Token.ValueText)
        throw new InvalidOperationException("Compiled fixture constant differs");
    rows.Add(new { Name = variable.Identifier.ValueText, Utf8Sha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(actual))) });
}
if (rows.Count != 23 || changed == 0)
    throw new InvalidOperationException("Wrong fixture scope");
string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
using var output = new FileStream(args[3], FileMode.CreateNew);
await JsonSerializer.SerializeAsync(output, new
{
    Status = "PASS_ROSLYN_VALUES_AND_ACTUAL_FULL4_COMPILED_23_FIXTURES_UNCHANGED",
    BeforeSha256 = Hash(args[0]), AfterSha256 = Hash(args[1]), Full4DifferentialDllSha256 = Hash(args[2]),
    Tokens = oldTokens.Length, ChangedRawLiteralTokenCount = changed, Fixtures = rows,
    TestsRerun = false, RuntimeProductionChanged = false
}, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine("PASS: all 23 raw fixture values match the tested full4 assembly");
