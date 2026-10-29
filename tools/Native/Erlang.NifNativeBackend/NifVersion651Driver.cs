using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.NifNativeBackend;

internal static class NifVersion651Driver
{
    internal static async Task Run(string[] arguments)
    {
        NifNativeHarness.Require((OperatingSystem.IsWindows() || OperatingSystem.IsLinux()) && arguments.Length == 3);
        bool linux = OperatingSystem.IsLinux();
        string admissionPath = Path.GetFullPath(arguments[0]);
        Verify(admissionPath, arguments[1]);
        using var document = JsonDocument.Parse(File.ReadAllBytes(admissionPath));
        JsonElement plan = document.RootElement;
        string status = Text(plan, NifVersion651DriverNames.StatusField);
        bool module690 = status == NifModuleText690Names.Admission;
        NifNativeHarness.Require(module690 || status == NifVersion651DriverNames.Admission);
        string subject = module690 ? Module690(Text(plan, NifModuleText690Names.CaseField)) : NifVersion651DriverNames.Module;
        string observer = module690 ? NifModuleText690Names.Observer : NifVersion651DriverNames.Observer;
        if (module690)
            NifNativeHarness.Require(Text(plan, NifModuleText690Names.CallingModuleField) == subject
                && Text(plan, NifVersion651DriverNames.IdField) == NifModuleText690Names.NativeCase);
        NifNativeHarness.Require(Text(plan, NifVersion651DriverNames.PinField) == NifVersion651DriverNames.Pin);
        string route = Text(plan, NifVersion651DriverNames.RouteField);
        NifNativeHarness.Require(route is NifVersion651DriverNames.Reviewed or NifVersion651DriverNames.Ordinary);
        string library = Text(plan, NifVersion651DriverNames.LibraryPathField);
        string libraryHash = Text(plan, NifVersion651DriverNames.LibrarySha256Field);
        string suffix = linux ? NifVersion651DriverNames.LinuxSuffix : NifVersion651DriverNames.NativeSuffix;
        NifNativeHarness.Require(library.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        string build = Text(plan, NifVersion651DriverNames.BuildReceiptPathField);
        string buildHash = Text(plan, NifVersion651DriverNames.BuildReceiptSha256Field);
        Verify(library, libraryHash);
        Verify(build, buildHash);
        using var buildDocument = JsonDocument.Parse(File.ReadAllBytes(build));
        NifNativeHarness.Require(Text(buildDocument.RootElement, NifVersion651DriverNames.StatusField) == (linux ? NifVersion651DriverNames.LinuxBuildStatus : NifVersion651DriverNames.BuildStatus));
        JsonElement image = buildDocument.RootElement.GetProperty(NifVersion651DriverNames.ImagesField).EnumerateArray()
            .Single(row => Text(row, NifVersion651DriverNames.IdField) == Text(plan, NifVersion651DriverNames.IdField));
        NifNativeHarness.Require(Text(image.GetProperty(NifVersion651DriverNames.LibraryField), NifVersion651DriverNames.Sha256Field) == libraryHash);
        NifNativeHarness.Require(Text(image.GetProperty(NifVersion651DriverNames.LibraryField), NifVersion651DriverNames.PathField) == library);
        string? bridgePath = null;
        string? bridgeHash = null;
        string? bridgeReceipt = null;
        string? bridgeReceiptHash = null;
        if (linux)
        {
            bridgePath = Text(plan, NifVersion651DriverNames.BridgePathField);
            bridgeHash = Text(plan, NifVersion651DriverNames.BridgeSha256Field);
            bridgeReceipt = Text(plan, NifVersion651DriverNames.BridgeBuildReceiptPathField);
            bridgeReceiptHash = Text(plan, NifVersion651DriverNames.BridgeBuildReceiptSha256Field);
            Verify(bridgePath, bridgeHash);
            Verify(bridgeReceipt, bridgeReceiptHash);
            JsonElement bridge = buildDocument.RootElement.GetProperty(NifVersion651DriverNames.BridgeField);
            NifNativeHarness.Require(Text(bridge.GetProperty(NifVersion651DriverNames.LibraryField), NifVersion651DriverNames.PathField) == bridgePath);
            NifNativeHarness.Require(Text(bridge.GetProperty(NifVersion651DriverNames.LibraryField), NifVersion651DriverNames.Sha256Field) == bridgeHash);
            NifNativeHarness.Require(Text(bridge.GetProperty(NifVersion651DriverNames.BuildReceiptField), NifVersion651DriverNames.PathField) == bridgeReceipt);
            NifNativeHarness.Require(Text(bridge.GetProperty(NifVersion651DriverNames.BuildReceiptField), NifVersion651DriverNames.Sha256Field) == bridgeReceiptHash);
            using var bridgeDocument = JsonDocument.Parse(File.ReadAllBytes(bridgeReceipt));
            NifNativeHarness.Require(Text(bridgeDocument.RootElement, NifVersion651DriverNames.StatusField) == NifVersion651DriverNames.LinuxBridgeBuildStatus);
            NifNativeHarness.Require(Text(bridgeDocument.RootElement, NifVersion651DriverNames.PinField) == NifVersion651DriverNames.Pin);
            NifNativeHarness.Require(Text(bridgeDocument.RootElement, NifVersion651DriverNames.BridgePathField) == bridgePath);
            NifNativeHarness.Require(Text(bridgeDocument.RootElement, NifVersion651DriverNames.BridgeSha256Field) == bridgeHash);
        }
        JsonElement[] beams = plan.GetProperty(NifVersion651DriverNames.BeamsField).EnumerateArray().ToArray();
        NifNativeHarness.Require(beams.Length == 2);
        foreach (JsonElement beam in beams)
            Verify(Text(beam, NifVersion651DriverNames.PathField), Text(beam, NifVersion651DriverNames.Sha256Field));
        string output = Path.GetFullPath(arguments[2]);
        NifNativeHarness.Require(!Directory.Exists(output));
        Directory.CreateDirectory(output);
        var errors = new List<string>();
        ProcessRuntime? runtime = null;
        ModuleCodeImage? imageOwner = null;
        bool observed = false;
        bool imageRetired = false;
        bool runtimeClosed = false;
        try
        {
            runtime = new ProcessRuntime();
            ProcessRuntime owner = runtime;
            await owner.InitializeModulesAsync(registry =>
            {
                var dependencies = new BeamDependencySelection([subject, observer]);
                var prepared = beams.Select(beam => registry.PrepareBeam(File.ReadAllBytes(Text(beam, NifVersion651DriverNames.PathField)), dependencies)).ToArray();
                NifNativeHarness.Require(prepared.Select(image => image.Name).ToHashSet(StringComparer.Ordinal).SetEquals([subject, observer]));
                NifNativeHarness.Require(registry.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published);
                foreach (var preparedImage in prepared)
                    foreach (var export in registry.Exports.Where(export => export.Module == preparedImage.Name))
                        NifNativeHarness.Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), preparedImage));
            });
            imageOwner = owner.Modules.CurrentCodeImage(subject)
                ?? throw new InvalidOperationException(NifNativeHarnessNames.Broken);
            if (route == NifVersion651DriverNames.Reviewed)
                owner.Modules.InstallReviewedNativeExecutionPolicy(path =>
                {
                    NifNativeHarness.Require(path == library);

                    return new NifNativeReviewedBasicAbi(
                        library,
                        libraryHash,
                        buildHash,
                        bridgePath,
                        bridgeHash
                    );
                });
            else if (linux)
                NifNativeExportRegistration.EnsureRegistered(NifNativeCapabilityProfile.ForReviewedBasicAbi(
                    new NifNativeReviewedBasicAbi(
                        library,
                        libraryHash,
                        buildHash,
                        bridgePath,
                        bridgeHash
                    )
                ));
            await NifNativeHarness.Context(
                owner,
                async context =>
            {
                Term result = module690
                    ? await owner.Modules.Call(
                        context,
                        observer,
                        NifModuleText690Names.Capture,
                        Term.A(subject),
                        Term.String(library[..^suffix.Length])
                    )
                    : await owner.Modules.Call(
                        context,
                        observer,
                        NifVersion651DriverNames.Entry,
                        Term.String(library[..^suffix.Length]),
                        context.Self
                    );
                File.WriteAllBytes(Path.Combine(output, NifVersion651DriverNames.Observed), ExternalTermFormat.Encode(result));
                observed = true;
            }
            );
        }
        catch (Exception error)
        {
            errors.Add(error.ToString());
        }
        finally
        {
            if (imageOwner is not null)
                try
                {
                    await imageOwner.NifBindings.RetireAsync().WaitAsync(NifNativeHarnessNames.Deadline);
                    imageRetired = true;
                }
                catch (Exception error)
                {
                    errors.Add(error.ToString());
                }
            if (runtime is not null)
                try
                {
                    await runtime.DisposeAsync();
                    runtimeClosed = true;
                }
                catch (Exception error)
                {
                    errors.Add(error.ToString());
                }
            try
            {
                Verify(admissionPath, arguments[1]);
                Verify(library, libraryHash);
                Verify(build, buildHash);
                if (linux)
                {
                    Verify(bridgePath!, bridgeHash!);
                    Verify(bridgeReceipt!, bridgeReceiptHash!);
                }
                foreach (JsonElement beam in beams)
                    Verify(Text(beam, NifVersion651DriverNames.PathField), Text(beam, NifVersion651DriverNames.Sha256Field));
            }
            catch (Exception error)
            {
                errors.Add(error.ToString());
            }
        }
        File.WriteAllText(
            Path.Combine(output, NifVersion651DriverNames.Completion),
            JsonSerializer.Serialize(new
            {
                Passed = observed && imageRetired && runtimeClosed && errors.Count == 0,
                Route = route,
                ModuleText690 = module690,
                CallingModule = subject,
                OriginalBeamExecution = true,
                WholeRawOutcomeCaptured = observed,
                ImageRetired = imageRetired,
                RuntimeClosed = runtimeClosed,
                Errors = errors,
                FrameProjection = false,
                ApiDelta = 0,
                FullContractDelta = 0
            })
        );
        NifNativeHarness.Require(observed && imageRetired && runtimeClosed && errors.Count == 0);
    }

    private static string Module690(string name) => name switch
    {
        NifModuleText690Names.AsciiCase => NifModuleText690Names.AsciiModule,
        NifModuleText690Names.LatinLowerCase => NifModuleText690Names.LatinLowerModule,
        NifModuleText690Names.LatinQuotedCase => NifModuleText690Names.LatinQuotedModule,
        NifModuleText690Names.LatinDistinctCase => NifModuleText690Names.LatinDistinctModule,
        _ => throw new InvalidOperationException(NifNativeHarnessNames.Broken)
    };

    private static string Text(JsonElement value, string name) => value.GetProperty(name).GetString()
        ?? throw new InvalidOperationException(NifNativeHarnessNames.Broken);

    private static void Verify(string path, string hash)
    {
        NifNativeHarness.Require(Path.IsPathFullyQualified(path));
        NifNativeHarness.Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).Equals(hash, StringComparison.OrdinalIgnoreCase));
    }
}
