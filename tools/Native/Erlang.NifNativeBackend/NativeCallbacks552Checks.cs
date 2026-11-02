using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.NifNativeBackend;

internal static class NativeCallbacks552Checks
{
    internal static async Task Run(string[] arguments)
    {
        if (!OperatingSystem.IsWindows() || IntPtr.Size != 8 || arguments.Length != 2 || !Path.IsPathFullyQualified(arguments[0]) || Directory.Exists(arguments[0]))
            throw new InvalidDataException(NativeCallbacks552Names.Failure);
        string runtimePath = typeof(ProcessRuntime).Assembly.Location;
        string runtimeSha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(runtimePath)));
        if (!runtimeSha.Equals(arguments[1], StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException(NativeCallbacks552Names.Failure);
        Directory.CreateDirectory(arguments[0]);
        (string Name, Func<Task> Run)[] cases =
        [
            (NativeCallbacks552Names.Layout, () => NifNativeTypeHostChecks.Run(NifNativeBridgeFixedHostChecks.Layout)),
            (NativeCallbacks552Names.LongWidth, () => NifNativeTypeHostChecks.Run(NifNativeBridgeFixedHostChecks.LongWidth)),
            (NativeCallbacks552Names.Compound, () => NifNativeTypeHostChecks.Run(NifNativeBridgeFixedHostChecks.CompoundTerms)),
            (NativeCallbacks552Names.Printf, NifPrintf791HostChecks.FixedTermRenderAndCleanup),
            (NativeCallbacks552Names.Optional, () =>
            {
                OptionalNative793Checks.ProtocolWindows();

                return Task.CompletedTask;
            })
        ];
        var results = new List<object>();
        int passed = 0;
        foreach (var item in cases)
        {
            try
            {
                await item.Run();
                passed++;
                results.Add(new { item.Name, Passed = true, Failure = (string?)null });
            }
            catch (Exception failure)
            {
                results.Add(new { item.Name, Passed = false, Failure = failure.ToString() });
            }
        }
        File.WriteAllText(
            Path.Combine(arguments[0], "completion.json"),
            JsonSerializer.Serialize(
                new
                {
                    Passed = passed == cases.Length,
                    Expected = cases.Length,
                    PassedCount = passed,
                    RuntimePath = runtimePath,
                    RuntimeSha256 = runtimeSha,
                    RuntimeMvid = typeof(ProcessRuntime).Assembly.ManifestModule.ModuleVersionId,
                    SourceOnlyOriginalCompatibilityClaimed = false,
                    ApiDelta = 0,
                    FullCompatibilityClaimed = false,
                    Results = results
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        if (passed != cases.Length) throw new InvalidOperationException(NativeCallbacks552Names.Failure);
    }
}
