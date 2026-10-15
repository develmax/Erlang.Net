using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace PressureRawControls;

internal static class RawCaptureFocus
{
    private static readonly JsonSerializerOptions json = new() { WriteIndented = true };

    internal static async Task<int> Run(string[] arguments)
    {
        if (arguments.Length != RawCaptureNames.ArgumentCount)
            throw new ArgumentException(RawCaptureNames.Arguments);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(RawCaptureNames.Platform);
        string suppliedPath = Path.GetFullPath(arguments[0]);
        string python = arguments[1];
        string output = Path.GetFullPath(arguments[2]);
        if (Directory.Exists(output) || File.Exists(output))
            throw new IOException(RawCaptureNames.ExistingOutput);
        Directory.CreateDirectory(output);
        byte[] before = await File.ReadAllBytesAsync(suppliedPath);
        Assembly assembly = Assembly.LoadFrom(suppliedPath);
        if (!StringComparer.OrdinalIgnoreCase.Equals(Path.GetFullPath(assembly.Location), suppliedPath))
            throw new InvalidOperationException(RawCaptureNames.AssemblyMismatch);
        MethodInfo method = assembly.GetType(RawCaptureNames.ProcessType, throwOnError: true)!
            .GetMethod(RawCaptureNames.RunMethod, BindingFlags.NonPublic | BindingFlags.Static)!;
        string controls = Path.Combine(AppContext.BaseDirectory, RawCaptureNames.Controls);
        string rawScript = Path.Combine(controls, RawCaptureNames.RawScript);
        string parentScript = Path.Combine(controls, RawCaptureNames.ParentScript);
        string holderScript = Path.Combine(controls, RawCaptureNames.HolderScript);
        object binding = new
        {
            ActualAssembly = suppliedPath,
            ActualAssemblySha256 = Convert.ToHexString(SHA256.HashData(before)),
            ActualAssemblyMvid = assembly.ManifestModule.ModuleVersionId,
            ActualMethod = method.DeclaringType!.FullName + Type.Delimiter + method.Name,
            ReflectionInvokedSuppliedAssembly = true,
            RuntimeProjectReferenced = false,
            MainAssemblyClaimed = false,
            NativeOtpExecuted = false,
            Platform = Environment.OSVersion.ToString(),
            Python = python,
            Fixtures = new[] { rawScript, parentScript, holderScript }.Select(path => new
            {
                Path = path,
                Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))
            }).ToArray()
        };
        await Write(Path.Combine(output, RawCaptureNames.Binding), binding);
        JsonElement? raw = null;
        JsonElement? inherited = null;
        object? cleanup = null;
        Exception? failure = null;
        string nonce = Guid.NewGuid().ToString();
        string identity = Path.Combine(output, RawCaptureNames.ChildIdentity);
        Task<JsonElement>? inheritedTask = null;
        var inheritedClock = new Stopwatch();
        try
        {
            string rawPrefix = Path.Combine(output, RawCaptureNames.RawPrefix);
            raw = await Invoke(PythonArguments(rawScript), rawPrefix)
                .WaitAsync(TimeSpan.FromSeconds(RawCaptureNames.CaptureDeadlineSeconds));
            Require(raw.Value, RawCaptureNames.RawExitCode, complete: true, RawCaptureNames.RawMismatch);
            RequireBytes(rawPrefix, RawCaptureNames.RawStdout, RawCaptureNames.RawStderr, complete: true);
            inheritedClock.Start();
            string inheritedPrefix = Path.Combine(output, RawCaptureNames.InheritedPrefix);
            inheritedTask = Invoke(PythonArguments(
                parentScript,
                holderScript,
                identity,
                nonce,
                RawCaptureNames.HolderSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)
            ), inheritedPrefix);
            try
            {
                inherited = await inheritedTask.WaitAsync(TimeSpan.FromSeconds(RawCaptureNames.CaptureDeadlineSeconds));
                Require(inherited.Value, RawCaptureNames.ParentExitCode, complete: false, RawCaptureNames.PipeMismatch);
                RequireBytes(inheritedPrefix, RawCaptureNames.InheritedStdout, RawCaptureNames.InheritedStderr, complete: false);
            }
            finally
            {
                // The owned child is deliberately alive for longer than the complete control deadline.
                // Verify its PID, exact creation FILETIME and executable before touching that process alone.
                cleanup = await OwnedChildCleanup.Run(identity, nonce, inheritedTask, Path.Combine(output, RawCaptureNames.ChildCleanup));
                inheritedClock.Stop();
            }
            if (inheritedClock.Elapsed > TimeSpan.FromSeconds(RawCaptureNames.OverallDeadlineSeconds))
                throw new TimeoutException(RawCaptureNames.PipeMismatch);
            byte[] after = await File.ReadAllBytesAsync(suppliedPath);
            if (!before.AsSpan().SequenceEqual(after))
                throw new InvalidOperationException(RawCaptureNames.AssemblyMismatch);
        }
        catch (Exception exception)
        {
            failure = exception;
            inheritedClock.Stop();
        }
        var receipt = new
        {
            Status = failure is null ? RawCaptureNames.Pass : RawCaptureNames.Fail,
            Binding = binding,
            Raw = raw,
            InheritedPipes = inherited,
            InheritedElapsedMilliseconds = inheritedClock.ElapsedMilliseconds,
            CaptureDeadlineSeconds = RawCaptureNames.CaptureDeadlineSeconds,
            OverallDeadlineSeconds = RawCaptureNames.OverallDeadlineSeconds,
            Cleanup = cleanup,
            FailureType = failure?.GetType().FullName,
            Failure = failure?.ToString(),
            NativeOtpExecuted = false,
            MainAssemblyClaimed = false
        };
        await Write(Path.Combine(output, RawCaptureNames.Receipt), receipt);
        Console.WriteLine(JsonSerializer.Serialize(receipt));

        return failure is null ? 0 : 1;

        async Task<JsonElement> Invoke(string[] childArguments, string prefix)
        {
            var task = (Task)method.Invoke(null, [python, childArguments, prefix])!;
            await task;
            object result = task.GetType().GetProperty(RawCaptureNames.ResultProperty)!.GetValue(task)!;

            return JsonSerializer.SerializeToElement(result, result.GetType());
        }

        string[] PythonArguments(params string[] childArguments)
            => Path.GetFileNameWithoutExtension(python).Equals(RawCaptureNames.PythonLauncher, StringComparison.OrdinalIgnoreCase)
                ? [RawCaptureNames.PythonVersion, .. childArguments]
                : childArguments;
    }

    private static void Require(JsonElement result, int exitCode, bool complete, string diagnostic)
    {
        if (result.GetProperty(RawCaptureNames.ExitCode).GetInt32() != exitCode ||
            result.GetProperty(RawCaptureNames.TimedOut).GetBoolean() ||
            result.GetProperty(RawCaptureNames.RawComplete).GetBoolean() != complete)
            throw new InvalidOperationException(diagnostic);
    }

    private static void RequireBytes(string prefix, byte[] stdout, byte[] stderr, bool complete)
    {
        if (!File.ReadAllBytes(prefix + RawCaptureNames.StdoutSuffix).AsSpan().SequenceEqual(stdout) ||
            !File.ReadAllBytes(prefix + RawCaptureNames.StderrSuffix).AsSpan().SequenceEqual(stderr) ||
            File.ReadAllText(prefix + RawCaptureNames.CompleteSuffix) != (complete ? RawCaptureNames.CompleteTrue : RawCaptureNames.CompleteFalse) ||
            File.ReadAllText(prefix + RawCaptureNames.ExitSuffix) !=
                (complete ? RawCaptureNames.RawExitCode : RawCaptureNames.ParentExitCode).ToString(System.Globalization.CultureInfo.InvariantCulture) + RawCaptureNames.NewLine)
            throw new InvalidOperationException(complete ? RawCaptureNames.RawMismatch : RawCaptureNames.PipeMismatch);
    }

    private static Task Write(string path, object value)
        => File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, json) + RawCaptureNames.NewLine);
}
