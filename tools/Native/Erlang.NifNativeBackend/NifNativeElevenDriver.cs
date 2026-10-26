using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Compiler;
using Erlang.Differential;

namespace Erlang.NifNativeBackend;

// One case per externally owned process. Admission is a separately reviewed Root artifact.
internal static class NifNativeElevenDriver
{
    internal static async Task Run(string[] arguments, bool ordinaryHost = false)
    {
        NifNativeHarness.Require(arguments.Length == 4 && (OperatingSystem.IsLinux() || OperatingSystem.IsWindows()));
        string admissionPath = Path.GetFullPath(arguments[0]);
        Verify(admissionPath, arguments[1]);
        using var admission = JsonDocument.Parse(File.ReadAllBytes(admissionPath));
        JsonElement plan = admission.RootElement;
        NifNativeHarness.Require(Text(plan, "Status") == NifNativeElevenNames.Admission);
        string casesPath = Text(plan, "CasesPath");
        Verify(casesPath, NifNativeElevenNames.CasesSha256);
        using var caseDocument = JsonDocument.Parse(File.ReadAllBytes(casesPath));
        NifNativeHarness.Require(caseDocument.RootElement.GetProperty("Cases").GetArrayLength() == NifNativeElevenNames.CaseCount);
        string caseName = arguments[2];
        JsonElement selected = plan.GetProperty("Cases").EnumerateArray().Single(row => Text(row, "Name") == caseName);
        JsonElement specification = caseDocument.RootElement.GetProperty("Cases").EnumerateArray().Single(row => Text(row, "Name") == caseName);
        foreach (string key in NifNativeElevenNames.SpecificationKeys)
            NifNativeHarness.Require(Text(selected, key) == Text(specification, key));
        NifNativeHarness.Require(Text(selected, "SourceSha256") == Text(specification, "ErlangSourceSha256"));
        string buildReceipt = Text(plan, "NativeBuildReceiptPath");
        string buildHash = Text(plan, "NativeBuildReceiptSha256");
        Verify(buildReceipt, buildHash);
        using var build = JsonDocument.Parse(File.ReadAllBytes(buildReceipt));
        string expectedBuild = OperatingSystem.IsWindows()
            ? NifNativeElevenNames.WindowsBuildStatus : NifNativeElevenNames.LinuxBuildStatus;
        NifNativeHarness.Require(Text(build.RootElement, "Status") == expectedBuild);
        if (OperatingSystem.IsWindows())
            NifNativeHarness.Require(Text(build.RootElement, "Platform") == NifNativeElevenNames.WindowsPlatform);
        NifNativeHarness.Require(Text(build.RootElement, "SourceManifestSha256") == Text(plan, "CollectorManifestSha256"));
        string sourcePath = Text(selected, "SourcePath");
        Verify(sourcePath, Text(selected, "SourceSha256"));
        string imagePath = Text(selected, "LibraryPath");
        string imageHash = Text(selected, "LibrarySha256");
        Verify(imagePath, imageHash);
        JsonElement image = build.RootElement.GetProperty("Images").EnumerateArray().Single(row => Text(row, "Leaf") == Text(selected, "NativeLeaf"));
        NifNativeHarness.Require(Text(image, "Sha256") == imageHash);
        NifNativeHarness.Require(Text(image, "SourceSha256") == Text(selected, "NativeSourceSha256"));
        string? bridgePath = null;
        string? bridgeHash = null;
        if (OperatingSystem.IsLinux())
        {
            bridgePath = Text(plan, "BridgePath");
            bridgeHash = Text(plan, "BridgeSha256");
            Verify(bridgePath, bridgeHash);
            string bridgeReceipt = Text(plan, "BridgeBuildReceiptPath");
            Verify(bridgeReceipt, Text(plan, "BridgeBuildReceiptSha256"));
        }
        else
        {
            NifNativeHarness.Require(!plan.TryGetProperty("BridgePath", out _) && !plan.TryGetProperty("BridgeSha256", out _));
        }
        string output = Path.GetFullPath(arguments[3]);
        NifNativeHarness.Require(!Directory.Exists(output));
        Directory.CreateDirectory(output);
        string suffix = OperatingSystem.IsWindows() ? ".dll" : ".so";
        NifNativeHarness.Require(imagePath.EndsWith(suffix, StringComparison.Ordinal));
        string stem = imagePath[..^suffix.Length];
        using var compiled = GeneratedModuleCompiler.Compile(File.ReadAllText(sourcePath));
        await using var runtime = new ProcessRuntime();
        compiled.Definition.Register(runtime.Modules);
        string module = Text(selected, "Module");
        ModuleCodeImage ownedImage = runtime.Modules.CurrentCodeImage(module)
            ?? throw new InvalidOperationException(NifNativeHarnessNames.Broken);
        if (ordinaryHost)
        {
            // The developer driver still admits exact owned images before any native operation.
            // Linux needs its separately admitted runtime ABI bridge; no per-image runtime policy.
            if (OperatingSystem.IsLinux())
                NifNativeExportRegistration.EnsureRegistered(NifNativeCapabilityProfile.ForReviewedBasicAbi(
                    new NifNativeReviewedBasicAbi(
                        imagePath,
                        imageHash,
                        buildHash,
                        bridgePath,
                        bridgeHash
                    )
                ));
        }
        else
        {
            runtime.Modules.InstallReviewedNativeExecutionPolicy(path =>
            {
                NifNativeHarness.Require(path == imagePath);

                return new NifNativeReviewedBasicAbi(
                    imagePath,
                    imageHash,
                    buildHash,
                    bridgePath,
                    bridgeHash
                );
            });
        }

        Term? observed = null;
        try
        {
            await NifNativeHarness.Context(
                runtime,
                async context =>
            {
                await Stub(compiled.Definition, context);
                Term info = caseName == NifNativeElevenNames.LoadVeto ? Term.A(NifNativeElevenNames.Veto) : Term.I(NifNativeElevenNames.LoadInfo);
                Term result = await Invoke(
                    compiled.Definition,
                    context,
                    NifNativeElevenNames.Load,
                    [Term.String(stem), info]
                );
                File.WriteAllBytes(Path.Combine(output, NifNativeElevenNames.LoadResult), ExternalTermFormat.Encode(result));
                NifNativeLoadReasonChecks.AssertOriginal(caseName, result);
                Term body;
                if (Text(selected, "SourceExpected") == "success")
                {
                    NifNativeHarness.Require(result.Equals(Term.A(NifNativeElevenNames.Ok)));
                    Term payload = Term.Tuple(
                        Term.A(NifNativeElevenNames.Payload),
                        new MapTerm([new KeyValuePair<Term, Term>(
                            Term.A(NifNativeElevenNames.PayloadKey),
                            Term.List(Term.I(NifNativeElevenNames.PayloadInteger), new BitString(NifNativeElevenNames.PayloadBytes))
                        )])
                    );
                    NifNativeHarness.Require((await Invoke(
                        compiled.Definition,
                        context,
                        NifNativeElevenNames.Identity,
                        [payload]
                    )).Equals(payload));
                    Term marker = await Invoke(
                        compiled.Definition,
                        context,
                        NifNativeElevenNames.Marker,
                        []
                    );
                    Term third = await Invoke(
                        compiled.Definition,
                        context,
                        NifNativeElevenNames.Third,
                        []
                    );
                    Term expectedMarker = Term.I(caseName == NifNativeElevenNames.Alpha ? NifNativeElevenNames.AlphaMarker : caseName == NifNativeElevenNames.AbsentLoad ? NifNativeElevenNames.NoLoadMarker : NifNativeElevenNames.LoadInfo);
                    Term expectedThird = caseName == NifNativeElevenNames.Alpha ? Term.String(NifNativeElevenNames.AlphaThird) : Term.I(NifNativeElevenNames.BetaThird);
                    NifNativeHarness.Require(marker.Equals(expectedMarker) && third.Equals(expectedThird));
                    body = Term.Tuple(
                        payload,
                        marker,
                        third,
                        Term.A(NifNativeElevenNames.Untouched)
                    );
                }
                else
                {
                    NifNativeHarness.Require(result is TupleTerm { Items.Count: 2 } outer && outer.Items[0].Equals(Term.A(NifNativeElevenNames.Error)) && outer.Items[1] is TupleTerm { Items.Count: 2 } detail && detail.Items[0].Equals(Term.A(Text(selected, "SourceExpected"))));
                    await Stub(compiled.Definition, context);
                    body = Term.Tuple(Term.A(NifNativeElevenNames.StubAfterFailure), Term.A(NifNativeElevenNames.NotLoaded));
                }
                NifNativeHarness.Require((await Invoke(
                    compiled.Definition,
                    context,
                    NifNativeElevenNames.Ordinary,
                    []
                )).Equals(Term.A(NifNativeElevenNames.Untouched)));
                observed = Term.Tuple(
                    Term.A(NifNativeElevenNames.Result),
                    Term.A(caseName),
                    Term.A(module),
                    result,
                    body
                );
                File.WriteAllBytes(Path.Combine(output, NifNativeElevenNames.Raw), ExternalTermFormat.Encode(observed));
            }
            );
        }
        finally
        {
            await ownedImage.NifBindings.RetireAsync().WaitAsync(NifNativeHarnessNames.Deadline);
        }
        Verify(imagePath, imageHash);
        NifNativeHarness.Require(observed is not null);
        File.WriteAllText(
            Path.Combine(output, NifNativeElevenNames.Completion),
            JsonSerializer.Serialize(new
            {
                Name = caseName,
                Status = "OBSERVED_MANAGED_SOURCE_ROUTE_AND_RETIRED_IMAGE",
                NativeBuildReceiptSha256 = buildHash,
                AdmissionSha256 = arguments[1],
                OriginalComparisonExecuted = false,
                RuntimeImagePolicyInstalled = !ordinaryHost,
                OrdinaryHostFileLoading = ordinaryHost,
                AdmittedLinuxBridgeBootstrapped = ordinaryHost && OperatingSystem.IsLinux()
            })
        );
    }

    private static async Task Stub(ModuleDefinition module, ProcessContext context)
    {
        try
        {
            await Invoke(
                module,
                context,
                NifNativeElevenNames.Marker,
                []
            );
        }
        catch (ErlangException failure) when (failure.Reason.Equals(Term.A(NifNativeElevenNames.NotLoaded)))
        {
            return;
        }
        throw new InvalidOperationException(NifNativeHarnessNames.Broken);
    }

    private static ValueTask<Term> Invoke(
        ModuleDefinition module,
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments
    )
        => Execution.InvokeAsync(
            module,
            function,
            context,
            arguments
        );

    private static string Text(JsonElement row, string name) => row.GetProperty(name).GetString()
        ?? throw new InvalidOperationException(NifNativeHarnessNames.Broken);

    private static void Verify(string path, string expected)
    {
        NifNativeHarness.Require(Path.IsPathFullyQualified(path));
        using var stream = File.OpenRead(path);
        NifNativeHarness.Require(Convert.ToHexString(SHA256.HashData(stream)).Equals(expected, StringComparison.OrdinalIgnoreCase));
    }
}
