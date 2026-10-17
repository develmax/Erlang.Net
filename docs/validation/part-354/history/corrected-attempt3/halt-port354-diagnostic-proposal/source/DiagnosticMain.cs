using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;

internal static class DiagnosticMain
{
    private static string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    public static async Task<int> Main(string[] args)
    {
        if (args.Length != 3 || !DiagnosticNames.Targets.TryGetValue(args[1], out var target))
            throw new ArgumentException(DiagnosticNames.Invalid);
        string directory = Path.GetFullPath(args[0]);
        string output = Path.GetFullPath(args[2]);
        if (File.Exists(output))
            throw new IOException(DiagnosticNames.Invalid);
        var before = Directory.GetFiles(directory, DiagnosticNames.Pattern).Order(StringComparer.Ordinal)
            .Select(path => new { Path = path, Sha256 = Digest(path) }).ToArray();
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            string path = Path.Combine(directory, name.Name + Path.GetExtension(DiagnosticNames.TestAssembly));

            return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        }
        AssemblyLoadContext.Default.Resolving += Resolve;
        // Root assigns this process to a kill-on-close job before permitting invocation.
        if (await Console.In.ReadLineAsync() != DiagnosticNames.Go)
            throw new InvalidOperationException(DiagnosticNames.Invalid);
        var watch = Stopwatch.StartNew();
        string? failure = null;
        bool completed = false;
        bool canonicalTimeout = false;
        Console.WriteLine(JsonSerializer.Serialize(new { Phase = DiagnosticNames.Start, Target = args[1], target.Method, target.Argument, Pid = Environment.ProcessId }));
        Console.Out.Flush();
        try
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(directory, DiagnosticNames.TestAssembly));
            var type = assembly.GetType(DiagnosticNames.TestType, true)!;
            var method = type.GetMethod(target.Method, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
            object?[]? arguments = target.Argument is { } value ? [value] : null;
            Task task = method.Invoke(null, arguments) as Task ?? throw new InvalidOperationException(DiagnosticNames.Invalid);
            if (target.Method == DiagnosticNames.Run)
            {
                try
                {
                    await task.WaitAsync(TimeSpan.FromSeconds(DiagnosticNames.CanonicalSeconds));
                }
                catch (TimeoutException) when (!task.IsCompleted)
                {
                    canonicalTimeout = true;
                    throw;
                }
            }
            else
                await task;
            completed = true;
        }
        catch (Exception error)
        {
            failure = (error is TargetInvocationException { InnerException: { } inner } ? inner : error).ToString();
        }
        finally
        {
            AssemblyLoadContext.Default.Resolving -= Resolve;
        }
        var loaded = AssemblyLoadContext.Default.Assemblies
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location) && Path.GetDirectoryName(a.Location) == directory)
            .OrderBy(a => a.Location, StringComparer.Ordinal)
            .Select(a => new { Path = a.Location, Sha256 = Digest(a.Location) }).ToArray();
        bool unchanged = before.All(row => File.Exists(row.Path) && Digest(row.Path) == row.Sha256);
        using (var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            await JsonSerializer.SerializeAsync(stream, new
            {
                Target = args[1], target.Method, target.Argument, Passed = completed && unchanged,
                Failure = failure, CanonicalOuterTimeout = canonicalTimeout, ElapsedSeconds = watch.Elapsed.TotalSeconds,
                BeforeAssemblies = before, LoadedAssemblies = loaded, InputAssembliesUnchanged = unchanged,
                CanonicalRunBudgetSeconds = DiagnosticNames.CanonicalSeconds, OriginalMethodBodiesModified = false,
                InnerTimeoutsModified = false, ManagedPortProbeExecuted = true, OriginalOtpExecuted = false,
                FullValidationExecuted = false
            }, new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine(JsonSerializer.Serialize(new { Phase = completed && unchanged ? DiagnosticNames.Pass : DiagnosticNames.Fail, Target = args[1], Failure = failure }));

        return completed && unchanged ? 0 : 1;
    }
}
