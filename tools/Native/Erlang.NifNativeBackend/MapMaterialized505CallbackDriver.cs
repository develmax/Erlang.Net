using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

internal static class MapMaterialized505CallbackDriver
{
    internal static async Task Run(string[] arguments)
    {
        Require(arguments.Length == 4 && OperatingSystem.IsWindows());
        Verify(arguments[0], arguments[1]);
        var admission = JsonSerializer.Deserialize<MapMaterialized505CallbackAdmission>(File.ReadAllBytes(arguments[0]))
            ?? throw new InvalidOperationException(MapMaterialized505CallbackNames.Broken);
        Require(admission.Status == MapMaterialized505CallbackNames.Admission);
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
        Require(planned.GetProperty(MapMaterialized505CallbackNames.ReceiptNativeSourceSha256).GetString() == admission.NativeSourceSha256);
        Require(planned.GetProperty(MapMaterialized505CallbackNames.ReceiptErlangSourceSha256).GetString() == admission.ErlangSourceSha256);
        using var build = JsonDocument.Parse(File.ReadAllBytes(admission.BuildReceiptPath));
        JsonElement receipt = build.RootElement;
        Require(receipt.GetProperty(MapMaterialized505CallbackNames.ReceiptStatus).GetString() == MapMaterialized505CallbackNames.Build);
        Require(receipt.GetProperty(MapMaterialized505CallbackNames.ReceiptPinnedCommit).GetString() == MapMaterialized505CallbackNames.Pin);
        Require(receipt.GetProperty(MapMaterialized505CallbackNames.ReceiptPlanSha256).GetString() == admission.PlanSha256);
        Require(receipt.GetProperty(MapMaterialized505CallbackNames.ReceiptLibrarySha256).GetString() == admission.LibrarySha256);
        Require(receipt.GetProperty(MapMaterialized505CallbackNames.ReceiptNativeSourceSha256).GetString() == admission.NativeSourceSha256);
        Require(receipt.GetProperty(MapMaterialized505CallbackNames.ReceiptErlangSourceSha256).GetString() == admission.ErlangSourceSha256);
        Require(receipt.GetProperty(MapMaterialized505CallbackNames.ReceiptCompileExitCode).GetInt32() == 0 && receipt.GetProperty(MapMaterialized505CallbackNames.ReceiptLinkExitCode).GetInt32() == 0);
        Require(receipt.GetProperty(MapMaterialized505CallbackNames.CompiledSourceField).GetString() == admission.NativeSourceSha256);
        string route = arguments[3];
        Require(route is MapMaterialized505CallbackNames.Ordinary or MapMaterialized505CallbackNames.Reviewed);
        string output = Path.GetFullPath(arguments[2]);
        Require(!Directory.Exists(output) && Path.IsPathFullyQualified(admission.LibraryPath));
        Require(admission.LibraryPath.EndsWith(MapMaterialized505CallbackNames.LibrarySuffix, StringComparison.OrdinalIgnoreCase));
        Require(MapMaterialized505NativeModuleHandle.Find(library) == 0);
        Directory.CreateDirectory(output);
        int environments = NifNativeCallEnvironment.ActiveCount;
        long buffers = NifNativeBinaryStorage.LiveBufferCount;
        int nativeOwned = NifNativeBinaryRegistry.NativeOwnedCount;
        int reviewedPolicyCalls = 0;
        int expectedPolicyCalls = route == MapMaterialized505CallbackNames.Reviewed ? 1 : 0;
        using var compiled = GeneratedModuleCompiler.Compile(File.ReadAllText(admission.ErlangSourcePath));
        await using (var runtime = new ProcessRuntime())
        {
            compiled.Definition.Register(runtime.Modules);
            var image = runtime.Modules.CurrentCodeImage(MapMaterialized505CallbackNames.Module)
                ?? throw new InvalidOperationException(MapMaterialized505CallbackNames.Broken);
            if (route == MapMaterialized505CallbackNames.Reviewed)
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
                        MapMaterialized505CallbackNames.Load,
                        context,
                        [Term.String(library[..^MapMaterialized505CallbackNames.LibrarySuffix.Length])]
                    );
                    File.WriteAllBytes(Path.Combine(output, MapMaterialized505CallbackNames.Loaded), ExternalTermFormat.Encode(loaded));
                    Require(loaded.Equals(Term.A(MapMaterialized505CallbackNames.Ok)));
                    Require(Volatile.Read(ref reviewedPolicyCalls) == expectedPolicyCalls);
                    Require(MapMaterialized505NativeModuleHandle.Find(library) != 0);
                    Term observed = await Execution.InvokeAsync(
                        compiled.Definition,
                        MapMaterialized505CallbackNames.Run,
                        context,
                        []
                    );
                    Require(MapMaterialized505NativeModuleHandle.Find(library) != 0);
                    Term defaultEncoded = await runtime.Modules.Call(
                        context,
                        MapMaterialized505CallbackNames.Erlang,
                        MapMaterialized505CallbackNames.TermToBinary,
                        observed,
                        Term.List(Term.Tuple(Term.A(MapMaterialized505CallbackNames.MinorVersion), Term.I(MapMaterialized505CallbackNames.EtfMinorVersion)))
                    );
                    File.WriteAllBytes(Path.Combine(output, MapMaterialized505CallbackNames.DefaultRaw), ((BitString)defaultEncoded).ToArray());
                    Term encoded = await runtime.Modules.Call(
                        context,
                        MapMaterialized505CallbackNames.Erlang,
                        MapMaterialized505CallbackNames.TermToBinary,
                        observed,
                        Term.List(
                            Term.A(MapMaterialized505CallbackNames.Deterministic),
                            Term.Tuple(Term.A(MapMaterialized505CallbackNames.MinorVersion), Term.I(MapMaterialized505CallbackNames.EtfMinorVersion))
                        )
                    );
                    File.WriteAllBytes(Path.Combine(output, MapMaterialized505CallbackNames.Raw), ((BitString)encoded).ToArray());
                    Require(Cons.Items(observed).Count() == MapMaterialized505CallbackNames.ObservationCases);
                }
                );
            }
            finally
            {
                await image.NifBindings.RetireAsync().WaitAsync(NifNativeHarnessNames.Deadline);
            }
        }
        Require(MapMaterialized505NativeModuleHandle.Find(library) == 0);
        Require(Volatile.Read(ref reviewedPolicyCalls) == expectedPolicyCalls);
        Require(NifNativeCallEnvironment.ActiveCount == environments);
        Require(NifNativeBinaryStorage.LiveBufferCount == buffers && NifNativeBinaryRegistry.NativeOwnedCount == nativeOwned);
        Verify(admission.LibraryPath, admission.LibrarySha256);
        File.WriteAllText(
            Path.Combine(output, MapMaterialized505CallbackNames.Completion),
            JsonSerializer.Serialize(new
            {
                Status = MapMaterialized505CallbackNames.Retired,
                Route = route,
                ReviewedPolicyInvocationCount = Volatile.Read(ref reviewedPolicyCalls),
                AdmissionSha256 = arguments[1],
                admission.BuildReceiptSha256,
                admission.LibrarySha256,
                ReturnedSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output, MapMaterialized505CallbackNames.Raw)))),
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
            throw new InvalidOperationException(MapMaterialized505CallbackNames.Broken);
    }
}
