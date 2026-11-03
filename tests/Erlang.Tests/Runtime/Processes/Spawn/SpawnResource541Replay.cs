using System.Security.Cryptography;
using System.Text.Json;
using Erlang.Otp;
using static Erlang.Tests.SpawnResource541Names;

namespace Erlang.Tests;

internal static class SpawnResource541Replay
{
    internal static async Task Run(
        string repository,
        string admissionPath,
        string admissionSha,
        string metadataCompletion,
        string metadataSha,
        bool owned,
        string output,
        string backend = SpawnResource542Names.ManagedBackend
    )
    {
        Require(backend is SpawnResource542Names.ManagedBackend or SpawnResource542Names.OriginalBackend);
        repository = Path.GetFullPath(repository);
        Require(Digest(admissionPath) == admissionSha.ToUpperInvariant() && !Directory.Exists(output));
        OriginalMap517Admission receipt = JsonSerializer.Deserialize<OriginalMap517Admission>(File.ReadAllBytes(admissionPath))!;
        Require(receipt.PinnedCommit == Pin && receipt.Job == Job && receipt.OriginalAcquisitionPassed);
        foreach (var file in new[]
        {
            (receipt.SourcePath, receipt.SourceSha256), (receipt.BeamPath, receipt.BeamSha256),
            (receipt.WorkerBeamPath, receipt.WorkerBeamSha256), (receipt.RawPath, receipt.RawSha256),
            (receipt.DeterministicPath, receipt.DeterministicSha256), (receipt.CompletionPath, receipt.CompletionSha256),
            (receipt.InventoryPath, receipt.InventorySha256), (receipt.ProviderBeforePath, receipt.ProviderBeforeSha256),
            (receipt.ProviderAfterPath, receipt.ProviderAfterSha256)
        })
            Require(Digest(OriginalMap517AdmissionReader.Resolve(repository, file.Item1)) == file.Item2);
        Require(Digest(Path.Combine(repository, SourceFixture)) == receipt.SourceSha256);
        Term expectedWorkerWitness = SpawnWorker546Admission.Read(repository, admissionPath, receipt);
        Require(Digest(metadataCompletion) == metadataSha.ToUpperInvariant());
        using var metadataDocument = JsonDocument.Parse(File.ReadAllBytes(metadataCompletion));
        JsonElement metadata = metadataDocument.RootElement;
        Require(metadata.GetProperty("Pin").GetString() == Pin && metadata.GetProperty("OriginalOnly").GetBoolean()
            && metadata.GetProperty("AcquisitionPassed").GetBoolean());
        using var provider = JsonDocument.Parse(File.ReadAllBytes(OriginalMap517AdmissionReader.Resolve(repository, receipt.ProviderBeforePath)));
        Require(metadata.GetProperty("ProviderAdmissionSha256").GetString() == provider.RootElement.GetProperty("AdmissionSha256").GetString());
        string metadataDirectory = Path.GetDirectoryName(Path.GetFullPath(metadataCompletion))!;
        foreach (var row in metadata.GetProperty("Files").EnumerateArray())
        {
            string name = row.GetProperty("Path").GetString()!;
            Require(!Path.IsPathRooted(name));
            string file = Path.GetFullPath(Path.Combine(metadataDirectory, name));
            Require(file.StartsWith(
                metadataDirectory + Path.DirectorySeparatorChar,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal
            ));
            Require(File.Exists(file) && new FileInfo(file).Length == row.GetProperty("Bytes").GetInt64()
                && Digest(file) == row.GetProperty("Sha256").GetString());
        }
        JsonElement profile = metadata.GetProperty(DynamicApplyNativeMetadata527Names.Field);
        string metadataName = profile.GetProperty("Name").GetString()!;
        Require(Path.GetFileName(metadataName) == metadataName);
        byte[] nativeBytes = File.ReadAllBytes(Path.Combine(metadataDirectory, metadataName));
        RuntimeNativeMetadataImage native = DynamicApplyNativeMetadata527Admission.Verify(metadataDirectory, profile, nativeBytes);
        Require(native.OriginalBeamSha256 == profile.GetProperty("Sha256").GetString());
        byte[] subject = File.ReadAllBytes(OriginalMap517AdmissionReader.Resolve(repository, receipt.BeamPath));
        byte[] worker = File.ReadAllBytes(OriginalMap517AdmissionReader.Resolve(repository, receipt.WorkerBeamPath));
        (byte[] Bytes, string Path, string Sha256)? originalProc = backend == SpawnResource542Names.OriginalBackend
            ? SpawnResource542OriginalAdmission.Read(provider.RootElement) : null;
        await using var runtime = OwnedGuest506RuntimeFactory.Create(owned, native);
        PreparedBeamImage? originalImage = null;
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            async (registry, scope) =>
        {
            await runtime.InitializeModulesAsync(_ =>
            {
                OtpModules.Register(registry);
            });
            var selection = new BeamDependencySelection(new[] { Core, SpawnResource541Names.ProcLib, Subject, Worker });
            var images = new List<PreparedBeamImage>
                    {
                        scope.Prepare(() => registry.PrepareBeam(subject, selection)),
                        scope.Prepare(() => registry.PrepareBeam(worker, selection))
                    };
            if (originalProc is { } admitted)
            {
                originalImage = scope.Prepare(() => registry.PrepareBeam(admitted.Bytes,
                    new BeamDependencySelection(SpawnResource542Names.Imports)));
                Require(originalImage.Name == SpawnResource541Names.ProcLib && !originalImage.HasOnLoad
                    && originalImage.NativePrimitiveExports.Count == 0
                    && originalImage.OriginalBeamSha256 == admitted.Sha256);
                Require(DebugDirtyNative424Access.IsNativeEntry(
                    registry,
                    SpawnResource542Names.Lists,
                    SpawnResource542Names.Member,
                    2
                )
                    && DebugDirtyNative424Access.IsNativeEntry(
                        registry,
                        SpawnResource542Names.Lists,
                        SpawnResource542Names.KeyFind,
                        3
                    ));
                images.Add(originalImage);
            }

            return _ =>
            {
                Require(registry.PublishPreparedBeam(images.ToArray()).Status == BeamPublicationStatus.Published);
                RequireOriginalOwners(registry, originalImage);
            };
        }
        );
        Directory.CreateDirectory(output);
        if (originalProc is { } selected)
            await File.WriteAllTextAsync(
                Path.Combine(output, SpawnResource542Names.ProofFile),
                JsonSerializer.Serialize(new
                {
                    SelectedPath = selected.Path,
                    SelectedSha256 = selected.Sha256,
                    SourcePin = Pin,
                    ExplicitImports = SpawnResource542Names.Imports,
                    HasOnLoad = originalImage!.HasOnLoad,
                    NativePrimitiveCount = originalImage.NativePrimitiveExports.Count,
                    LoadedOriginalLibraryModules = new[] { SpawnResource541Names.ProcLib },
                    FullBootClaimed = false
                })
            );
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle controller = runtime.Spawn(async context =>
        {
            try
            {
                Term actual = await runtime.Modules.Call(
                    context,
                    Worker,
                    SpawnResource541Names.Run,
                    Term.A(Subject)
                );
                bool actualWitnessPresent = context.Dictionary.TryGetValue(Term.A(SpawnWorker546Names.Key), out Term? actualWorkerWitness);
                Term expected = OwnedFunctionOriginal407Terms.Decode(File.ReadAllBytes(
                    OriginalMap517AdmissionReader.Resolve(repository, receipt.RawPath)));
                Term minor = Term.Tuple(Term.A(Minor), Term.I(2));
                var raw = (BitString)await runtime.Modules.Call(
                    context,
                    Core,
                    Encode,
                    actual,
                    Term.List(minor)
                );
                var deterministic = (BitString)await runtime.Modules.Call(
                    context,
                    Core,
                    Encode,
                    actual,
                    Term.List(Term.A(Deterministic), minor)
                );
                if (actualWitnessPresent)
                {
                    var witnessBytes = (BitString)await runtime.Modules.Call(
                        context,
                        Core,
                        Encode,
                        actualWorkerWitness!,
                        Term.List(minor)
                    );
                    await File.WriteAllBytesAsync(Path.Combine(output, SpawnWorker546Names.WitnessFile), witnessBytes.ToArray());
                }
                await File.WriteAllBytesAsync(Path.Combine(output, RawFile), raw.ToArray());
                await File.WriteAllBytesAsync(Path.Combine(output, DeterministicFile), deterministic.ToArray());
                RequireOriginalOwners(runtime.Modules, originalImage);
                Require(runtime.Modules.SourceOwner(Worker, SpawnResource541Names.Run, 1) is PreparedBeamImage actualWorkerImage
                    && actualWorkerImage.OriginalBeamSha256 == receipt.WorkerBeamSha256);
                Require(actualWitnessPresent);
                SpawnWorker546FunctionScope.RequireExportFunctionsOnly(expected);
                SpawnWorker546FunctionScope.RequireExportFunctionsOnly(actual);
                SpawnWorker546WitnessLaw workerRoles = SpawnWorker546WitnessLaw.Bind(
                    expectedWorkerWitness,
                    actualWorkerWitness!,
                    expected,
                    actual,
                    context.Self
                );
                new SpawnResource541Comparison(workerRoles).Compare(expected, actual);
                await File.WriteAllTextAsync(
                    Path.Combine(output, "comparison.json"),
                    JsonSerializer.Serialize(new
                    {
                        Passed = true,
                        LibraryBackend = backend,
                        OriginalFixtureBackend = "AdmittedCompiledBEAM",
                        WholeProtocolIncludingStacks = true,
                        RawEtfRewritten = false,
                        IdentityLaw = "AuthenticatedWorkerCallerPidsAndSpawnReplyReferenceBijection",
                        WorkerWitnessAuthenticated = true,
                        PositiveScope = "315NamedTargetsExportFunOnly",
                        NewFunCreatorPidCompatibilityClaimed = false,
                        FullContractsClosed = 0
                    })
                );

                completed.TrySetResult();
            }
            catch (Exception error)
            {
                completed.TrySetException(error);
            }

            return Nil.Value;
        });
        await completed.Task.WaitAsync(Deadline);
        await controller.Completion.WaitAsync(Deadline);
    }

    private static void RequireOriginalOwners(ModuleRegistry registry, PreparedBeamImage? image)
    {
        if (image is null)
            return;
        foreach (var export in registry.Exports.Where(export => export.Module == SpawnResource541Names.ProcLib))
            Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
    }

    private static string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static void Require(bool value)
    {
        if (!value)
            throw new InvalidDataException(Failure);
    }
}
