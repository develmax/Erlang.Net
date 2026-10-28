using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

internal static class MapReentry505CallbackDriver
{
    internal static async Task Run(string[] arguments)
    {
        Require(arguments.Length == 4 && OperatingSystem.IsWindows());
        Verify(arguments[0], arguments[1]);
        var admission = JsonSerializer.Deserialize<MapReentry505CallbackAdmission>(File.ReadAllBytes(arguments[0]))
            ?? throw new InvalidOperationException(MapReentry505CallbackNames.Broken);
        Require(admission.Status == MapReentry505CallbackNames.Admission);
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
        Require(planned.GetProperty(MapReentry505CallbackNames.ReceiptNativeSourceSha256).GetString() == admission.NativeSourceSha256);
        Require(planned.GetProperty(MapReentry505CallbackNames.ReceiptErlangSourceSha256).GetString() == admission.ErlangSourceSha256);
        using var build = JsonDocument.Parse(File.ReadAllBytes(admission.BuildReceiptPath));
        JsonElement receipt = build.RootElement;
        Require(receipt.GetProperty(MapReentry505CallbackNames.ReceiptStatus).GetString() == MapReentry505CallbackNames.Build);
        Require(receipt.GetProperty(MapReentry505CallbackNames.ReceiptPinnedCommit).GetString() == MapReentry505CallbackNames.Pin);
        Require(receipt.GetProperty(MapReentry505CallbackNames.ReceiptPlanSha256).GetString() == admission.PlanSha256);
        Require(receipt.GetProperty(MapReentry505CallbackNames.ReceiptLibrarySha256).GetString() == admission.LibrarySha256);
        Require(receipt.GetProperty(MapReentry505CallbackNames.ReceiptNativeSourceSha256).GetString() == admission.NativeSourceSha256);
        Require(receipt.GetProperty(MapReentry505CallbackNames.ReceiptErlangSourceSha256).GetString() == admission.ErlangSourceSha256);
        Require(receipt.GetProperty(MapReentry505CallbackNames.ReceiptCompileExitCode).GetInt32() == 0 && receipt.GetProperty(MapReentry505CallbackNames.ReceiptLinkExitCode).GetInt32() == 0);
        Require(receipt.GetProperty(MapReentry505CallbackNames.CompiledSourceField).GetString() == admission.NativeSourceSha256);
        string route = arguments[3];
        Require(route is MapReentry505CallbackNames.Ordinary or MapReentry505CallbackNames.Reviewed);
        string output = Path.GetFullPath(arguments[2]);
        Require(!Directory.Exists(output) && Path.IsPathFullyQualified(admission.LibraryPath));
        Require(admission.LibraryPath.EndsWith(MapReentry505CallbackNames.LibrarySuffix, StringComparison.OrdinalIgnoreCase));
        Require(MapReentry505NativeModuleHandle.Find(library) == 0);
        Directory.CreateDirectory(output);
        int environments = NifNativeCallEnvironment.ActiveCount;
        long buffers = NifNativeBinaryStorage.LiveBufferCount;
        int nativeOwned = NifNativeBinaryRegistry.NativeOwnedCount;
        int reviewedPolicyCalls = 0;
        int expectedPolicyCalls = route == MapReentry505CallbackNames.Reviewed ? 1 : 0;
        using var compiled = GeneratedModuleCompiler.Compile(File.ReadAllText(admission.ErlangSourcePath));
        await using (var runtime = new ProcessRuntime())
        {
            compiled.Definition.Register(runtime.Modules);
            var image = runtime.Modules.CurrentCodeImage(MapReentry505CallbackNames.Module)
                ?? throw new InvalidOperationException(MapReentry505CallbackNames.Broken);
            if (route == MapReentry505CallbackNames.Reviewed)
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
                        MapReentry505CallbackNames.Load,
                        context,
                        [Term.String(library[..^MapReentry505CallbackNames.LibrarySuffix.Length])]
                    );
                    File.WriteAllBytes(Path.Combine(output, MapReentry505CallbackNames.Loaded), ExternalTermFormat.Encode(loaded));
                    Require(loaded.Equals(Term.A(MapReentry505CallbackNames.Ok)));
                    Require(Volatile.Read(ref reviewedPolicyCalls) == expectedPolicyCalls);
                    Require(MapReentry505NativeModuleHandle.Find(library) != 0);
                    Term observed = await Execution.InvokeAsync(
                        compiled.Definition,
                        MapReentry505CallbackNames.Run,
                        context,
                        []
                    );
                    Require(MapReentry505NativeModuleHandle.Find(library) != 0);
                    Term defaultEncoded = await runtime.Modules.Call(
                        context,
                        MapReentry505CallbackNames.Erlang,
                        MapReentry505CallbackNames.TermToBinary,
                        observed,
                        Term.List(Term.Tuple(Term.A(MapReentry505CallbackNames.MinorVersion), Term.I(MapReentry505CallbackNames.EtfMinorVersion)))
                    );
                    File.WriteAllBytes(Path.Combine(output, MapReentry505CallbackNames.DefaultRaw), ((BitString)defaultEncoded).ToArray());
                    Term encoded = await runtime.Modules.Call(
                        context,
                        MapReentry505CallbackNames.Erlang,
                        MapReentry505CallbackNames.TermToBinary,
                        observed,
                        Term.List(
                            Term.A(MapReentry505CallbackNames.Deterministic),
                            Term.Tuple(Term.A(MapReentry505CallbackNames.MinorVersion), Term.I(MapReentry505CallbackNames.EtfMinorVersion))
                        )
                    );
                    File.WriteAllBytes(Path.Combine(output, MapReentry505CallbackNames.Raw), ((BitString)encoded).ToArray());
                    Require(observed is TupleTerm { Items.Count: MapReentry505CallbackNames.ObservationGroups });
                }
                );
            }
            finally
            {
                await image.NifBindings.RetireAsync().WaitAsync(NifNativeHarnessNames.Deadline);
            }
        }
        Require(MapReentry505NativeModuleHandle.Find(library) == 0);
        Require(Volatile.Read(ref reviewedPolicyCalls) == expectedPolicyCalls);
        Require(NifNativeCallEnvironment.ActiveCount == environments);
        Require(NifNativeBinaryStorage.LiveBufferCount == buffers && NifNativeBinaryRegistry.NativeOwnedCount == nativeOwned);
        Verify(admission.LibraryPath, admission.LibrarySha256);
        File.WriteAllText(
            Path.Combine(output, MapReentry505CallbackNames.Completion),
            JsonSerializer.Serialize(new
            {
                Status = MapReentry505CallbackNames.Retired,
                Route = route,
                ReviewedPolicyInvocationCount = Volatile.Read(ref reviewedPolicyCalls),
                AdmissionSha256 = arguments[1],
                admission.BuildReceiptSha256,
                admission.LibrarySha256,
                ReturnedSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(output, MapReentry505CallbackNames.Raw)))),
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
            throw new InvalidOperationException(MapReentry505CallbackNames.Broken);
    }
}
