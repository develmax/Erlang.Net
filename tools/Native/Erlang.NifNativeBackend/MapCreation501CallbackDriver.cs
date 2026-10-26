using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

internal static class MapCreation501CallbackDriver
{
    internal static async Task Run(string[] arguments)
    {
        Require(arguments.Length == 4 && OperatingSystem.IsWindows());
        Verify(arguments[0], arguments[1]);
        var admission = JsonSerializer.Deserialize<MapCreation501CallbackAdmission>(File.ReadAllBytes(arguments[0]))
            ?? throw new InvalidOperationException(MapCreation501CallbackNames.Broken);
        Require(admission.Status == MapCreation501CallbackNames.Admission);
        foreach (string path in new[] { admission.BuildReceiptPath, admission.PlanPath, admission.LibraryPath, admission.NativeSourcePath, admission.ErlangSourcePath })
            Require(Path.IsPathFullyQualified(path));
        string library = Path.GetFullPath(admission.LibraryPath);
        Verify(admission.BuildReceiptPath, admission.BuildReceiptSha256);
        Verify(admission.PlanPath, admission.PlanSha256);
        Verify(admission.LibraryPath, admission.LibrarySha256);
        Verify(admission.NativeSourcePath, admission.NativeSourceSha256);
        Verify(admission.ErlangSourcePath, admission.ErlangSourceSha256);
        using var plan = JsonDocument.Parse(File.ReadAllBytes(admission.PlanPath));
        JsonElement planned = plan.RootElement;
        Require(planned.GetProperty(MapCreation501CallbackNames.ReceiptNativeSourceSha256).GetString() == admission.NativeSourceSha256);
        Require(planned.GetProperty(MapCreation501CallbackNames.ReceiptErlangSourceSha256).GetString() == admission.ErlangSourceSha256);
        using var build = JsonDocument.Parse(File.ReadAllBytes(admission.BuildReceiptPath));
        JsonElement receipt = build.RootElement;
        Require(receipt.GetProperty(MapCreation501CallbackNames.ReceiptStatus).GetString() == MapCreation501CallbackNames.Build);
        Require(receipt.GetProperty(MapCreation501CallbackNames.ReceiptPinnedCommit).GetString() == MapCreation501CallbackNames.Pin);
        Require(receipt.GetProperty(MapCreation501CallbackNames.ReceiptPlanSha256).GetString() == admission.PlanSha256);
        Require(receipt.GetProperty(MapCreation501CallbackNames.ReceiptLibrarySha256).GetString() == admission.LibrarySha256);
        Require(receipt.GetProperty(MapCreation501CallbackNames.ReceiptNativeSourceSha256).GetString() == admission.NativeSourceSha256);
        Require(receipt.GetProperty(MapCreation501CallbackNames.ReceiptErlangSourceSha256).GetString() == admission.ErlangSourceSha256);
        Require(receipt.GetProperty(MapCreation501CallbackNames.ReceiptCompileExitCode).GetInt32() == 0 && receipt.GetProperty(MapCreation501CallbackNames.ReceiptLinkExitCode).GetInt32() == 0);
        Require(receipt.GetProperty(MapCreation501CallbackNames.CompiledSourceField).GetString() == admission.NativeSourceSha256);
        string route = arguments[3];
        Require(route is MapCreation501CallbackNames.Ordinary or MapCreation501CallbackNames.Reviewed);
        string output = Path.GetFullPath(arguments[2]);
        Require(!Directory.Exists(output) && Path.IsPathFullyQualified(admission.LibraryPath));
        Require(admission.LibraryPath.EndsWith(MapCreation501CallbackNames.LibrarySuffix, StringComparison.OrdinalIgnoreCase));
        Require(MapCreation501NativeModuleHandle.Find(library) == 0);
        Directory.CreateDirectory(output);
        int environments = NifNativeCallEnvironment.ActiveCount;
        long buffers = NifNativeBinaryStorage.LiveBufferCount;
        int nativeOwned = NifNativeBinaryRegistry.NativeOwnedCount;
        int reviewedPolicyCalls = 0;
        int expectedPolicyCalls = route == MapCreation501CallbackNames.Reviewed ? 1 : 0;
        using var compiled = GeneratedModuleCompiler.Compile(File.ReadAllText(admission.ErlangSourcePath));
        await using (var runtime = new ProcessRuntime())
        {
            compiled.Definition.Register(runtime.Modules);
            var image = runtime.Modules.CurrentCodeImage(MapCreation501CallbackNames.Module)
                ?? throw new InvalidOperationException(MapCreation501CallbackNames.Broken);
            if (route == MapCreation501CallbackNames.Reviewed)
                runtime.Modules.InstallReviewedNativeExecutionPolicy(path =>
                {
                    Interlocked.Increment(ref reviewedPolicyCalls);
                    Require(string.Equals(Path.GetFullPath(path), library, StringComparison.OrdinalIgnoreCase));

                    return new NifNativeReviewedBasicAbi(library, admission.LibrarySha256, admission.BuildReceiptSha256);
                });
            try
            {
                await NifNativeHarness.Context(
                    runtime,
                    async context =>
                {
                    Term loaded = await Execution.InvokeAsync(
                        compiled.Definition,
                        MapCreation501CallbackNames.Load,
                        context,
                        [Term.String(library[..^MapCreation501CallbackNames.LibrarySuffix.Length])]
                    );
                    File.WriteAllBytes(Path.Combine(output, MapCreation501CallbackNames.Loaded), ExternalTermFormat.Encode(loaded));
                    Require(loaded.Equals(Term.A(MapCreation501CallbackNames.Ok)));
                    Require(Volatile.Read(ref reviewedPolicyCalls) == expectedPolicyCalls);
                    Require(MapCreation501NativeModuleHandle.Find(library) != 0);
                    Term observed = await Execution.InvokeAsync(
                        compiled.Definition,
                        MapCreation501CallbackNames.Run,
                        context,
                        []
                    );
                    Require(MapCreation501NativeModuleHandle.Find(library) != 0);
                    File.WriteAllBytes(Path.Combine(output, MapCreation501CallbackNames.Raw), ExternalTermFormat.Encode(observed));
                    Require(observed is TupleTerm { Items.Count: 6 });
                }
                );
            }
            finally
            {
                await image.NifBindings.RetireAsync().WaitAsync(NifNativeHarnessNames.Deadline);
            }
        }
        Require(MapCreation501NativeModuleHandle.Find(library) == 0);
        Require(Volatile.Read(ref reviewedPolicyCalls) == expectedPolicyCalls);
        Require(NifNativeCallEnvironment.ActiveCount == environments);
        Require(NifNativeBinaryStorage.LiveBufferCount == buffers && NifNativeBinaryRegistry.NativeOwnedCount == nativeOwned);
        Verify(admission.LibraryPath, admission.LibrarySha256);
        File.WriteAllText(
            Path.Combine(output, MapCreation501CallbackNames.Completion),
            JsonSerializer.Serialize(new
            {
                Status = MapCreation501CallbackNames.Retired,
                Route = route,
                ReviewedPolicyInvocationCount = Volatile.Read(ref reviewedPolicyCalls),
                AdmissionSha256 = arguments[1],
                admission.BuildReceiptSha256,
                admission.LibrarySha256,
                ReturnedSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output, MapCreation501CallbackNames.Raw)))),
                EnvironmentBaselineRestored = true,
                BinaryOwnershipBaselineRestored = true,
                NativeModuleAbsentBeforeLoad = true,
                NativeModulePresentDuringInvocation = true,
                NativeModuleAbsentAfterRetirementAndRuntimeDisposal = true,
                OriginalComparisonExecuted = false,
                NoExpectedResultSeededFromManaged = true
            })
        );
    }

    private static void Verify(string path, string expected)
        => Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) == expected);

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(MapCreation501CallbackNames.Broken);
    }
}
