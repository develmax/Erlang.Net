using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

internal static class MapValue503CallbackDriver
{
    internal static async Task Run(string[] arguments)
    {
        Require(arguments.Length == 4 && OperatingSystem.IsWindows());
        Verify(arguments[0], arguments[1]);
        var admission = JsonSerializer.Deserialize<MapValue503CallbackAdmission>(File.ReadAllBytes(arguments[0]))
            ?? throw new InvalidOperationException(MapValue503CallbackNames.Broken);
        Require(admission.Status == MapValue503CallbackNames.Admission);
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
        Require(planned.GetProperty(MapValue503CallbackNames.ReceiptNativeSourceSha256).GetString() == admission.NativeSourceSha256);
        Require(planned.GetProperty(MapValue503CallbackNames.ReceiptErlangSourceSha256).GetString() == admission.ErlangSourceSha256);
        using var build = JsonDocument.Parse(File.ReadAllBytes(admission.BuildReceiptPath));
        JsonElement receipt = build.RootElement;
        Require(receipt.GetProperty(MapValue503CallbackNames.ReceiptStatus).GetString() == MapValue503CallbackNames.Build);
        Require(receipt.GetProperty(MapValue503CallbackNames.ReceiptPinnedCommit).GetString() == MapValue503CallbackNames.Pin);
        Require(receipt.GetProperty(MapValue503CallbackNames.ReceiptPlanSha256).GetString() == admission.PlanSha256);
        Require(receipt.GetProperty(MapValue503CallbackNames.ReceiptLibrarySha256).GetString() == admission.LibrarySha256);
        Require(receipt.GetProperty(MapValue503CallbackNames.ReceiptNativeSourceSha256).GetString() == admission.NativeSourceSha256);
        Require(receipt.GetProperty(MapValue503CallbackNames.ReceiptErlangSourceSha256).GetString() == admission.ErlangSourceSha256);
        Require(receipt.GetProperty(MapValue503CallbackNames.ReceiptCompileExitCode).GetInt32() == 0 && receipt.GetProperty(MapValue503CallbackNames.ReceiptLinkExitCode).GetInt32() == 0);
        Require(receipt.GetProperty(MapValue503CallbackNames.CompiledSourceField).GetString() == admission.NativeSourceSha256);
        string route = arguments[3];
        Require(route is MapValue503CallbackNames.Ordinary or MapValue503CallbackNames.Reviewed);
        string output = Path.GetFullPath(arguments[2]);
        Require(!Directory.Exists(output) && Path.IsPathFullyQualified(admission.LibraryPath));
        Require(admission.LibraryPath.EndsWith(MapValue503CallbackNames.LibrarySuffix, StringComparison.OrdinalIgnoreCase));
        Require(MapValue503NativeModuleHandle.Find(library) == 0);
        Directory.CreateDirectory(output);
        int environments = NifNativeCallEnvironment.ActiveCount;
        long buffers = NifNativeBinaryStorage.LiveBufferCount;
        int nativeOwned = NifNativeBinaryRegistry.NativeOwnedCount;
        int reviewedPolicyCalls = 0;
        int expectedPolicyCalls = route == MapValue503CallbackNames.Reviewed ? 1 : 0;
        using var compiled = GeneratedModuleCompiler.Compile(File.ReadAllText(admission.ErlangSourcePath));
        await using (var runtime = new ProcessRuntime())
        {
            compiled.Definition.Register(runtime.Modules);
            var image = runtime.Modules.CurrentCodeImage(MapValue503CallbackNames.Module)
                ?? throw new InvalidOperationException(MapValue503CallbackNames.Broken);
            if (route == MapValue503CallbackNames.Reviewed)
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
                        MapValue503CallbackNames.Load,
                        context,
                        [Term.String(library[..^MapValue503CallbackNames.LibrarySuffix.Length])]
                    );
                    File.WriteAllBytes(Path.Combine(output, MapValue503CallbackNames.Loaded), ExternalTermFormat.Encode(loaded));
                    Require(loaded.Equals(Term.A(MapValue503CallbackNames.Ok)));
                    Require(Volatile.Read(ref reviewedPolicyCalls) == expectedPolicyCalls);
                    Require(MapValue503NativeModuleHandle.Find(library) != 0);
                    Term observed = await Execution.InvokeAsync(
                        compiled.Definition,
                        MapValue503CallbackNames.Run,
                        context,
                        []
                    );
                    Require(MapValue503NativeModuleHandle.Find(library) != 0);
                    File.WriteAllBytes(Path.Combine(output, MapValue503CallbackNames.Raw), ExternalTermFormat.Encode(observed));
                    Require(observed is TupleTerm { Items.Count: 6 });
                }
                );
            }
            finally
            {
                await image.NifBindings.RetireAsync().WaitAsync(NifNativeHarnessNames.Deadline);
            }
        }
        Require(MapValue503NativeModuleHandle.Find(library) == 0);
        Require(Volatile.Read(ref reviewedPolicyCalls) == expectedPolicyCalls);
        Require(NifNativeCallEnvironment.ActiveCount == environments);
        Require(NifNativeBinaryStorage.LiveBufferCount == buffers && NifNativeBinaryRegistry.NativeOwnedCount == nativeOwned);
        Verify(admission.LibraryPath, admission.LibrarySha256);
        File.WriteAllText(
            Path.Combine(output, MapValue503CallbackNames.Completion),
            JsonSerializer.Serialize(new
            {
                Status = MapValue503CallbackNames.Retired,
                Route = route,
                ReviewedPolicyInvocationCount = Volatile.Read(ref reviewedPolicyCalls),
                AdmissionSha256 = arguments[1],
                admission.BuildReceiptSha256,
                admission.LibrarySha256,
                ReturnedSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output, MapValue503CallbackNames.Raw)))),
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
            throw new InvalidOperationException(MapValue503CallbackNames.Broken);
    }
}
