using System.Reflection;
using System.Text.Json;
using static Erlang.Tests.OriginalReference563Names;

namespace Erlang.Tests;

internal static partial class OriginalReference563Replay
{
    private static async Task RunAdmittedCounters610(
        string repository,
        OriginalReference563Admission receipt,
        string metadataCompletion,
        string metadataSha,
        bool owned,
        string output,
        OriginalAtomics588Admission counters
    )
    {
        output = System.IO.Path.GetFullPath(output);
        if (Directory.Exists(output))
            throw new InvalidDataException(Failure);
        RuntimeNativeMetadataImage metadata = OriginalFloat556Metadata.ReadProvider(
            repository,
            receipt.ProviderBeforePath,
            metadataCompletion,
            metadataSha
        );
        string subjectName = OriginalCounters610Names.Subject;
        Directory.CreateDirectory(output);
        var expectedProfile = OriginalCounters610Protocol.Read(ExternalTermFormat.Decode(
            File.ReadAllBytes(OriginalReference563AdmissionReader.Resolve(repository, receipt.RawPath)))).Profile;
        var expectedDeterministic = OriginalCounters610Protocol.Read(ExternalTermFormat.Decode(
            File.ReadAllBytes(OriginalReference563AdmissionReader.Resolve(repository, receipt.DeterministicPath)))).Profile;
        Require(expectedProfile == expectedDeterministic);
        OriginalCounters610NegativeControls.Check(File.ReadAllBytes(OriginalReference563AdmissionReader.Resolve(repository, receipt.RawPath)));
        OriginalCounters610NegativeControls.Check(File.ReadAllBytes(OriginalReference563AdmissionReader.Resolve(repository, receipt.DeterministicPath)));
        var runtime = OriginalCounters610Runtime.Create(owned, metadata, expectedProfile);
        Exception? primary588 = null;
        try
        {
            bool actualOwned = (bool)typeof(ProcessRuntime).GetProperty(
                OwnedGuest506Names.PrivateOption,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!.GetValue(runtime)!;
            Require(actualOwned == owned);
            await DebugSame420HostAccess.Initialize(runtime);
            await runtime.InitializeModulesAsync(registry => RuntimeBootstrapDebugSharing425.Register(runtime, registry));
            using PreparedBeamImage nativeCounters = CountersOriginal421Preparation.Prepare(runtime, runtime.Modules);
            await runtime.InitializeModulesAsync(registry =>
            {
                Require(registry.PublishPreparedBeam(nativeCounters).Status == BeamPublicationStatus.Published);
                // This existing source-authenticated five-clause bridge is not
                // whole-module erts_internal admission. Install the independent
                // atomics constructor after publishing its module image.
                RuntimeBootstrapAtomics.Register(runtime, registry);
            });
            PreparedBeamImage[] stockImages = OriginalAtomics588Images.Prepare(runtime, repository, counters);
            PreparedBeamImage subject = runtime.Modules.PrepareBeam(
                File.ReadAllBytes(OriginalReference563AdmissionReader.Resolve(repository, receipt.BeamPath)),
                new BeamDependencySelection(DynamicApplyBoundary520Imports.Read(
                    File.ReadAllBytes(OriginalReference563AdmissionReader.Resolve(repository, receipt.BeamPath))))
            );
            PreparedBeamImage worker = runtime.Modules.PrepareBeam(
                File.ReadAllBytes(OriginalReference563AdmissionReader.Resolve(repository, receipt.WorkerBeamPath)),
                new BeamDependencySelection(new[] { Core, subjectName })
            );
            Require(subject.Name == subjectName && worker.Name == Worker
                && !subject.HasOnLoad && !worker.HasOnLoad
                && subject.NativePrimitiveExports.Count == 0 && worker.NativePrimitiveExports.Count == 0
                && subject.OriginalBeamSha256 == receipt.BeamSha256
                && worker.OriginalBeamSha256 == receipt.WorkerBeamSha256);
            OriginalAtomics588Images.RequireWorkers(repository, counters, [subject, worker]);
            Require(runtime.Modules.PublishPreparedBeam([.. stockImages, subject, worker]).Status == BeamPublicationStatus.Published);
            var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ProcessHandle controller = runtime.Spawn(async context =>
            {
                IDisposable? returnedHostOwnership610 = null;
                try
                {
                    Require(ReferenceEquals(runtime.Modules.SourceOwner(subjectName, OriginalReference563Names.Run, 0), subject)
                        && ReferenceEquals(runtime.Modules.SourceOwner(Worker, OriginalReference563Names.Run, 1), worker)
                        && ReferenceEquals(runtime.Modules.SourceOwner(Worker, WorkerFunction, WorkerArity), worker));
                    OriginalAtomics588Images.RequireOwners(
                        runtime,
                        stockImages,
                        repository,
                        counters
                    );
                    await OriginalAtomics588Images.CaptureOwners(
                        context,
                        runtime,
                        stockImages,
                        output,
                        OriginalAtomics588MetadataNames.BeforePhase
                    );
                    await OriginalCounters610Runtime.RequireProfile(context, expectedProfile);
                    object[] nativeFive = OriginalCounters610Runtime.RequireNativeFive(runtime);
                    foreach (var signature in OriginalCounters610Names.NativeFive)
                        Require(ReferenceEquals(runtime.Modules.SourceOwner(OriginalCounters610Fields.ErtsInternal,
                            signature.Item1, signature.Item2), nativeCounters));
                    foreach (var export in OriginalCounters610Names.PublicSix)
                        Require(ReferenceEquals(
                            runtime.Modules.SourceOwner(OriginalCounters610Fields.Counters, export.Item1, export.Item2),
                            stockImages.Single(image => image.Name == OriginalCounters610Fields.Counters)
                        ));
                    Term actual = await runtime.Modules.Call(
                        context,
                        Worker,
                        OriginalReference563Names.Run,
                        Term.A(subjectName)
                    );
                    OriginalCounters610Runtime.RequireOutside(context);
                    returnedHostOwnership610 = OriginalCounters610ResultOwnership.CaptureHostCarriers(actual);
                    Term minor = Term.Tuple(Term.A(MinorVersion), Term.I(2));
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
                    byte[] actualRaw = raw.ToArray();
                    byte[] actualDeterministic = deterministic.ToArray();
                    await File.WriteAllBytesAsync(System.IO.Path.Combine(output, RawFile), actualRaw);
                    await File.WriteAllBytesAsync(System.IO.Path.Combine(output, DeterministicFile), actualDeterministic);
                    byte[] original = File.ReadAllBytes(OriginalReference563AdmissionReader.Resolve(repository, receipt.DeterministicPath));
                    byte[] originalRaw = File.ReadAllBytes(OriginalReference563AdmissionReader.Resolve(repository, receipt.RawPath));
                    _ = OriginalCounters610Protocol.Read(actual);
                    bool successfulCases = true;
                    bool rawEqual = actualRaw.AsSpan().SequenceEqual(originalRaw);
                    bool deterministicEqual = actualDeterministic.AsSpan().SequenceEqual(original);
                    OriginalCounters610Result ownRefDefault = OriginalCounters610Compare.Compare(originalRaw, actualRaw);
                    OriginalCounters610Result ownRefDeterministic = OriginalCounters610Compare.Compare(original, actualDeterministic);
                    bool ownRefProtocol = OriginalCounters610Compare.SameBinding(ownRefDefault, ownRefDeterministic);
                    bool protocolPassed = ownRefProtocol;
                    Require(ReferenceEquals(runtime.Modules.SourceOwner(subjectName, OriginalReference563Names.Run, 0), subject)
                        && ReferenceEquals(runtime.Modules.SourceOwner(Worker, OriginalReference563Names.Run, 1), worker)
                        && ReferenceEquals(runtime.Modules.SourceOwner(Worker, WorkerFunction, WorkerArity), worker));
                    OriginalAtomics588Images.RequireOwners(
                        runtime,
                        stockImages,
                        repository,
                        counters
                    );
                    await OriginalAtomics588Images.CaptureOwners(
                        context,
                        runtime,
                        stockImages,
                        output,
                        OriginalAtomics588MetadataNames.AfterPhase
                    );
                    await File.WriteAllTextAsync(
                        System.IO.Path.Combine(output, ComparisonFile),
                        JsonSerializer.Serialize(new
                        {
                            Passed = protocolPassed && successfulCases,
                            OwnReferenceProtocolOptIn = true,
                            OwnReferenceProtocolPassed = ownRefProtocol,
                            OwnReferenceDefault = ownRefDefault,
                            OwnReferenceDeterministic = ownRefDeterministic,
                            RequiredSuccessfulCases = successfulCases,
                            OriginalBasedNegativeControlsPassed = true,
                            ActualProfileCheckedBeforeSubject = true,
                            WholeDefaultEtfEqual = rawEqual,
                            WholeDeterministicEtfEqual = deterministicEqual,
                            OwnedProfile = actualOwned,
                            SourcePin = Pin,
                            SubjectBeamSha256 = receipt.BeamSha256,
                            StockImages = stockImages.Select(image => new
                            {
                                image.Name,
                                image.OriginalBeamSha256,
                                NativeMd5 = Convert.ToHexString(image.NativeMd5.ToArray()),
                                Dependencies = image.RegistryModuleDependencies.Order(StringComparer.Ordinal).ToArray()
                            }).ToArray(),
                            WorkerBeamSha256 = receipt.WorkerBeamSha256,
                            WorkerSourceSha256 = receipt.WorkerSourceSha256,
                            EffectiveNativeMetadataSha256 = metadata.OriginalBeamSha256,
                            WholeRawValuesAndStacksCompared = true,
                            Comparison = OriginalCounters610Names.ComparisonLaw,
                            OriginalProfile = expectedProfile,
                            NativeFiveBindings = nativeFive,
                            NativeFiveSourceOwnerBeamSha256 = nativeCounters!.OriginalBeamSha256,
                            NativeFiveUsesScopedPinnedClauses = true,
                            WholeErtsInternalImageAdmitted = false,
                            NativeFiveExecutionEvidence = OriginalCounters610Fields.NativeFiveExecutionEvidence,
                            PublicSixOriginalSourceOwners = OriginalCounters610Names.PublicSix.Select(signature => new { Function = signature.Item1, Arity = signature.Item2 }).ToArray(),
                            RawEtfRetained = true,
                            RawEtfRewritten = false,
                            FullContractsClosed = 0
                        })
                    );
                    Require(protocolPassed && successfulCases);
                    completed.TrySetResult();
                }
                catch (Exception error)
                {
                    try
                    {
                        await File.WriteAllTextAsync(System.IO.Path.Combine(output, FailureFile), error.ToString());
                        completed.TrySetException(error);
                    }
                    catch (Exception evidenceError)
                    {
                        completed.TrySetException(new AggregateException(error, evidenceError));
                    }
                }

                finally
                {
                    OriginalCounters610Runtime.RequireOutside(context);
                    returnedHostOwnership610?.Dispose();
                }

                return Nil.Value;
            });
            await completed.Task.WaitAsync(Deadline);
            Require((await controller.Completion.WaitAsync(Deadline)).Equals(Term.A(Normal)));
        }
        catch (Exception error)
        {
            primary588 = error;
            throw;
        }
        finally
        {
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
                var counterCleanup = runtime.CountersStorage421;
                var atomicsCleanup = runtime.AtomicsStorage413;
                bool physicalClosed610 = counterCleanup.Closed && counterCleanup.LiveArrays == 0
                    && counterCleanup.OwnedNativeBytes == 0 && counterCleanup.ActiveInvocations == 0
                    && counterCleanup.Created == counterCleanup.Freed && atomicsCleanup.Closed
                    && atomicsCleanup.LiveArrays == 0 && atomicsCleanup.OwnedNativeBytes == 0
                    && atomicsCleanup.ActiveInvocations == 0 && atomicsCleanup.Created == atomicsCleanup.Freed;
                await File.WriteAllTextAsync(
                    System.IO.Path.Combine(output, OriginalCounters610Fields.Cleanup610Json),
                    JsonSerializer.Serialize(new
                    {
                        PhysicalClosed = physicalClosed610,
                        Counters = counterCleanup,
                        Atomics = atomicsCleanup,
                        OwnedProfile = owned,
                        ComparisonAloneIsNotCompletion = true
                    })
                );
                Require(physicalClosed610);
            }
            catch (Exception cleanup)
            {
                if (primary588 is not null) throw new AggregateException(primary588, cleanup);
                throw;
            }
        }
    }

}
