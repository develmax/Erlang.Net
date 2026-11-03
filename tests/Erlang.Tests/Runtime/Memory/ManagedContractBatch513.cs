using static Erlang.Tests.ManagedContractBatch513Names;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Erlang.Differential;

namespace Erlang.Tests;

// Capture driver, not a test whose successful return promotes compatibility.
internal static class ManagedContractBatch513
{
    internal static async Task CaptureAsync(
        string repository,
        string planPath,
        string planSha256,
        string admissionPath,
        string admissionSha256,
        string output,
        string? exactJob = null,
        bool? exactOwned = null
    )
    {
        if (Directory.Exists(output) || Digest(planPath) != planSha256 || Digest(admissionPath) != admissionSha256)
            throw new InvalidDataException(AdmissionRequired);
        using var plan = JsonDocument.Parse(File.ReadAllBytes(planPath));
        foreach (var source in plan.RootElement.GetProperty(SourcesProperty).EnumerateArray())
            if (Digest(Path.Combine(repository, source.GetProperty(PathProperty).GetString()!)) != source.GetProperty(Sha256Property).GetString())
                throw new InvalidDataException(SourceChanged);
        var jobs = JsonSerializer.Deserialize<ManagedBatchJob513[]>(plan.RootElement.GetProperty(JobsProperty).GetRawText())!;
        var libraries = JsonSerializer.Deserialize<ManagedBatchImage513[]>(plan.RootElement.GetProperty(LibrariesProperty).GetRawText())!
            .ToDictionary(image => image.Module, StringComparer.Ordinal);
        if (jobs.Length != ExpectedJobs || jobs.Select(job => job.Job).Distinct(StringComparer.Ordinal).Count() != jobs.Length)
            throw new InvalidDataException(InventoryRequired);
        Directory.CreateDirectory(output);
        var receipts = new List<object>();
        if (exactJob is not null && !jobs.Any(job => job.Job == exactJob))
            throw new ArgumentException(UnknownJob);
        foreach (var job in jobs.Where(job => exactJob is null || job.Job == exactJob))
            foreach (bool owned in new[] { false, true }.Where(owned => exactOwned is null || owned == exactOwned))
            {
                string directory = Path.Combine(output, job.Job, owned ? OwnedProfile : OrdinaryProfile);
                Directory.CreateDirectory(directory);
                try
                {
                    await Capture(job, owned, directory);
                    receipts.Add(new
                    {
                        job.Job,
                        Owned = owned,
                        Captured = true,
                        ManagedOriginalWholeObserverComparisonPerformed = job.Paired335,
                        FullContractCompatibilityClaimed = false,
                        Artifacts = CaptureArtifacts(directory),
                        Comparison = job.Paired335 ? StrictPairStatus : PendingProtocolStatus
                    });
                }
                catch (Exception error)
                {
                    try
                    {
                        await File.WriteAllTextAsync(Path.Combine(directory, FailureTextFile), error.ToString());
                    }
                    catch (Exception diagnostic)
                    {
                        throw new AggregateException(error, diagnostic);
                    }
                    receipts.Add(new { job.Job, Owned = owned, Captured = false, Error = error.ToString(), Artifacts = CaptureArtifacts(directory) });
                }
            }
        await File.WriteAllTextAsync(
            Path.Combine(output, BatchFile),
            JsonSerializer.Serialize(
                new
                {
                    PlanSha256 = planSha256,
                    AdmissionSha256 = admissionSha256,
                    Runs = receipts,
                    CaptureOnly = true,
                    CompatibilityPassed = false,
                    WholeContractsClosed = 0,
                    DynamicProtocolsPending = true,
                    RawEtfRewritten = false,
                    Isolation = exactJob is not null && exactOwned is not null ? IsolatedChildStatus : SharedHostStatus
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );

        async Task Capture(ManagedBatchJob513 job, bool owned, string directory)
        {
            Stage(directory, StageInputs);
            if (!job.Paired335 && (job.WorkerImage is null || job.OriginalWorkerBaseline != WorkerBaselineCollected))
                throw new InvalidOperationException(WorkerBaselineRequired);
            if (job.RequiresOriginalKernelBoot && !OperatingSystem.IsWindows())
                throw new InvalidOperationException(LinuxBootAdmissionRequired);
            var imageRows = job.LibraryModules.Select(module => libraries[module]).Concat(job.FixtureImages)
                .Concat(job.WorkerImage is { } worker ? new[] { worker } : []).ToArray();
            foreach (var image in imageRows)
                _ = Read(image);
            // The boot fixture selects the original library catalog for later
            // code_server loads as well as the eagerly prepared image subset.
            // Keep that explicit caller authority when replacing boot images.
            byte[] selectedCatalog = job.RequiresOriginalKernelBoot
                ? StandardBootOriginal412Access.Resource(StandardBootOriginal412Names.Dependencies)
                : [];
            string[] selectedLibraries = job.RequiresOriginalKernelBoot
                ? JsonSerializer.Deserialize<string[]>(selectedCatalog)!
                : [];
            string[] dependencies = imageRows.Select(row => row.Module)
                .Concat(job.RuntimeRequirements.Select(row => row.Module))
                .Concat(selectedLibraries).Distinct(StringComparer.Ordinal).ToArray();
            var generated = new List<GeneratedModuleArtifact>();
            ProcessRuntime? runtime = null;
            Exception? primaryFailure = null;
            try
            {
                Stage(directory, StageRuntime);
                runtime = job.RequiresOriginalKernelBoot
                    ? await StandardBootOriginal412Runtime.Create(
                        admissionPath,
                        true,
                        true,
                        owned,
                        (owner, registry) =>
                        {
                            ManagedBatchImage513 stockEts = imageRows.Single(image => image.Module == ManagedStockEts520Names.Module);

                            return ManagedStockEts520Preparation.Prepare(
                                owner,
                                registry,
                                stockEts,
                                Read(stockEts),
                                dependencies
                            );
                        }
                    )
                    : OwnedGuest506RuntimeFactory.Create(owned);
                Stage(directory, StageRuntimeReady);
                string[] sourceSubjects = job.SourceSubjects ?? [];
                foreach (string module in sourceSubjects)
                {
                    if (module is not (GuestSubject or ProbeSubject))
                        throw new InvalidDataException(SourceSubjectAdmissionRequired);
                    string source = Path.Combine(repository, FixtureDirectory, module + SourceExtension);
                    var artifact = GeneratedModuleCompiler.Compile(File.ReadAllText(source), module + SourceExtension);
                    generated.Add(artifact);
                    artifact.Definition.Register(runtime.Modules);
                }
                Stage(directory, StageImages);
                var publication = new List<PreparedBeamImage>();
                await runtime.InitializeModulesAsync(registry =>
                {

                    var preparationFailures = new List<Exception>();
                    var preparationDiagnostics = new List<object>();
                    foreach (var image in imageRows.Where(row => !sourceSubjects.Contains(row.Module, StringComparer.Ordinal)))
                    {
                        try
                        {
                            var prepared = registry.PrepareBeam(Read(image), new BeamDependencySelection(dependencies));
                            object? existing = registry.SourceOwner(image.Module, ModuleInfoFunction, 0);
                            if (existing is PreparedBeamImage original)
                            {
                                if (original.OriginalBeamSha256 != image.Sha256
                                    || !original.NativeMd5.Equals(prepared.NativeMd5)
                                    || !original.Attributes.Equals(prepared.Attributes)
                                    || !original.Compilation.Equals(prepared.Compilation)
                                    || original.AttributeProvenance != prepared.AttributeProvenance
                                    || original.CompilationProvenance != prepared.CompilationProvenance)
                                    throw new InvalidOperationException(BootImageMismatch + image.Module);
                                continue;
                            }
                            publication.Add(prepared);
                        }
                        catch (Exception error)
                        {
                            object? native = error.Data[NativePrimitiveSignatureKey] is IReadOnlyList<(string Module, string Name, int Arity)> signatures
                                ? signatures.Select(signature => new
                                {
                                    signature.Module,
                                    Function = signature.Name,
                                    signature.Arity
                                }).ToArray()
                                : null;
                            preparationDiagnostics.Add(new
                            {
                                image.Module,
                                image.Path,
                                image.Sha256,
                                NativePrimitives = native,
                                Error = error.ToString()
                            });
                            preparationFailures.Add(new InvalidOperationException(PrepareImageFailure + image.Module, error));
                        }
                    }
                    if (preparationFailures.Count != 0)
                    {
                        File.WriteAllText(
                            Path.Combine(directory, PrepareFailuresFile),
                            JsonSerializer.Serialize(
                            preparationDiagnostics, new JsonSerializerOptions { WriteIndented = true })
                        );

                        throw new AggregateException(PrepareBatchFailure, preparationFailures);
                    }
                    if (publication.Any(image => image.HasOnLoad) && job.LibraryOnLoadOrder is null)
                        throw new InvalidOperationException(OnLoadOrderRequired);
                    string[] onLoadOrder = job.LibraryOnLoadOrder ?? [];
                    if (onLoadOrder.Distinct(StringComparer.Ordinal).Count() != onLoadOrder.Length
                        || !publication.Where(image => image.HasOnLoad).Select(image => image.Name)
                            .ToHashSet(StringComparer.Ordinal).SetEquals(onLoadOrder))
                        throw new InvalidOperationException(OnLoadOrderRequired);
                    var ordinary = publication.Where(image => !image.HasOnLoad).ToArray();
                    if (registry.PublishPreparedBeam(ordinary).Status != BeamPublicationStatus.Published)
                        throw new InvalidOperationException(PublicationFailed);
                    var missingRuntime = job.RuntimeRequirements.Where(requirement =>
                        !registry.HasExport(requirement.Module, requirement.Function, requirement.Arity)
                        || requirement.Kind == NativePrimitiveKind && !(bool)typeof(ModuleRegistry)
                            .GetMethod(NativeEntryMethod, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                            .Invoke(registry, [requirement.Module, requirement.Function, requirement.Arity])!).ToArray();
                    if (missingRuntime.Length != 0)
                    {
                        File.WriteAllText(
                            Path.Combine(directory, RuntimeRequirementsFile),
                            JsonSerializer.Serialize(
                            missingRuntime, new JsonSerializerOptions { WriteIndented = true })
                        );

                        throw new InvalidOperationException(RuntimeEntryMissing + JsonSerializer.Serialize(missingRuntime));
                    }
                });
                Stage(directory, StagePublished);
                var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                ProcessRuntime current = runtime;
                ProcessHandle controller = runtime.Spawn(async context =>
                {
                    try
                    {
                        if (job.RequiresOriginalKernelBoot)
                        {
                            using var admission = JsonDocument.Parse(File.ReadAllBytes(admissionPath));
                            string installed = admission.RootElement.GetProperty(InstalledRootProperty).GetString()!;
                            _ = await current.Modules.Call(context, PrimFileModule, OnLoadFunction);
                            string[] arguments = [RootOption, installed, BindirOption, Path.Combine(installed, ErtsBin),
                                BootOption, Path.Combine(installed, BootFile), ModeOption, Interactive, NoShellOption, NoInputOption];
                            using var deadline = new CancellationTokenSource(BootDeadline);
                            Stage(directory, StageBoot);
                            _ = await current.StartOtpBootAsync(arguments.Select(value => new BitString(Encoding.UTF8.GetBytes(value))), deadline.Token);
                            Stage(directory, StageBootReady);
                            foreach (string name in new[] { InitializationName, ApplicationControllerName, CodeServerName, LoggerName })
                                if (await current.Modules.Call(
                                    context,
                                    CoreErlangModule,
                                    WhereisFunction,
                                    Term.A(name)
                                ) is not Pid)
                                    throw new InvalidOperationException(KernelWitnessMissing + name);
                        }
                        foreach (string module in job.LibraryOnLoadOrder ?? [])
                        {
                            var image = publication.Single(image => image.Name == module && image.HasOnLoad);
                            Stage(directory, StageOnLoad);
                            BeamOnLoadResult result = await current.Modules.LoadPreparedOnLoadAsync(current, image);
                            Term failure = result.Outcome.Failure switch
                            {
                                ErlangException guest => Term.Tuple(Term.A(guest.ExceptionClass), guest.Reason, guest.StackTraceTerm),
                                Exception host => new BitString(Encoding.UTF8.GetBytes(host.ToString())),
                                _ => Term.A(ManagedOnLoad520Names.Undefined)
                            };
                            await Write(
                                ManagedOnLoad520Names.OutcomePrefix + module + ManagedOnLoad520Names.EtfExtension,
                                Term.Tuple(
                                    Term.A(module),
                                    result.Callback,
                                    Term.A(result.Published ? ManagedOnLoad520Names.True : ManagedOnLoad520Names.False),
                                    result.Outcome.Returned ?? Term.A(ManagedOnLoad520Names.Undefined),
                                    result.DownReason,
                                    failure
                                ),
                                false
                            );
                            if (!result.Published)
                                throw new InvalidOperationException(OnLoadFailed + module, result.Outcome.Failure);
                        }
                        if (publication.Any(image => image.HasOnLoad && !image.IsConsumed))
                            throw new InvalidOperationException(OnLoadNotPublished);
                        foreach (var image in imageRows.Where(row => !sourceSubjects.Contains(row.Module, StringComparer.Ordinal)))
                            foreach (var export in current.Modules.Exports.Where(export => export.Module == image.Module))
                                if (current.Modules.SourceOwner(export.Module, export.Function, export.Arity) is not PreparedBeamImage)
                                    throw new InvalidOperationException(ManagedFallbackRejected + export.Module);
                        await ManagedCaptureAuthority520.WriteAsync(
                            directory,
                            ManagedCaptureAuthority520Names.Before,
                            selectedCatalog,
                            selectedLibraries,
                            current.Modules,
                            imageRows
                        );
                        await Write(OwnerBeforeFile, ActualOwners(), false);
                        Term observed = job.Paired335
                            ? await current.Modules.Call(context, job.Module, PairedEntry)
                            : await current.Modules.Call(
                                context,
                                WorkerModule,
                                WorkerEntry,
                                Term.A(job.Module)
                            );
                        await Write(RawFile, observed, false);
                        await Write(DeterministicFile, observed, true);
                        await Write(OwnerAfterFile, ActualOwners(), false);
                        await ManagedCaptureAuthority520.WriteAsync(
                            directory,
                            ManagedCaptureAuthority520Names.After,
                            selectedCatalog,
                            selectedLibraries,
                            current.Modules,
                            imageRows
                        );
                        bool returned = observed is TupleTerm { Items: [TupleTerm { Items: [Atom { Name: ReturnedTag }, _] }, Atom { Name: NormalTag }] };
                        await ManagedCaptureEvidence520.RequireReturnedAsync(
                            async () =>
                            {
                                Term key = Term.A(job.Paired335 ? PairedWitnessKey : WorkerWitnessKey);
                                await Write(WitnessFile, context.Dictionary[key], false);
                            },
                            returned
                        );
                        completed.TrySetResult();
                    }
                    catch (Exception error)
                    {
                        Exception failure = error;
                        try
                        {
                            if (error is ErlangException guest)
                                await Write(FailureEtfFile, Term.Tuple(Term.A(guest.ExceptionClass), guest.Reason, guest.StackTraceTerm), false);
                        }
                        catch (Exception diagnostic)
                        {
                            failure = new AggregateException(error, diagnostic);
                        }
                        completed.TrySetException(failure);
                    }

                    return Nil.Value;

                    async Task Write(string name, Term value, bool deterministic)
                    {
                        Term minor = Term.Tuple(Term.A(MinorVersionOption), Term.I(SerializationMinorVersion));
                        var options = deterministic ? Term.List(Term.A(DeterministicOption), minor) : Term.List(minor);
                        var binary = (BitString)await current.Modules.Call(
                            context,
                            CoreErlangModule,
                            EncodeFunction,
                            value,
                            options
                        );
                        await File.WriteAllBytesAsync(Path.Combine(directory, name), binary.ToArray());
                    }

                    Term ActualOwners()
                    {
                        return Term.List(imageRows.Where(image => !sourceSubjects.Contains(image.Module, StringComparer.Ordinal)).Select(image =>
                        {
                            if (current.Modules.SourceOwner(image.Module, ModuleInfoFunction, 0) is not PreparedBeamImage owner
                                || owner.OriginalBeamSha256 != image.Sha256)
                                throw new InvalidOperationException(ActualOwnerMismatch + image.Module);
                            var exports = current.Modules.Exports.Where(export => export.Module == image.Module).ToArray();
                            if (exports.Any(export => !ReferenceEquals(current.Modules.SourceOwner(export.Module, export.Function, export.Arity), owner)))
                                throw new InvalidOperationException(ActualOwnerMismatch + image.Module);

                            return Term.Tuple(
                                Term.A(image.Module),
                                new BitString(Convert.FromHexString(owner.OriginalBeamSha256)),
                                owner.NativeMd5,
                                owner.Compilation,
                                owner.Attributes,
                                Term.List(exports.Select(export => Term.Tuple(Term.A(export.Function), Term.I(export.Arity))).ToArray())
                            );
                        }).ToArray());
                    }
                });
                await completed.Task.WaitAsync(CaptureDeadline);
                await controller.Completion.WaitAsync(ControllerJoinDeadline);
            }
            catch (Exception error)
            {
                primaryFailure = error;
                throw;
            }
            finally
            {
                await ManagedCaptureCleanup520.DisposeAsync(
                    primaryFailure,
                    runtime is null ? null : async () => await runtime.DisposeAsync(),
                    generated.Select(artifact => (Action)artifact.Dispose),
                    () => Stage(directory, StageRetirement),
                    success => Stage(directory, success ? StageRetired : ManagedCaptureTrace520Names.RetirementFailed)
                );
            }
            if (job.Paired335)
                foreach (string name in new[] { RawFile, DeterministicFile })
                    if (!File.ReadAllBytes(Path.Combine(directory, name)).AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(repository,
                        GoldenDirectory, name))))
                        throw new InvalidOperationException(PairMismatch + name);
        }

        byte[] Read(ManagedBatchImage513 image)
        {
            string path = Path.GetFullPath(Path.Combine(repository, image.Path));
            byte[] raw = File.ReadAllBytes(path);
            if (Convert.ToHexString(SHA256.HashData(raw)) != image.Sha256)
                throw new InvalidDataException(ImageChanged + image.Module);

            return raw;
        }
    }

    private static void Stage(string directory, string phase)
    {
        if (Environment.GetEnvironmentVariable(DiagnosticEnvironment) == DiagnosticEnabled)
            File.AppendAllLines(Path.Combine(directory, StageFile), [phase]);
    }

    private static string Digest(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static object[] CaptureArtifacts(string directory) => Directory.GetFiles(directory)
        .OrderBy(path => path, StringComparer.Ordinal)
        .Select(path => (object)new { Name = Path.GetFileName(path), Bytes = new FileInfo(path).Length, Sha256 = Digest(path) }).ToArray();
}
