using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.NifNativeBackend;

internal static class NifDirty521Driver
{
    internal static async Task Run(string[] arguments, Func<bool, Task<ProcessRuntime>>? runtimeFactory = null)
    {
        if (arguments.Length != 5 || !OperatingSystem.IsWindows())
            throw new InvalidDataException(NifDirty521Names.Arguments);
        string admissionPath = Path.GetFullPath(arguments[0]);
        Verify(admissionPath, arguments[1]);
        using var admission = JsonDocument.Parse(File.ReadAllBytes(admissionPath));
        JsonElement root = admission.RootElement;
        Require(root.GetProperty(NifDirty521Names.FieldStatus).GetString() == NifDirty521Names.AdmissionStatus);
        Require(root.GetProperty(NifDirty521Names.FieldPin).GetString() == NifDirty521Names.Pin);
        foreach (var input in root.GetProperty(NifDirty521Names.FieldInputs).EnumerateArray())
            Verify(
                input.GetProperty(NifDirty521Names.FieldPath).GetString()!,
                input.GetProperty(NifDirty521Names.FieldSha256).GetString()!
            );
        string route = arguments[2];
        Require(route is NifDirty521Names.RouteOrdinary or NifDirty521Names.RouteReviewed);
        bool owned = arguments[3] switch
        {
            NifDirty521Names.ProfileOrdinary => false,
            NifDirty521Names.ProfileOwned => true,
            _ => throw new InvalidDataException(NifDirty521Names.Arguments)
        };
        string output = Path.GetFullPath(arguments[4]);
        Require(!Directory.Exists(output));
        Directory.CreateDirectory(output);
        string library = root.GetProperty(NifDirty521Names.FieldNativeLibraryPath).GetString()!;
        Require(Path.IsPathFullyQualified(library) && library.EndsWith(NifDirty521Names.LibrarySuffix, StringComparison.OrdinalIgnoreCase));
        string librarySha = root.GetProperty(NifDirty521Names.FieldNativeLibrarySha256).GetString()!;
        Verify(library, librarySha);
        Verify(
            root.GetProperty(NifDirty521Names.FieldBuildReceiptPath).GetString()!,
            root.GetProperty(NifDirty521Names.FieldBuildReceiptSha256).GetString()!
        );
        Require(MapFamily504NativeModuleHandle.Find(library) == 0);
        int environments = NifNativeCallEnvironment.ActiveCount;
        long buffers = NifNativeBinaryStorage.LiveBufferCount;
        int binaries = NifNativeBinaryRegistry.NativeOwnedCount;
        int mutexes795 = NifNativeAllocationOwner.LiveMutexCount795;
        int policyCalls = 0;
        string mode = root.GetProperty(NifDirty521Names.FieldMode).GetString()!;
        string observerModule526 = root.TryGetProperty(NifDirty521Names.FieldObserverModule526, out var observer526)
            ? observer526.GetString()! : NifDirty521Names.ObserverModule;
        string nifModule526 = root.TryGetProperty(NifDirty521Names.FieldNifModule526, out var native526)
            ? native526.GetString()! : NifDirty521Names.NifModule;
        var declaredModules526 = root.GetProperty(NifDirty521Names.FieldImages).EnumerateArray()
            .Select(image => image.GetProperty(NifDirty521Names.FieldModule).GetString()).ToArray();
        Require(declaredModules526.Contains(observerModule526) && declaredModules526.Contains(nifModule526));
        Require(mode is NifDirty521Names.ProbeMode or NifDirty521Names.WholeMode or NifDirty521Names.ControlMode);
        string barrier = Path.Combine(output, NifDirty521Names.BarrierFile), marker = Path.Combine(output, NifDirty521Names.UnloadMarkerFile);
        Require((barrier + marker).All(character => character is > '\0' and < '\u0080'));
        ProcessRuntime? runtime = null;
        var images = new List<PreparedBeamImage>();
        Exception? failure = null;
        byte[]? actualRaw = null, actualDeterministic = null;
        try
        {
            runtime = runtimeFactory is null ? NativeOwned516Profile.Create(owned) : await runtimeFactory(owned);
            Require(runtime.OwnedTermsPilot506 == owned);
            await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
                runtime,
                (registry, scope) =>
            {
                string[] declarations = root.GetProperty(NifDirty521Names.FieldDependencies).EnumerateArray().Select(value => value.GetString()!).ToArray();
                var errors = new List<Exception>();
                var publication = new List<PreparedBeamImage>();
                foreach (var image in root.GetProperty(NifDirty521Names.FieldImages).EnumerateArray())
                {
                    try
                    {
                        string path = image.GetProperty(NifDirty521Names.FieldPath).GetString()!;
                        Verify(path, image.GetProperty(NifDirty521Names.FieldSha256).GetString()!);
                        string module = image.GetProperty(NifDirty521Names.FieldModule).GetString()!;
                        if (registry.SourceOwner(module, NifDirty521Names.ModuleInfoFunction, 0) is PreparedBeamImage existing)
                        {
                            Require(existing.OriginalBeamSha256 == image.GetProperty(NifDirty521Names.FieldSha256).GetString());
                            images.Add(existing);
                            continue;
                        }
                        PreparedBeamImage prepared = scope.Prepare(() => registry.PrepareBeam(File.ReadAllBytes(path), new BeamDependencySelection(declarations)));
                        Require(prepared.Name == module);
                        images.Add(prepared);
                        publication.Add(prepared);
                    }
                    catch (Exception error)
                    {
                        errors.Add(error);
                    }
                }
                if (errors.Count != 0)
                    throw new AggregateException(errors);

                return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
                {
                    if (publication.Count != 0)
                        Require(registry.PublishPreparedBeam(publication.ToArray()).Status == BeamPublicationStatus.Published);
                });
            }
            );
            if (mode == NifDirty521Names.ControlMode)
            {
                var missing = new List<string>();
                foreach (var requirement in root.GetProperty(NifDirty521Names.FieldControlDependencies).EnumerateArray())
                {
                    string module = requirement.GetProperty(NifDirty521Names.FieldModule).GetString()!;
                    string function = requirement.GetProperty(NifDirty521Names.FieldFunction).GetString()!;
                    int arity = requirement.GetProperty(NifDirty521Names.FieldArity).GetInt32();
                    if (!runtime.Modules.HasExport(module, function, arity)
                        || !(runtime.Modules.IsNativeEntry(module, function, arity)
                            || runtime.Modules.SourceOwner(module, function, arity) is PreparedBeamImage))
                        missing.Add(module + NifDirty521Names.ModuleSeparator + function + NifDirty521Names.AritySeparator + arity);
                }
                File.WriteAllText(Path.Combine(output, NifDirty521Names.FileDependencyGatesJson), JsonSerializer.Serialize(missing));
                if (missing.Count != 0)
                    throw new InvalidOperationException(NifDirty521Names.MissingDependencies + string.Join(NifDirty521Names.MissingSeparator, missing));
            }
            if (route == NifDirty521Names.RouteReviewed)
                runtime.Modules.InstallReviewedNativeExecutionPolicy(path =>
                {
                    Interlocked.Increment(ref policyCalls);
                    Require(string.Equals(Path.GetFullPath(path), library, StringComparison.OrdinalIgnoreCase));

                    return new NifNativeReviewedBasicAbi(library, librarySha, root.GetProperty(NifDirty521Names.FieldBuildReceiptSha256).GetString()!);
                });
            ProcessRuntime current = runtime;
            var sourceOwners = images.Select(image =>
            {
                var exports = current.Modules.Exports.Where(export => export.Module == image.Name).ToArray();
                Require(exports.Length > 0 && exports.All(export => ReferenceEquals(
                    current.Modules.SourceOwner(export.Module, export.Function, export.Arity), image)));

                return new { image.Name, image.OriginalBeamSha256, Exports = exports.Select(export => new { export.Function, export.Arity }).ToArray() };
            }).ToArray();
            File.WriteAllText(Path.Combine(output, NifDirty521Names.FileSourceOwnersBeforeJson), JsonSerializer.Serialize(sourceOwners));
            await NifNativeHarness.Context(
                current,
                async context =>
            {
                Term io = await current.Modules.Call(
                    context,
                    NifDirty521Names.ErlangModule,
                    NifDirty521Names.SystemInfoFunction,
                    Term.A(NifDirty521Names.IoSelector)
                );
                Term cpu = await current.Modules.Call(
                    context,
                    NifDirty521Names.ErlangModule,
                    NifDirty521Names.SystemInfoFunction,
                    Term.A(NifDirty521Names.CpuSelector)
                );
                Require(io.Equals(Term.I(NifDirty521Names.IoWorkers)) && cpu.Equals(Term.I(1)));
                await File.WriteAllTextAsync(
                    Path.Combine(output, NifDirty521Names.RuntimeProfileFile),
                    JsonSerializer.Serialize(new { ActualIoWorkers = io.ToString(), ActualCpuWorkers = cpu.ToString(), OwnedProfile = owned })
                );
                Term observed;
                if (mode == NifDirty521Names.ControlMode)
                    observed = await current.Modules.Call(
                        context,
                        observerModule526,
                        NifDirty521Names.ObserveControlFunction,
                        Term.String(library[..^NifDirty521Names.LibrarySuffix.Length]),
                        Term.String(barrier),
                        Term.String(marker),
                        Term.Tuple(
                            Term.A(root.GetProperty(NifDirty521Names.FieldKind).GetString()!),
                            Term.A(root.GetProperty(NifDirty521Names.FieldLifecycle).GetString()!)
                        )
                    );
                else
                    observed = await current.Modules.Call(
                        context,
                        observerModule526,
                        NifDirty521Names.ObserveFunction,
                        Term.String(library[..^NifDirty521Names.LibrarySuffix.Length]),
                        Term.A(mode)
                    );
                actualRaw = await Encode(context, observed, false);
                actualDeterministic = await Encode(context, observed, true);
                await File.WriteAllBytesAsync(Path.Combine(output, NifDirty521Names.FileObservedEtf), actualRaw);
                await File.WriteAllBytesAsync(Path.Combine(output, NifDirty521Names.FileObservedDeterministicEtf), actualDeterministic);
                Require(images.All(image => current.Modules.Exports.Where(export => export.Module == image.Name)
                    .All(export => ReferenceEquals(current.Modules.SourceOwner(export.Module, export.Function, export.Arity), image))));
                if (observed is TupleTerm { Items: [Atom { Name: NifDirty521Names.RaisedTag }, _, _, _] })
                    throw new InvalidOperationException(NifDirty521Names.ObserverRaised);
            }
            );
        }
        catch (Exception error)
        {
            failure = error;
            try
            {
                await File.WriteAllTextAsync(Path.Combine(output, NifDirty521Names.FileFailureTxt), error.ToString());
                if (error is ErlangException guest)
                    await File.WriteAllBytesAsync(
                        Path.Combine(output, NifDirty521Names.FileGuestFailure),
                        ExternalTermFormat.Encode(
                        Term.Tuple(Term.A(guest.ExceptionClass), guest.Reason, guest.StackTraceTerm))
                    );
            }
            catch (Exception diagnostic)
            {
                failure = Combine(failure, diagnostic);
            }
        }
        finally
        {
            try
            {
                if (mode == NifDirty521Names.ControlMode)
                    await File.WriteAllTextAsync(barrier, NifDirty521Names.ReleaseContents);
                foreach (PreparedBeamImage image in images.Where(image => image.Name == nifModule526))
                    await image.ExecutionIdentity.Image.NifBindings.RetireAsync().WaitAsync(NifNativeHarnessNames.Deadline);
            }
            catch (Exception error)
            {
                failure = Combine(failure, error);
            }
            try
            {
                if (runtime is not null)
                    await runtime.DisposeAsync();
            }
            catch (Exception error)
            {
                failure = Combine(failure, error);
            }
        }
        bool moduleAbsentAfter = MapFamily504NativeModuleHandle.Find(library) == 0;
        NifNativeAllocationDomainSnapshot allocationLedger = runtime?.NativeAllocationSnapshot ?? default;
        bool allocationLedgerClean = allocationLedger.JoinedOwners == 0
            && allocationLedger.Allocations.Count == 0
            && allocationLedger.Allocations.RequestedBytes == 0
            && allocationLedger.Allocations.BackingRequestBytes == 0;
        int mutexesAfter795 = NifNativeAllocationOwner.LiveMutexCount795;
        bool mutexLedgerClean795 = mutexesAfter795 == mutexes795;
        bool clean = moduleAbsentAfter
            && allocationLedgerClean
            && mutexLedgerClean795
            && NifNativeCallEnvironment.ActiveCount == environments
            && NifNativeBinaryStorage.LiveBufferCount == buffers
            && NifNativeBinaryRegistry.NativeOwnedCount == binaries;
        if (!clean)
            failure = Combine(failure, new InvalidOperationException(NifDirty521Names.Cleanup));
        Verify(library, librarySha);
        bool rawEqual = actualRaw is not null && actualRaw.AsSpan().SequenceEqual(File.ReadAllBytes(root.GetProperty(NifDirty521Names.FieldOriginalRawPath).GetString()!));
        bool deterministicEqual = actualDeterministic is not null && actualDeterministic.AsSpan().SequenceEqual(File.ReadAllBytes(root.GetProperty(NifDirty521Names.FieldOriginalDeterministicPath).GetString()!));
        bool captured = failure is null && actualRaw is not null && actualDeterministic is not null;
        if (captured && (!rawEqual || !deterministicEqual))
            failure = Combine(failure, new InvalidOperationException(NifDirty521Names.RawMismatch));
        if (policyCalls != (route == NifDirty521Names.RouteReviewed ? 1 : 0) && failure is null)
            failure = new InvalidOperationException(NifDirty521Names.PolicyMismatch);
        if (failure is not null)
            File.WriteAllText(Path.Combine(output, NifDirty521Names.FileFailureTxt), failure.ToString());
        File.WriteAllText(
            Path.Combine(output, NifDirty521Names.FileCompletionJson),
            JsonSerializer.Serialize(
                new
                {
                    Captured = captured,
                    RawComparisonPassed = rawEqual && deterministicEqual,
                    ExecutionAndCleanupPassed = captured,
                    ChildPassedFiniteRawComparison = failure is null,
                    AdmissionSha256 = arguments[1],
                    Route = route,
                    OwnedProfile = owned,
                    Mode = mode,
                    RawWholeEqual = rawEqual,
                    DeterministicWholeEqual = deterministicEqual,
                    NormalizationPerformed = false,
                    NativeEnvironmentAndBinaryBaselinesRestored = clean,
                    NativeModuleAbsentBeforeAndAfter = moduleAbsentAfter,
                    NativeAllocationLedgerBaselineRestored = allocationLedgerClean,
                    NativeAllocationLedgerAfter = allocationLedger,
                    NativeMutexRegistryBaselineRestored = mutexLedgerClean795,
                    NativeMutexRegistryBefore = mutexes795,
                    NativeMutexRegistryAfter = mutexesAfter795,
                    NativeCountsBefore = new { Environments = environments, Buffers = buffers, Binaries = binaries },
                    NativeCountsAfter = new { Environments = NifNativeCallEnvironment.ActiveCount, Buffers = NifNativeBinaryStorage.LiveBufferCount, Binaries = NifNativeBinaryRegistry.NativeOwnedCount },
                    PolicyCalls = policyCalls,
                    TypedCausalAndPidReferenceOwnerProtocol = mode == NifDirty521Names.ProbeMode ? NifDirty521Names.FiniteRawFlagProtocol : NifDirty521Names.PendingCausalProtocol,
                    OriginalCleanupEtfCompared = false,
                    SourceCleanupProtocol = NifDirty521Names.PendingSourceCleanupProtocol,
                    SchedulerCapacityAndCompleteEvalErrorLaunchContext = NifDirty521Names.ProtocolPending,
                    FullCompatibilityClaimed = false,
                    WholeContractsClosed = 0,
                    Error = failure?.ToString()
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        if (failure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static async Task<byte[]> Encode(ProcessContext context, Term observed, bool deterministic)
    {
        Term minor = Term.Tuple(Term.A(NifDirty521Names.MinorVersionOption), Term.I(NifDirty521Names.SerializationMinorVersion));
        Term options = deterministic ? Term.List(Term.A(NifDirty521Names.DeterministicOption), minor) : Term.List(minor);
        var bytes = (BitString)await context.Runtime.Modules.Call(
            context,
            NifDirty521Names.ErlangModule,
            NifDirty521Names.EncodeFunction,
            observed,
            options
        );

        return bytes.ToArray();
    }

    private static Exception Combine(Exception? previous, Exception next) => previous is null ? next : new AggregateException(previous, next);

    private static void Verify(string path, string expected)
    {
        Require(Path.IsPathFullyQualified(path) && Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) == expected.ToUpperInvariant());
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifDirty521Names.Broken);
    }
}
