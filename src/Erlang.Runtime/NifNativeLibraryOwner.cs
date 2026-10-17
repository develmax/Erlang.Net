namespace Erlang;

internal sealed unsafe class NifNativeLibraryOwner : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly ModuleCodeImage image;
    private readonly ProcessContext initializationContext;
    private readonly NifNativeAllocationOwner allocations = new();
    private readonly Dictionary<(string Name, int Arity), nint> functions = new();
    private NifNativeWindowsCallbacks? windowsCallbacks;
    private NifNativeAbiEntry entry;
    private nint library;
    private FileStream? verifiedFile;
    private nint privateData;
    private bool initializationStarted;
    private bool reallocationEnabled;
    private bool initialized;
    private bool disposed;
    internal int OutstandingAfterNativeUnload { get; private set; } = -1;

    internal NifNativeLibraryOwner(ModuleCodeImage image, ProcessContext context)
    {
        this.image = image;
        initializationContext = context;
    }

    internal IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> Initialize(
        NifNativeCapabilityProfile profile,
        ModuleCodeDescriptor descriptor,
        Term loadInfo,
        CancellationToken cancellation
    )
    {
        using var lifecycle = NifNativeLifecycleGate.Enter(cancellation);
        lock (gate)
        {
            cancellation.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(profile);
            if (disposed)
                throw new ObjectDisposedException(nameof(NifNativeLibraryOwner), NifNativeDiagnostics.DisposedLibrary);
            if (initializationStarted)
                throw new InvalidOperationException(NifNativeDiagnostics.RepeatedInitialization);
            initializationStarted = true;
            reallocationEnabled = profile.ReallocationEnabled;
            allocations.JoinDomain(initializationContext.Runtime.NativeAllocationDomain);
            NifNativePlatformLoader.VerifyPlatform();
            try
            {
                verifiedFile = NifNativeCapabilityProfile.OpenVerified(profile.LibraryPath, profile.LibrarySha256);
            }
            catch (Exception openFailure) when (openFailure is FileNotFoundException or DirectoryNotFoundException)
            {
                throw new NifNativeLoadStageException(NifNativeDefaultLoaderNames.LoadFailed, openFailure);
            }
            NifNativeExportRegistration.EnsureRegistered(profile);
            if (reallocationEnabled)
                NifNativeReallocationExportRegistration.EnsureRegistered(profile);
            if (OperatingSystem.IsWindows())
                windowsCallbacks = new(reallocationEnabled);
            library = NifNativePlatformLoader.OpenNifLibrary(verifiedFile, profile.LibraryPath);
            nint initialize = NifNativePlatformLoader.NifInitializer(library);
            var failure = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(allocations, failure, reallocationEnabled);
            initializationContext.Runtime.Modules.AddNifTaint(image.Module);
            nint entryPointer = OperatingSystem.IsWindows()
                ? ((delegate* unmanaged[Cdecl]<nint, nint>)initialize)(windowsCallbacks!.Pointer)
                : ((delegate* unmanaged[Cdecl]<nint>)initialize)();
            failure.ThrowPending();
            entry = NifNativeEntryReader.Read(entryPointer);
            ValidateEntry(descriptor);
            using var environment = new NifNativeCallEnvironment(
                initializationContext,
                image,
                0,
                failure
            );
            nuint info = environment.Add(loadInfo);
            nint data = 0;
            int result = entry.Load == 0
                ? 0 : ((delegate* unmanaged[Cdecl]<nint, nint*, nuint, int>)entry.Load)(environment.Pointer, &data, info);
            if (result == 0)
            {
                privateData = data;
                initialized = true;
            }
            failure.ThrowPending();
            if (result != 0)
                throw new InvalidOperationException(NifNativeDiagnostics.LoadVeto);
            cancellation.ThrowIfCancellationRequested();
            var bindings = new List<KeyValuePair<(string Name, int Arity), ErlangFunction>>();
            foreach (var pair in functions)
            {
                nint function = pair.Value;
                int arity = pair.Key.Arity;
                bindings.Add(new(pair.Key, (context, arguments) => ValueTask.FromResult(Invoke(
                    context,
                    arguments,
                    function,
                    arity
                ))));
            }

            return bindings.AsReadOnly();
        }
    }

    private void ValidateEntry(ModuleCodeDescriptor descriptor)
    {
        foreach (var function in NifNativeEntryFunctions.Read(entry, image.Module, descriptor))
            functions.Add(function.Key, function.Value);
    }

    private Term Invoke(
        ProcessContext context,
        IReadOnlyList<Term> arguments,
        nint function,
        int arity
    )
    {
        // Image dispatch owns the admitted NifImageCall; this gate also joins actual native frames.
        lock (gate)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(NifNativeLibraryOwner), NifNativeDiagnostics.DisposedLibrary);
            if (!ReferenceEquals(context.Runtime, initializationContext.Runtime))
                throw new InvalidOperationException(NifNativeDiagnostics.ForeignRuntime);
            if (arguments.Count != arity)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            context.Cancellation.ThrowIfCancellationRequested();
            var failure = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(allocations, failure, reallocationEnabled);
            using var environment = new NifNativeCallEnvironment(
                context,
                image,
                privateData,
                failure
            );
            nuint[] handles = arguments.Select(environment.Add).ToArray();
            nuint returned;
            fixed (nuint* values = handles)
                returned = ((delegate* unmanaged[Cdecl]<nint, int, nuint*, nuint>)function)(environment.Pointer, handles.Length, values);
            // Pending exception wins even if native code returned an ordinary term.
            failure.ThrowPending();
            context.Cancellation.ThrowIfCancellationRequested();

            return environment.Get(returned);
        }
    }

    public ValueTask DisposeAsync()
    {
        using var lifecycle = NifNativeLifecycleGate.Enter(CancellationToken.None);
        lock (gate)
        {
            if (disposed)
                return ValueTask.CompletedTask;
            disposed = true;
            Exception? failure = null;
            try
            {
                if (initialized)
                {
                    initialized = false;
                    if (entry.Unload != 0)
                    {
                        var state = new NifNativeFailureState();
                        using var scope = new NifNativeCallbackScope(allocations, state, reallocationEnabled);
                        using var environment = new NifNativeCallEnvironment(
                            initializationContext,
                            image,
                            privateData,
                            state
                        );
                        ((delegate* unmanaged[Cdecl]<nint, nint, void>)entry.Unload)(environment.Pointer, privateData);
                        OutstandingAfterNativeUnload = allocations.Count;
                        state.ThrowPending();
                    }
                }
            }
            catch (Exception error)
            {
                failure = error;
            }
            finally
            {
                privateData = 0;
                // Containment for failed init/veto; no assertion that arbitrary native resources are reclaimed.
                try
                {
                    allocations.ReclaimAll();
                }
                catch (Exception reclaimFailure)
                {
                    failure = failure is null ? reclaimFailure : new AggregateException(failure, reclaimFailure);
                }
                nint previous = library;
                library = 0;
                try
                {
                    NifNativePlatformLoader.Close(previous);
                }
                catch (Exception closeFailure)
                {
                    failure = failure is null ? closeFailure : new AggregateException(failure, closeFailure);
                }
                var previousFile = verifiedFile;
                verifiedFile = null;
                try
                {
                    previousFile?.Dispose();
                }
                catch (Exception fileFailure)
                {
                    failure = failure is null ? fileFailure : new AggregateException(failure, fileFailure);
                }
                var previousCallbacks = windowsCallbacks;
                windowsCallbacks = null;
                try
                {
                    previousCallbacks?.Dispose();
                }
                catch (Exception callbackFailure)
                {
                    failure = failure is null ? callbackFailure : new AggregateException(failure, callbackFailure);
                }
                functions.Clear();
                try
                {
                    // Failed reclamation must retain a nonempty membership and its actual bytes.
                    allocations.TryEndDomainMembership();
                }
                catch (Exception membershipFailure)
                {
                    failure = failure is null ? membershipFailure : new AggregateException(failure, membershipFailure);
                }
            }
            if (failure is not null)
                return ValueTask.FromException(failure);

            return ValueTask.CompletedTask;
        }
    }
}
