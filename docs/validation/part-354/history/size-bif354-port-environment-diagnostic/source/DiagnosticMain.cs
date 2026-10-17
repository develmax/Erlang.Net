using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

internal static class DiagnosticMain
{
    private static string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static object Value(object item, string property) => item.GetType().GetProperty(property)!.GetValue(item)!;

    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 3) throw new ArgumentException("Expected actual DLL directory, exact fixture name, new output");
        string directory = Path.GetFullPath(args[0]);
        string name = args[1];
        string output = Path.GetFullPath(args[2]);
        if (File.Exists(output)) throw new IOException("Existing output refused");
        var before = Directory.GetFiles(directory, "*.dll").Order(StringComparer.Ordinal).Select(path => new { Path = path, Sha256 = Digest(path) }).ToArray();
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            string path = Path.Combine(directory, assemblyName.Name + ".dll");
            return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        }
        AssemblyLoadContext.Default.Resolving += Resolve;
        var watch = Stopwatch.StartNew();
        bool passed = false;
        string? failure = null;
        object? observation = null;
        try
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(directory, DiagnosticNames.Differential + ".dll"));
            var fixtures = (IEnumerable)assembly.GetType(DiagnosticNames.Cases, true)!.GetProperty(DiagnosticNames.All)!.GetValue(null)!;
            object fixture = fixtures.Cast<object>().Single(item => (string)Value(item, DiagnosticNames.Name) == name);
            string source = (string)Value(fixture, DiagnosticNames.Source);
            var compiler = assembly.GetType(DiagnosticNames.Compiler, true)!;
            using var artifact = (IDisposable)compiler.GetMethod(DiagnosticNames.Compile)!.Invoke(null, [source])!;
            var execution = assembly.GetType(DiagnosticNames.Execution, true)!;
            var pending = (Task)execution.GetMethod(DiagnosticNames.RunCaptured)!.Invoke(null, [artifact, Value(fixture, DiagnosticNames.Caller)])!;
            await pending;
            object captured = Value(pending, DiagnosticNames.Result);
            object actual = Value(captured, DiagnosticNames.Result);
            object expected = Value(fixture, DiagnosticNames.Expected);
            string stdout = (string)Value(captured, DiagnosticNames.Stdout);
            string stderr = (string)Value(captured, DiagnosticNames.Stderr);
            bool streams = true;
            if (Value(fixture, DiagnosticNames.ExpectedStdout) is not null || Value(fixture, DiagnosticNames.ExpectedStderr) is not null)
                streams = (bool)assembly.GetType(DiagnosticNames.Contract, true)!.GetMethod(DiagnosticNames.Matches)!.Invoke(null, [fixture, stdout, stderr])!;
            passed = actual.Equals(expected) && streams;
            observation = new { SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))), CallerContext = Value(fixture, DiagnosticNames.Caller), CompilerPolicy = Value(fixture, DiagnosticNames.Policy), CSharpSha256 = Value(artifact, DiagnosticNames.Generation), Actual = actual.ToString(), Expected = expected.ToString(), StandardOutputHex = stdout, StandardErrorHex = stderr, StreamsMatchOriginalFixtureContract = streams };
        }
        catch (Exception error)
        {
            failure = (error is TargetInvocationException { InnerException: { } inner } ? inner : error).ToString();
        }
        finally { AssemblyLoadContext.Default.Resolving -= Resolve; }
        var loaded = AssemblyLoadContext.Default.Assemblies.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location) && Path.GetDirectoryName(a.Location) == directory).OrderBy(a => a.Location, StringComparer.Ordinal).Select(a => new { Path = a.Location, Sha256 = Digest(a.Location) }).ToArray();
        bool unchanged = before.All(row => Digest(row.Path) == row.Sha256);
        using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, new { Name = name, Passed = passed && unchanged, Failure = failure, ElapsedSeconds = watch.Elapsed.TotalSeconds, Observation = observation, BeforeAssemblies = before, LoadedAssemblies = loaded, InputAssembliesUnchanged = unchanged, Environment = new { RuntimeInformation.FrameworkDescription, RuntimeInformation.OSDescription, RuntimeInformation.ProcessArchitecture, RuntimeInformation.OSArchitecture, CurrentDirectory = Environment.CurrentDirectory }, FullValidationExecuted = false, OriginalOtpExecuted = false, EnvironmentCausalityClaim = false }, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine((passed && unchanged ? "PASS " : "FAIL ") + name);
        return passed && unchanged ? 0 : 1;
    }
}
