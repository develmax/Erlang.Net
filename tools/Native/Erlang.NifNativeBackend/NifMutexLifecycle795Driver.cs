using System.Security.Cryptography;
using System.Text.Json;
using Names = Erlang.NifNativeBackend.NifMutexLifecycle795DriverNames;
namespace Erlang.NifNativeBackend;

internal static class NifMutexLifecycle795Driver
{
    internal static async Task Run(string[] args)
    {
        Require(args.Length == 5 && OperatingSystem.IsWindows());
        string admissionPath = Path.GetFullPath(args[0]);
        Verify(admissionPath, args[1]);
        using var document = JsonDocument.Parse(File.ReadAllBytes(admissionPath));
        JsonElement plan = document.RootElement;
        Require(Text(plan, Names.Status) == Names.AdmissionStatus && Text(plan, Names.Pin) == NifDirty521Names.Pin);

        void VerifyInputs()
        {
            Verify(admissionPath, args[1]);
            foreach (var row in plan.GetProperty(Names.Inputs).EnumerateArray()) Verify(Text(row, Names.Path), Text(row, Names.Sha256));
            foreach (var row in plan.GetProperty(Names.ManagedInputs).EnumerateArray()) Verify(Text(row, Names.Path), Text(row, Names.Sha256));
        }
        VerifyInputs();
        Require(string.Equals(typeof(NifMutexLifecycle795Driver).Assembly.Location, Text(plan, Names.ManagedHost), StringComparison.OrdinalIgnoreCase));
        var loadedCode = new[] { typeof(NifMutexLifecycle795Driver).Assembly, typeof(ProcessRuntime).Assembly,
            typeof(Term).Assembly, typeof(ExternalTermFormat).Assembly }.Distinct().Select(assembly =>
        {
            var admitted = plan.GetProperty(Names.ManagedInputs).EnumerateArray().Single(row =>
                string.Equals(Text(row, Names.Path), assembly.Location, StringComparison.OrdinalIgnoreCase));
            Verify(assembly.Location, Text(admitted, Names.Sha256));

            return new { assembly.Location, Sha256 = Text(admitted, Names.Sha256), Mvid = assembly.ManifestModule.ModuleVersionId };
        }).ToArray();

        string subject = Text(plan, Names.Subject);
        Require(subject is Names.Failed or Names.Upgrade);
        string route = args[2];
        Require(route is NifDirty521Names.RouteOrdinary or NifDirty521Names.RouteReviewed);
        bool owned = args[3] switch
        {
            NifDirty521Names.ProfileOrdinary => false,
            NifDirty521Names.ProfileOwned => true,
            _ => throw new InvalidDataException(NifMutexLifecycle795Names.Failure)
        };
        string output = Path.GetFullPath(args[4]);
        Require(!Directory.Exists(output));
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, Names.LoadedCodeFile), JsonSerializer.Serialize(loadedCode));
        var images = plan.GetProperty(Names.NativeImages).EnumerateArray().ToDictionary(row => Text(row, Names.Id));
        Require(subject == Names.Failed ? images.Count == 1 && images.ContainsKey(Names.Failed)
            : images.Count == 2 && images.ContainsKey(Names.A) && images.ContainsKey(Names.B));
        foreach (var image in images.Values)
        {
            Verify(Text(image, Names.Path), Text(image, Names.Sha256));
            Verify(Text(image, Names.BuildReceiptPath), Text(image, Names.BuildReceiptSha256));
            Require(MapFamily504NativeModuleHandle.Find(Text(image, Names.Path)) == 0);
        }
        int environments = NifNativeCallEnvironment.ActiveCount;
        long buffers = NifNativeBinaryStorage.LiveBufferCount;
        int binaries = NifNativeBinaryRegistry.NativeOwnedCount;
        int mutexes = NifNativeAllocationOwner.LiveMutexCount795;
        int policyCalls = 0;
        var errors = new List<string>();
        bool whole = false, split = subject == Names.Failed;
        byte[]? raw = null, deterministic = null;
        Term original = ExternalTermFormat.Decode(File.ReadAllBytes(Text(plan, Names.OriginalRawPath)));

        async Task<ProcessRuntime> Create()
        {
            var runtime = NativeOwned516Profile.Create(owned);
            try
            {
                PreparedBeamImage observer = runtime.Modules.PrepareBeam(
                    File.ReadAllBytes(Text(plan, Names.ObserverBeam)),
                    new BeamDependencySelection([Names.Erlang, Names.Module, Names.Observer])
                );
                await runtime.InitializeModulesAsync(registry =>
                {
                    Require(observer.Name == Names.Observer && registry.PublishPreparedBeam(observer).Status == BeamPublicationStatus.Published);
                    Require(ReferenceEquals(
                        registry.SourceOwner(Names.Observer, subject == Names.Failed ? Names.Failed : Names.Upgrade,
                        subject == Names.Failed ? 2 : 4),
                        observer
                    ));
                });
                if (route == NifDirty521Names.RouteReviewed)
                    runtime.Modules.InstallReviewedNativeExecutionPolicy(path =>
                    {
                        Interlocked.Increment(ref policyCalls);
                        var entry = images.Values.Single(row => string.Equals(Path.GetFullPath(path), Text(row, Names.Path), StringComparison.OrdinalIgnoreCase));

                        return new NifNativeReviewedBasicAbi(path, Text(entry, Names.Sha256), Text(entry, Names.BuildReceiptSha256));
                    });

                return runtime;
            }
            catch (Exception primary)
            {
                try
                {
                    await runtime.DisposeAsync().AsTask().WaitAsync(NifNativeHarnessNames.Deadline);
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(primary, cleanup);
                }
                throw;
            }
        }

        string Stem(string id) => Path.ChangeExtension(Text(images[id], Names.Path), null)!.Replace(Path.DirectorySeparatorChar, '/');

        BitString Beam(string field) => new(File.ReadAllBytes(Text(plan, field)));

        void Ledger(string name, ProcessRuntime runtime)
        {
            var capture = NifMutexLifecycle795Checks.Capture(runtime);
            File.WriteAllText(Path.Combine(output, name), JsonSerializer.Serialize(new { capture.Domain, capture.Mutexes }));
        }

        async Task Close(ProcessRuntime runtime)
        {
            await runtime.DisposeAsync().AsTask().WaitAsync(NifNativeHarnessNames.Deadline);
            var after = runtime.NativeAllocationSnapshot;
            var a = after.Allocations;
            Require(after.JoinedOwners == 0 && a.Count == 0 && a.RequestedBytes == 0 && a.BackingRequestBytes == 0);
            Require(a.AllocatedRequestedBytes == a.ReleasedRequestedBytes && a.AllocatedBackingRequestBytes == a.ReleasedBackingRequestBytes
                && a.SuccessfulAllocations == a.ReleasedAllocations);
        }
        ProcessRuntime? current = null;
        try
        {
            current = await Create();
            ProcessRuntime runtime = current;
            var baseline = NifMutexLifecycle795Checks.Capture(runtime);
            await NifNativeHarness.Context(
                runtime,
                async context =>
            {
                Term result = subject == Names.Failed
                    ? await runtime.Modules.Call(
                        context,
                        Names.Observer,
                        Names.Failed,
                        Beam(Names.BeamA),
                        Term.String(Stem(Names.Failed))
                    )
                    : await runtime.Modules.Call(
                        context,
                        Names.Observer,
                        Names.Upgrade,
                        Beam(Names.BeamA),
                        Beam(Names.BeamB),
                        Term.String(Stem(Names.A)),
                        Term.String(Stem(Names.B))
                    );
                Term wrapped = Term.Tuple(Term.A(Names.Returned), result);
                raw = await Encode(context, wrapped, false);
                deterministic = await Encode(context, wrapped, true);
                File.WriteAllBytes(Path.Combine(output, NifDirty521Names.FileObservedEtf), raw);
                File.WriteAllBytes(Path.Combine(output, Names.DeterministicFile), deterministic);
                if (subject == Names.Failed) NifMutexLifecycle795Checks.FailedInitializationClosed(runtime, baseline);
                Ledger(Names.WholeLedgerFile, runtime);
            }
            );
            whole = raw!.AsSpan().SequenceEqual(File.ReadAllBytes(Text(plan, Names.OriginalRawPath)))
                && deterministic!.AsSpan().SequenceEqual(File.ReadAllBytes(Text(plan, Names.OriginalDeterministicPath)));
            Require(whole);
        }
        catch (Exception error)
        {
            errors.Add(error.ToString());
        }
        finally
        {
            if (current is not null) try
                {
                    await Close(current);
                }
                catch (Exception error)
                {
                    errors.Add(error.ToString());
                }
        }
        if (subject == Names.Upgrade)
        {
            current = null;
            ProcessRuntime? independent = null;
            try
            {
                current = await Create();
                independent = NativeOwned516Profile.Create(owned);
                ProcessRuntime runtime = current;
                var baseline = NifMutexLifecycle795Checks.Capture(runtime);
                var expected = ((TupleTerm)((TupleTerm)original).Items[1]).Items;
                await NifNativeHarness.Context(
                    runtime,
                    async context =>
                {
                    async ValueTask<Term> Call(string function, params Term[] arguments)
                    {
                        await context.ExecutionCheckpoint();

                        return await runtime.Modules.Call(
                            context,
                            Names.Observer,
                            function,
                            arguments
                        );
                    }
                    Require((await Call(Names.OpenA, Beam(Names.BeamA), Term.String(Stem(Names.A)))).Equals(expected[0]));
                    ModuleCodeImage imageA = runtime.Modules.CurrentCodeImage(Names.Module)!;
                    NifNativeLibraryOwner ownerA = NifMutexLifecycle795Checks.CaptureOwner(imageA);
                    Require((await Call(Names.UpgradeB, Beam(Names.BeamB), Term.String(Stem(Names.B)))).Equals(expected[1]));
                    ModuleCodeImage imageB = runtime.Modules.CurrentCodeImage(Names.Module)!;
                    NifNativeLibraryOwner ownerB = NifMutexLifecycle795Checks.CaptureOwner(imageB);
                    Require(!ReferenceEquals(imageA, imageB));
                    Require(MapFamily504NativeModuleHandle.Find(Text(images[Names.A], Names.Path)) != 0
                        && MapFamily504NativeModuleHandle.Find(Text(images[Names.B], Names.Path)) != 0);
                    Require((await Call(Names.PurgeOld)).Equals(expected[2]));
                    await NifMutexLifecycle795Checks.AwaitActuallyRetiredImage(imageA);
                    NifMutexLifecycle795Checks.SourceImageClosedWithLiveTransfer(runtime, ownerA, baseline);
                    Require(MapFamily504NativeModuleHandle.Find(Text(images[Names.A], Names.Path)) == 0
                        && MapFamily504NativeModuleHandle.Find(Text(images[Names.B], Names.Path)) != 0);
                    Ledger(Names.SourceClosedLedgerFile, runtime);
                    Require((await Call(Names.ProbeCurrent)).Equals(expected[3]));
                    Require((await Call(Names.DestroyCurrent)).Equals(expected[4]));
                    NifMutexLifecycle795Checks.ExplicitMutexDestroyed(runtime, ownerA, baseline);
                    Ledger(Names.DestroyedLedgerFile, runtime);
                    Require((await Call(Names.ProbeCurrent)).Equals(expected[5]));
                    var closed = (TupleTerm)await Call(Names.CloseCurrent);
                    Require(closed.Items[0].Equals(expected[6]) && closed.Items[1].Equals(expected[7]) && closed.Items[2].Equals(expected[8]));
                    await NifMutexLifecycle795Checks.AwaitActuallyRetiredImage(imageB);
                    NifMutexLifecycle795Checks.BothImagesClosed(
                        runtime,
                        ownerA,
                        ownerB,
                        baseline
                    );
                    Ledger(Names.BothClosedLedgerFile, runtime);
                }
                );
                await NifNativeHarness.Context(
                    independent,
                    async context =>
                    Require((await independent.Modules.Call(
                        context,
                        Names.Erlang,
                        Names.Abs,
                        Term.I(-9)
                    )).Equals(Term.I(9)))
                );
                split = true;
            }
            catch (Exception error)
            {
                errors.Add(error.ToString());
            }
            finally
            {
                if (current is not null) try
                    {
                        await Close(current);
                    }
                    catch (Exception error)
                    {
                        errors.Add(error.ToString());
                    }
                if (independent is not null) try
                    {
                        await Close(independent);
                    }
                    catch (Exception error)
                    {
                        errors.Add(error.ToString());
                    }
            }
        }
        bool clean = NifNativeCallEnvironment.ActiveCount == environments && NifNativeBinaryStorage.LiveBufferCount == buffers
            && NifNativeBinaryRegistry.NativeOwnedCount == binaries && NifNativeAllocationOwner.LiveMutexCount795 == mutexes
            && images.Values.All(row => MapFamily504NativeModuleHandle.Find(Text(row, Names.Path)) == 0);
        try
        {
            VerifyInputs();
            Require(clean);
            Require(policyCalls == (route == NifDirty521Names.RouteReviewed ? subject == Names.Failed ? 1 : 4 : 0));
        }
        catch (Exception error)
        {
            errors.Add(error.ToString());
        }
        File.WriteAllText(
            Path.Combine(output, NifDirty521Names.FileCompletionJson),
            JsonSerializer.Serialize(
                new
                {
                    Passed = errors.Count == 0 && whole && split && clean,
                    WholeRawAndDeterministicEqual = whole,
                    PhysicalCloseBeforeTransferredMutexUse = split && subject == Names.Upgrade,
                    FailedInitializationNativeCustodyClosed = whole && subject == Names.Failed && clean,
                    ActualNativeBaselinesRestored = clean,
                    PolicyCalls = policyCalls,
                    AdmissionSha256 = args[1],
                    Route = route,
                    OwnedProfile = owned,
                    Subject = subject,
                    NormalizationPerformed = false,
                    FullCompatibilityClaimed = false,
                    WholeContractsClosed = 0,
                    Errors = errors
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        if (errors.Count != 0) throw new AggregateException(errors.Select(error => new InvalidOperationException(error)));
    }

    private static async Task<byte[]> Encode(ProcessContext context, Term value, bool deterministic)
    {
        Term minor = Term.Tuple(Term.A(NifDirty521Names.MinorVersionOption), Term.I(NifDirty521Names.SerializationMinorVersion));
        Term options = deterministic ? Term.List(Term.A(NifDirty521Names.DeterministicOption), minor) : Term.List(minor);

        return ((BitString)await context.Runtime.Modules.Call(
            context,
            Names.Erlang,
            NifDirty521Names.EncodeFunction,
            value,
            options
        )).ToArray();
    }

    private static string Text(JsonElement value, string key) => value.GetProperty(key).GetString()!;

    private static void Verify(string path, string hash) => Require(Path.IsPathFullyQualified(path)
        && Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) == hash.ToUpperInvariant());

    private static void Require(bool value)
    {
        if (!value) throw new InvalidDataException(NifMutexLifecycle795Names.Failure);
    }
}
