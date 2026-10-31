using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
namespace Erlang.NifNativeBackend;

internal static class NifLinuxMutex795Driver
{
    internal static async Task Run(string[] args)
    {
        Require(OperatingSystem.IsLinux() && IntPtr.Size == 8 && args.Length == 5);
        Require(Marshal.SizeOf<NifNativeLinuxMutex795Table>() == NifNativeLinuxMutex795Names.Size
            && (int)Marshal.OffsetOf<NifNativeLinuxMutex795Table>(nameof(NifNativeLinuxMutex795Table.Create)) == 8
            && (int)Marshal.OffsetOf<NifNativeLinuxMutex795Table>(nameof(NifNativeLinuxMutex795Table.Name)) == 48);
        Verify(args[0], args[1]);
        using var admission = JsonDocument.Parse(File.ReadAllBytes(args[0]));
        JsonElement plan = admission.RootElement;
        Require(Text(plan, NifDirty521Names.FieldStatus) == NifLinuxMutex795Names.Admission
            && Text(plan, NifDirty521Names.FieldPin) == NifDirty521Names.Pin);

        void VerifyInputs()
        {
            Verify(args[0], args[1]);
            foreach (var row in plan.GetProperty(NifDirty521Names.FieldInputs).EnumerateArray())
                Verify(Text(row, NifDirty521Names.FieldPath), Text(row, NifDirty521Names.FieldSha256));
        }
        VerifyInputs();
        string library = Text(plan, NifDirty521Names.FieldNativeLibraryPath);
        string libraryHash = Text(plan, NifDirty521Names.FieldNativeLibrarySha256);
        string receiptHash = Text(plan, NifDirty521Names.FieldBuildReceiptSha256);
        Verify(library, libraryHash);
        Verify(Text(plan, NifDirty521Names.FieldBuildReceiptPath), receiptHash);
        Verify(Text(plan, NifLinuxMutex795Names.Bridge), Text(plan, NifLinuxMutex795Names.BridgeHash));
        Verify(Text(plan, NifLinuxMutex795Names.BridgeReceipt), Text(plan, NifLinuxMutex795Names.BridgeReceiptHash));
        string route = args[2];
        Require(route is NifDirty521Names.RouteOrdinary or NifDirty521Names.RouteReviewed);
        bool owned = args[3] switch
        {
            NifDirty521Names.ProfileOrdinary => false,
            NifDirty521Names.ProfileOwned => true,
            _ => throw new InvalidDataException(NifLinuxMutex795Names.Failure)
        };
        if (route == NifDirty521Names.RouteOrdinary)
            Verify(
                Path.Combine(AppContext.BaseDirectory, NifLinuxCallbackNames.PackagedBridge),
                Text(plan, NifLinuxMutex795Names.BridgeHash)
            );
        string output = Path.GetFullPath(args[4]);
        Require(!Directory.Exists(output) && output.Length < 2048 && output.All(value => value is > '\0' and < '\u0080'));
        Directory.CreateDirectory(output);
        int beforeEnvironments = NifNativeCallEnvironment.ActiveCount;
        long beforeBuffers = NifNativeBinaryStorage.LiveBufferCount;
        int beforeBinaries = NifNativeBinaryRegistry.NativeOwnedCount;
        int beforeMutexes = NifNativeAllocationOwner.LiveMutexCount795;
        var errors = new List<string>();
        ProcessRuntime? first = null, second = null;
        Task? holder = null, retiring = null;
        string started = Path.Combine(output, NifLinuxMutex795Names.StartedFile);
        string release = Path.Combine(output, NifLinuxMutex795Names.ReleaseFile);
        bool firstEqual = false, secondEqual = false, authority = false, fence = false;
        int policyCalls = 0;

        async Task<ProcessRuntime> Create()
        {
            var runtime = NativeOwned516Profile.Create(owned);
            try
            {
                var dependencies = new BeamDependencySelection([ErlangModuleNames.Module, NifLinuxMutex795Names.Module, NifLinuxMutex795Names.Observer]);
                PreparedBeamImage module = runtime.Modules.PrepareBeam(File.ReadAllBytes(Text(plan, NifLinuxMutex795Names.ModuleBeam)), dependencies);
                PreparedBeamImage observer = runtime.Modules.PrepareBeam(File.ReadAllBytes(Text(plan, NifLinuxMutex795Names.ObserverBeam)), dependencies);
                await runtime.InitializeModulesAsync(registry =>
                {
                    Require(module.Name == NifLinuxMutex795Names.Module && observer.Name == NifLinuxMutex795Names.Observer);
                    Require(registry.PublishPreparedBeam(module, observer).Status == BeamPublicationStatus.Published);
                });
                if (route == NifDirty521Names.RouteReviewed)
                    runtime.Modules.InstallReviewedNativeExecutionPolicy(path =>
                    {
                        Require(string.Equals(Path.GetFullPath(path), library, StringComparison.Ordinal));
                        Interlocked.Increment(ref policyCalls);

                        return new NifNativeReviewedBasicAbi(
                            path,
                            libraryHash,
                            receiptHash,
                            Text(plan, NifLinuxMutex795Names.Bridge),
                            Text(plan, NifLinuxMutex795Names.BridgeHash)
                        );
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

        async Task<bool> Observe(ProcessRuntime runtime, string rawFile, string deterministicFile)
        {
            byte[]? raw = null, deterministic = null;
            await NifNativeHarness.Context(
                runtime,
                async context =>
            {
                Term result = await runtime.Modules.Call(
                    context,
                    NifLinuxMutex795Names.Observer,
                    NifDirty521Names.ObserveFunction,
                    Term.String(Path.ChangeExtension(library, null)!),
                    Term.A(NifDirty521Names.ProbeMode)
                );
                raw = await Encode(context, result, false);
                deterministic = await Encode(context, result, true);
                File.WriteAllBytes(Path.Combine(output, rawFile), raw);
                File.WriteAllBytes(Path.Combine(output, deterministicFile), deterministic);
            }
            );

            return raw!.AsSpan().SequenceEqual(File.ReadAllBytes(Text(plan, NifDirty521Names.FieldOriginalRawPath)))
                && deterministic!.AsSpan().SequenceEqual(File.ReadAllBytes(Text(plan, NifDirty521Names.FieldOriginalDeterministicPath)));
        }

        async Task Close(ProcessRuntime runtime)
        {
            await runtime.DisposeAsync().AsTask().WaitAsync(NifNativeHarnessNames.Deadline);
            var value = runtime.NativeAllocationSnapshot;
            var a = value.Allocations;
            Require(value.JoinedOwners == 0 && a.Count == 0 && a.RequestedBytes == 0 && a.BackingRequestBytes == 0);
            Require(a.AllocatedRequestedBytes == a.ReleasedRequestedBytes && a.AllocatedBackingRequestBytes == a.ReleasedBackingRequestBytes
                && a.SuccessfulAllocations == a.ReleasedAllocations);
        }
        try
        {
            first = await Create();
            second = await Create();
            firstEqual = await Observe(first, NifLinuxMutex795Names.RawA, NifLinuxMutex795Names.DeterministicA);
            secondEqual = await Observe(second, NifLinuxMutex795Names.RawB, NifLinuxMutex795Names.DeterministicB);
            Require(firstEqual && secondEqual);
            NifLinuxMutex795AuthorityChecks.Check(first, second);
            authority = true;
            ProcessRuntime runtime = first;
            ModuleCodeImage image = runtime.Modules.CurrentCodeImage(NifLinuxMutex795Names.Module)!;
            NifNativeLibraryOwner owner = NifMutexLifecycle795Checks.CaptureOwner(image);
            holder = NifNativeHarness.Context(
                runtime,
                async context =>
            {
                Term held = await runtime.Modules.Call(
                    context,
                    NifLinuxMutex795Names.Module,
                    NifLinuxMutex795Names.Hold,
                    Term.String(started),
                    Term.String(release)
                );
                var original = (TupleTerm)ExternalTermFormat.Decode(File.ReadAllBytes(Text(plan, NifDirty521Names.FieldOriginalRawPath)));
                Require(held.Equals(original.Items[1]));
            }
            );
            using (var deadline = new CancellationTokenSource(NifNativeHarnessNames.Deadline))
                while (!File.Exists(started))
                {
                    if (holder.IsCompleted) await holder;
                    await Task.Delay(10, deadline.Token);
                }
            retiring = image.NifBindings.RetireAsync();
            Require(!retiring.IsCompleted && !holder.IsCompleted);
            var allocations = (NifNativeAllocationOwner)typeof(NifNativeLibraryOwner)
                .GetField(NifLinuxMutex795Names.Allocations, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
            Require(!allocations.PhysicalImageClosed528);
            File.WriteAllText(release, string.Empty);
            await holder.WaitAsync(NifNativeHarnessNames.Deadline);
            await retiring.WaitAsync(NifNativeHarnessNames.Deadline);
            Require(allocations.PhysicalImageClosed528);
            fence = true;
            File.WriteAllText(Path.Combine(output, NifLinuxMutex795Names.PthreadA), NifLinuxMutex795AuthorityChecks.CheckedWitness(first, 2));
            File.WriteAllText(Path.Combine(output, NifLinuxMutex795Names.PthreadB), NifLinuxMutex795AuthorityChecks.CheckedWitness(second, 1));
        }
        catch (Exception error)
        {
            errors.Add(error.ToString());
        }
        finally
        {
            try
            {
                File.WriteAllText(release, string.Empty);
            }
            catch (Exception error)
            {
                errors.Add(error.ToString());
            }
            if (holder is not null) try
                {
                    await holder.WaitAsync(NifNativeHarnessNames.Deadline);
                }
                catch (Exception error)
                {
                    errors.Add(error.ToString());
                }
            if (retiring is not null) try
                {
                    await retiring.WaitAsync(NifNativeHarnessNames.Deadline);
                }
                catch (Exception error)
                {
                    errors.Add(error.ToString());
                }
            if (first is not null) try
                {
                    await Close(first);
                }
                catch (Exception error)
                {
                    errors.Add(error.ToString());
                }
            if (second is not null) try
                {
                    await Close(second);
                }
                catch (Exception error)
                {
                    errors.Add(error.ToString());
                }
        }
        bool clean = NifNativeCallEnvironment.ActiveCount == beforeEnvironments && NifNativeBinaryStorage.LiveBufferCount == beforeBuffers
            && NifNativeBinaryRegistry.NativeOwnedCount == beforeBinaries && NifNativeAllocationOwner.LiveMutexCount795 == beforeMutexes;
        try
        {
            VerifyInputs();
            Require(clean && policyCalls == (route == NifDirty521Names.RouteReviewed ? 2 : 0));
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
                    Passed = errors.Count == 0 && firstEqual && secondEqual && authority && fence && clean,
                    FirstWholeEqual = firstEqual,
                    SecondWholeEqual = secondEqual,
                    ActualVmAuthorityNegatives = authority,
                    RealNativeHolderRetirementFence = fence,
                    NativeBaselinesRestored = clean,
                    Route = route,
                    OwnedProfile = owned,
                    PolicyCalls = policyCalls,
                    AdmissionSha256 = args[1],
                    NormalizationPerformed = false,
                    FullTlsCompatibilityClaimed = false,
                    FullCompatibilityClaimed = false,
                    Errors = errors
                },
                new JsonSerializerOptions { WriteIndented = true }
            )
        );
        if (errors.Count != 0) throw new AggregateException(errors.Select(value => new InvalidOperationException(value)));
    }

    private static async Task<byte[]> Encode(ProcessContext context, Term value, bool deterministic)
    {
        Term minor = Term.Tuple(Term.A(NifDirty521Names.MinorVersionOption), Term.I(NifDirty521Names.SerializationMinorVersion));
        Term options = deterministic ? Term.List(Term.A(NifDirty521Names.DeterministicOption), minor) : Term.List(minor);

        return ((BitString)await context.Runtime.Modules.Call(
            context,
            ErlangModuleNames.Module,
            NifDirty521Names.EncodeFunction,
            value,
            options
        )).ToArray();
    }

    private static string Text(JsonElement root, string field) => root.GetProperty(field).GetString()!;

    private static void Verify(string path, string digest) => Require(Path.IsPathFullyQualified(path)
        && Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) == digest.ToUpperInvariant());

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(NifLinuxMutex795Names.Failure);
    }
}
