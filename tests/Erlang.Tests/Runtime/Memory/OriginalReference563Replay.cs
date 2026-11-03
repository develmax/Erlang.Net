using System.Reflection;
using System.Text.Json;
using static Erlang.Tests.OriginalReference563Names;

namespace Erlang.Tests;

internal static partial class OriginalReference563Replay
{
    internal static async Task Run(
        string repository,
        string admission,
        string admissionSha,
        string metadataCompletion,
        string metadataSha,
        bool owned,
        string output
    )
    {
        repository = System.IO.Path.GetFullPath(repository);
        OriginalReference563Admission receipt = OriginalReference563AdmissionReader.Read(repository, admission, admissionSha);
        await RunAdmitted588(
            repository,
            receipt,
            metadataCompletion,
            metadataSha,
            owned,
            output,
            null
        );
    }

    private static async Task RunAdmitted588(
        string repository,
        OriginalReference563Admission receipt,
        string metadataCompletion,
        string metadataSha,
        bool owned,
        string output,
        OriginalAtomics588Admission? atomics,
        bool atomicsOwnRef590 = false
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
        bool loaderMap = receipt.Job == OriginalLoaderMap528Names.Job;
        string subjectName = atomics is not null ? OriginalAtomics588Names.Subject : loaderMap ? OriginalLoaderMap528Names.Subject : Subject;
        Directory.CreateDirectory(output);
        var runtime = OwnedGuest506RuntimeFactory.Create(owned, metadata);
        Exception? primary588 = null;
        try
        {
            bool actualOwned = (bool)typeof(ProcessRuntime).GetProperty(
                OwnedGuest506Names.PrivateOption,
                BindingFlags.Instance | BindingFlags.NonPublic
            )!.GetValue(runtime)!;
            Require(actualOwned == owned);
            await DebugSame420HostAccess.Initialize(runtime);
            await runtime.InitializeModulesAsync(registry =>
            {
                RuntimeBootstrapDebugSharing425.Register(runtime, registry);
                if (atomics is not null) RuntimeBootstrapAtomics.Register(runtime, registry);
                if (loaderMap)
                    RuntimeBootstrapMapKeys426.Register(runtime, registry);
            });
            PreparedBeamImage[] stockImages = atomics is null ? [] : OriginalAtomics588Images.Prepare(runtime, repository, atomics);
            PreparedBeamImage subject = runtime.Modules.PrepareBeam(
                File.ReadAllBytes(OriginalReference563AdmissionReader.Resolve(repository, receipt.BeamPath)),
                new BeamDependencySelection(atomics is not null
                    ? DynamicApplyBoundary520Imports.Read(File.ReadAllBytes(OriginalReference563AdmissionReader.Resolve(repository, receipt.BeamPath)))
                    : loaderMap
                    ? new[] { Core, Debug, OriginalLoaderMap528Names.InternalModule, OriginalLoaderMap528Names.MapsModule, OriginalLoaderMap528Names.OtherSubject }
                    : new[] { Core, Debug })
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
            if (atomics is not null) OriginalAtomics588Images.RequireWorkers(repository, atomics, [subject, worker]);
            PreparedBeamImage? other = loaderMap ? runtime.Modules.PrepareBeam(
                File.ReadAllBytes(OriginalReference563AdmissionReader.Resolve(repository, receipt.OtherBeamPath!)),
                new BeamDependencySelection(Array.Empty<string>())
            ) : null;
            if (other is not null)
                Require(other.Name == OriginalLoaderMap528Names.OtherSubject && !other.HasOnLoad
                    && other.NativePrimitiveExports.Count == 0 && other.OriginalBeamSha256 == receipt.OtherBeamSha256);
            Require(runtime.Modules.PublishPreparedBeam(atomics is not null ? [.. stockImages, subject, worker]
                : other is null ? [subject, worker] : [other, subject, worker]).Status == BeamPublicationStatus.Published);
            var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ProcessHandle controller = runtime.Spawn(async context =>
            {
                try
                {
                    Require(ReferenceEquals(runtime.Modules.SourceOwner(subjectName, OriginalReference563Names.Run, 0), subject)
                        && ReferenceEquals(runtime.Modules.SourceOwner(Worker, OriginalReference563Names.Run, 1), worker)
                        && ReferenceEquals(runtime.Modules.SourceOwner(Worker, WorkerFunction, WorkerArity), worker)
                        && (other is null || ReferenceEquals(runtime.Modules.SourceOwner(OriginalLoaderMap528Names.OtherSubject, OriginalLoaderMap528Names.OtherFunction, 1), other)));
                    if (atomics is not null) OriginalAtomics588Images.RequireOwners(
                        runtime,
                        stockImages,
                        repository,
                        atomics
                    );
                    if (atomics is not null) await OriginalAtomics588Images.CaptureOwners(
                        context,
                        runtime,
                        stockImages,
                        output,
                        OriginalAtomics588MetadataNames.BeforePhase
                    );
                    Term actual = await runtime.Modules.Call(
                        context,
                        Worker,
                        OriginalReference563Names.Run,
                        Term.A(subjectName)
                    );
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
                    bool successfulCases = atomics is not null ? OriginalAtomics588Cases.HasSuccessfulCases(actual)
                        : loaderMap ? HasLoaderMapCases(actual) : HasSuccessfulCases(actual);
                    bool rawEqual = actualRaw.AsSpan().SequenceEqual(originalRaw);
                    bool deterministicEqual = actualDeterministic.AsSpan().SequenceEqual(original);
                    OriginalAtomics590Result? ownRefDefault = atomicsOwnRef590 && atomics is not null
                        ? OriginalAtomics590Identity.Compare(originalRaw, actualRaw) : null;
                    OriginalAtomics590Result? ownRefDeterministic = atomicsOwnRef590 && atomics is not null
                        ? OriginalAtomics590Identity.Compare(original, actualDeterministic) : null;
                    bool ownRefProtocol = ownRefDefault is not null && ownRefDeterministic is not null
                        && OriginalAtomics590Identity.SameBinding(ownRefDefault, ownRefDeterministic);
                    bool protocolPassed = rawEqual && deterministicEqual || ownRefProtocol;
                    Require(ReferenceEquals(runtime.Modules.SourceOwner(subjectName, OriginalReference563Names.Run, 0), subject)
                        && ReferenceEquals(runtime.Modules.SourceOwner(Worker, OriginalReference563Names.Run, 1), worker)
                        && ReferenceEquals(runtime.Modules.SourceOwner(Worker, WorkerFunction, WorkerArity), worker)
                        && (other is null || ReferenceEquals(runtime.Modules.SourceOwner(OriginalLoaderMap528Names.OtherSubject, OriginalLoaderMap528Names.OtherFunction, 1), other)));
                    if (atomics is not null) OriginalAtomics588Images.RequireOwners(
                        runtime,
                        stockImages,
                        repository,
                        atomics
                    );
                    if (atomics is not null) await OriginalAtomics588Images.CaptureOwners(
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
                            OwnReferenceProtocolOptIn = atomicsOwnRef590,
                            OwnReferenceProtocolPassed = ownRefProtocol,
                            OwnReferenceDefault = ownRefDefault,
                            OwnReferenceDeterministic = ownRefDeterministic,
                            RequiredSuccessfulCases = successfulCases,
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
                            Comparison = atomicsOwnRef590 ? OriginalAtomics590Names.ComparisonLaw
                                : WholeByteComparisonLaw,
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

                return Nil.Value;
            });
            await completed.Task.WaitAsync(Deadline);
            await controller.Completion.WaitAsync(Deadline);
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
            }
            catch (Exception cleanup)
            {
                if (primary588 is not null) throw new AggregateException(primary588, cleanup);
                throw;
            }
        }
    }

    private static bool HasLoaderMapCases(Term workerResult)
    {
        if (workerResult is not TupleTerm { Items.Count: 2 } outer
            || !outer.Items[1].Equals(Term.A(Normal))
            || outer.Items[0] is not TupleTerm { Items.Count: 2 } result
            || !result.Items[0].Equals(Term.A(Returned)))
            return false;
        var fields = new HashSet<string>(StringComparer.Ordinal);
        Term remaining = result.Items[1];
        while (remaining is Cons cell)
        {
            if (cell.Head is not TupleTerm { Items.Count: 2 } field || field.Items[0] is not Atom key
                || !fields.Add(key.Name))
                return false;
            remaining = cell.Tail;
        }

        return remaining is Nil && fields.SetEquals(OriginalLoaderMap528Names.Fields);
    }

    // Coverage gate only, after full raw capture. Exact whole ETF remains the
    // comparison law; no field or raised error is removed from that comparison.
    private static bool HasSuccessfulCases(Term workerResult)
    {
        if (workerResult is not TupleTerm { Items.Count: 2 } outer
            || !outer.Items[1].Equals(Term.A(Normal))
            || outer.Items[0] is not TupleTerm { Items.Count: 2 } result
            || !result.Items[0].Equals(Term.A(Returned)))
            return false;
        bool ordinary = false, roundtrip = false;
        Term remaining = result.Items[1];
        while (remaining is Cons cell)
        {
            if (cell.Head is not TupleTerm { Items.Count: 2 } field || field.Items[0] is not Atom key)
                return false;
            if (key.Name is OrdinaryCase or RoundtripCase)
            {
                if (field.Items[1] is not TupleTerm { Items.Count: 2 } outcome
                    || !outcome.Items[0].Equals(Term.A(Returned)))
                    return false;
                if (key.Name == OrdinaryCase)
                {
                    if (ordinary)
                        return false;
                    ordinary = true;
                }
                else
                {
                    if (roundtrip)
                        return false;
                    roundtrip = true;
                }
            }
            remaining = cell.Tail;
        }

        return remaining is Nil && ordinary && roundtrip;
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidDataException(Failure);
    }
}
