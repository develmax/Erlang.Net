namespace Erlang;

internal sealed unsafe partial class NifNativeLibraryOwner : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly ModuleCodeImage image;
    private readonly ProcessContext initializationContext;
    private readonly NifNativeAllocationOwner allocations = new();
    private readonly Dictionary<(string Name, int Arity), NifNativeFunction521> functions = new();
    private NifNativeWindowsCallbacks? windowsCallbacks;
    private NifNativeLinuxCapsuleLease? linuxCapsule;
    private NifNativeAbiEntry entry;
    private nint library;
    private FileStream? libraryFile;
    private nint privateData;
    private bool initializationStarted;
    private NifNativeInitializationPhase616 phase616;
    private bool reallocationEnabled;
    private bool initialized;
    private bool disposed;
    private Exception? disposalFailure;
    internal bool RequiresUpgrade544 { private get; set; }
    internal NifNativeUpgradeOperation544? UpgradeOperation544 { get; set; }
    internal int OutstandingAfterNativeUnload { get; private set; } = -1;

    internal NifNativeLibraryOwner(ModuleCodeImage image, ProcessContext context)
    {
        this.image = image;
        initializationContext = context;
    }

    internal void OpenAndValidate616(
        NifNativeCapabilityProfile profile,
        ModuleCodeDescriptor descriptor,
        CancellationToken cancellation,
        NifNativeLifecyclePermit616 permit
    )
    {
        using var lifecycle = permit.Enter616();
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
            allocations.AttachImage528();
            allocations.RegisterWindow434(image.Module);
            NifNativePlatformLoader.VerifyPlatform();
            try
            {
                if (!OperatingSystem.IsWindows() && !NifNativePlatformPolicy.UseLinuxNamespace)
                    NifNativePortableRegistration.EnsureRegistered(initializationContext.Runtime, profile);
                libraryFile = profile.OpenLibraryFile();
            }
            catch (Exception openFailure) when (openFailure is FileNotFoundException or DirectoryNotFoundException ||
                profile.ExpectedLibrarySha256 is null && openFailure is (UnauthorizedAccessException or IOException)
                || openFailure is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException
                || openFailure is PlatformNotSupportedException && openFailure.Message is
                    NifNativePortableNames.ReviewedLoading or NifNativePortableNames.UnsupportedReallocation or NifNativeDiagnostics.ManagedLayout
                || openFailure is InvalidOperationException && openFailure.Message is
                    NifNativePortableNames.SingleRuntime or NifNativePortableNames.BridgeMismatch
                || !OperatingSystem.IsWindows() && !NifNativePlatformPolicy.UseLinuxNamespace
                    && openFailure is InvalidOperationException && openFailure.Message == NifNativeDiagnostics.BridgeContract)
            {
                throw new NifNativeLoadStageException(NifNativeDefaultLoaderNames.LoadFailed, openFailure);
            }
            if (OperatingSystem.IsWindows())
            {
                NifNativeExportRegistration.EnsureRegistered(profile);
                if (reallocationEnabled)
                    NifNativeReallocationExportRegistration.EnsureRegistered(profile);
            }
            if (OperatingSystem.IsWindows())
            {
                windowsCallbacks = new(reallocationEnabled);
                windowsCallbacks.BindMutexes795(allocations);
                windowsCallbacks.BindIndependentEnvironments(initializationContext.Runtime);
            }
            var failure = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(allocations, failure, reallocationEnabled);
            using var environment = new NifNativeCallEnvironment(
                initializationContext,
                image,
                0,
                failure
            );
            environment.AttachGuestRoots516(initializationContext);
            using var invocation = scope.BindEnvironment(environment);
            if (NifNativePlatformPolicy.UseLinuxNamespace)
            {
                try
                {
                    linuxCapsule = initializationContext.Runtime.OpenNativeLinuxCapsule(profile, image, libraryFile);
                    library = linuxCapsule.Pointer;
                }
                catch (Exception nativeFailure) when (nativeFailure is IOException or UnauthorizedAccessException or
                    DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
                {
                    throw new NifNativeLoadStageException(NifNativeDefaultLoaderNames.LoadFailed, nativeFailure);
                }
            }
            else
            {
                library = NifNativePlatformLoader.OpenNifLibrary(
                    libraryFile,
                    profile.NativeLibraryPath,
                    useDescriptor: profile.ExpectedLibrarySha256 is not null
                );
            }
            failure.ThrowPending();
            nint initialize = NifNativePlatformLoader.NifInitializer(library);
            initializationContext.Runtime.Modules.AddNifTaint(image.Module);
            nint entryPointer = OperatingSystem.IsWindows()
                ? ((delegate* unmanaged[Cdecl]<nint, nint>)initialize)(windowsCallbacks!.Pointer)
                : ((delegate* unmanaged[Cdecl]<nint>)initialize)();
            failure.ThrowPending();
            entry = NifNativeEntryReader.Read(entryPointer, image.Module);
            ValidateEntry(descriptor);
            phase616 = NifNativeInitializationPhase616.Opened;
        }
    }

    internal IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> InitializeLoad616(
        Term loadInfo,
        CancellationToken cancellation,
        NifNativeLifecyclePermit616 permit
    )
    {
        using var lifecycle = permit.Enter616();
        lock (gate)
        {
            if (disposed || RequiresUpgrade544 || phase616 != NifNativeInitializationPhase616.Opened)
                throw new InvalidOperationException(NifNativeTransition544Names.Admission);
            cancellation.ThrowIfCancellationRequested();
            var failure = new NifNativeFailureState();
            using var scope = new NifNativeCallbackScope(allocations, failure, reallocationEnabled);
            using var environment = new NifNativeCallEnvironment(
                initializationContext,
                image,
                0,
                failure
            );
            environment.AttachGuestRoots516(initializationContext);
            using var invocation = scope.BindEnvironment(environment);
            nuint info = environment.Add(loadInfo);
            phase616 = NifNativeInitializationPhase616.Invoking;
            int result = InvokeLoadCallback557(environment, info);
            if (result == 0)
            {
                initialized = true;
                phase616 = NifNativeInitializationPhase616.NativeSucceeded;
            }
            failure.ThrowPending();
            if (result != 0)
                throw new NifNativeLoadVetoException(result);
            cancellation.ThrowIfCancellationRequested();

            return PrepareBindings616();
        }
    }

    private IReadOnlyList<KeyValuePair<(string Name, int Arity), ErlangFunction>> PrepareBindings616()
    {
        var bindings = new List<KeyValuePair<(string Name, int Arity), ErlangFunction>>(functions.Count);
        foreach (var pair in functions)
            bindings.Add(new(pair.Key, BindFunction521(pair.Key, pair.Value)));
        SealGuestBindings535(bindings);

        return bindings.AsReadOnly();
    }

    internal void PrepareUpgrade616()
    {
        lock (gate)
        {
            if (disposed || !RequiresUpgrade544 || phase616 != NifNativeInitializationPhase616.Opened
                || UpgradeOperation544 is not { } operation || entry.Upgrade == 0)
                throw new NifImageAdmissionException(NifImageAdmissionFailure.UpgradeUnsupported);
            operation.Prepare544(this, PrepareBindings616(), resourceLoad527!);
            phase616 = NifNativeInitializationPhase616.Prepared;
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
        // The admitted image call now remains pinned through physical dirty settlement.
        // Do not serialize native bodies: same-owner status/release NIFs must run.
        nint data;
        lock (gate)
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(NifNativeLibraryOwner), NifNativeDiagnostics.DisposedLibrary);
            if (!ReferenceEquals(context.Runtime, initializationContext.Runtime))
                throw new InvalidOperationException(NifNativeDiagnostics.ForeignRuntime);
            if (arguments.Count != arity)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            context.Cancellation.ThrowIfCancellationRequested();
            data = privateData;
        }
        var failure = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(allocations, failure, reallocationEnabled);
        using var environment = new NifNativeCallEnvironment(
            context,
            image,
            data,
            failure
        );
        environment.AttachGuestRoots516(context);
        using var invocation = scope.BindEnvironment(environment);
        nuint[] handles = arguments.Select(environment.Add).ToArray();
        nuint returned;
        fixed (nuint* values = handles)
            returned = ((delegate* unmanaged[Cdecl]<nint, int, nuint*, nuint>)function)(environment.Pointer, handles.Length, values);
        // Pending exception wins even if native code returned an ordinary term.
        failure.ThrowPending();
        context.Cancellation.ThrowIfCancellationRequested();

        return environment.ExportResult516(returned);
    }

    private ValueTask DisposeSynchronous616(NifNativeLifecyclePermit616 permit)
    {
        var errors = new Exception?[NifNativePhaseNames616.RetirementFailureSlots];
        var lifecycle = permit.Enter616();
        lock (gate)
        {
            NifNativeCallbackScope? scope = null;
            NifNativeCallEnvironment? environment = null;
            NifNativeEnvironmentBinding? invocation = null;
            try
            {
                if (!disposed)
                {
                    try
                    {
                        RollbackResourceLoad527();
                    }
                    catch (Exception error)
                    {
                        errors[NifNativePhaseNames616.ResourceRollbackFailure] = error;
                    }
                    var state = new NifNativeFailureState();
                    scope = new(allocations, state, reallocationEnabled);
                    environment = new(
                        initializationContext,
                        image,
                        privateData,
                        state
                    );
                    invocation = scope.BindEnvironment(environment);
                    disposed = true;
                    phase616 = NifNativeInitializationPhase616.Closing;
                    bool failedInitialization795 = !initialized;
                    bool nativeClosed = false;
                    bool retainMutexCallbacks795 = false;
                    try
                    {
                        if (initialized)
                        {
                            initialized = false;
                            if (entry.Unload != 0)
                            {
                                ((delegate* unmanaged[Cdecl]<nint, nint, void>)entry.Unload)(environment.Pointer, privateData);
                                OutstandingAfterNativeUnload = allocations.Count;
                                state.ThrowPending();
                            }
                        }
                    }
                    catch (Exception error)
                    {
                        errors[NifNativePhaseNames616.UnloadFailure] = error;
                    }
                    try
                    {
                        if (linuxCapsule is { } capsule)
                        {
                            capsule.Dispose();
                            linuxCapsule = null;
                        }
                        else
                            NifNativePlatformLoader.Close(library);
                        nativeClosed = true;
                        library = 0;
                        state.ThrowPending();
                    }
                    catch (Exception error)
                    {
                        errors[NifNativePhaseNames616.LibraryCloseFailure] = error;
                    }
                    if (nativeClosed)
                    {
                        privateData = 0;
                        try
                        {
                            allocations.CloseImageMutexes795(failedInitialization795);
                        }
                        catch (Exception error)
                        {
                            retainMutexCallbacks795 = true;
                            errors[NifNativePhaseNames616.MutexCloseFailure] = error;
                        }
                        FileStream? previousFile = libraryFile;
                        libraryFile = null;
                        try
                        {
                            previousFile?.Dispose();
                        }
                        catch (Exception error)
                        {
                            errors[NifNativePhaseNames616.LibraryFileFailure] = error;
                        }
                        if (retainMutexCallbacks795)
                        {
                            try
                            {
                                NifNativeLinuxNamespaceRetention.Retain(this);
                            }
                            catch (Exception error)
                            {
                                errors[NifNativePhaseNames616.RetentionFailure] = error;
                            }
                        }
                        else
                        {
                            NifNativeWindowsCallbacks? previousCallbacks = windowsCallbacks;
                            windowsCallbacks = null;
                            try
                            {
                                previousCallbacks?.Dispose();
                            }
                            catch (Exception error)
                            {
                                errors[NifNativePhaseNames616.WindowsCallbacksFailure] = error;
                            }
                        }
                        functions.Clear();
                        try
                        {
                            allocations.TryEndDomainMembership();
                        }
                        catch (Exception error)
                        {
                            errors[NifNativePhaseNames616.DomainMembershipFailure] = error;
                        }
                    }
                    else
                    {
                        try
                        {
                            NifNativeLinuxNamespaceRetention.Retain(this);
                        }
                        catch (Exception error)
                        {
                            errors[NifNativePhaseNames616.RetentionFailure] = error;
                        }
                    }
                }
            }
            catch (Exception error)
            {
                errors[NifNativePhaseNames616.RetirementSetupFailure] = error;
            }
            finally
            {
                try
                {
                    invocation?.Dispose();
                }
                catch (Exception error)
                {
                    errors[NifNativePhaseNames616.InvocationDisposeFailure] = error;
                }
                try
                {
                    environment?.Dispose();
                }
                catch (Exception error)
                {
                    errors[NifNativePhaseNames616.EnvironmentDisposeFailure] = error;
                }
                try
                {
                    scope?.Dispose();
                }
                catch (Exception error)
                {
                    errors[NifNativePhaseNames616.ScopeDisposeFailure] = error;
                }
                try
                {
                    lifecycle.Dispose();
                }
                catch (Exception error)
                {
                    errors[NifNativePhaseNames616.LifecycleScopeFailure] = error;
                }
            }
            Exception[] failures = errors.Where(error => error is not null).Cast<Exception>().ToArray();
            if (failures.Length != 0)
            {
                Exception failure = failures.Length == 1 ? failures[0] : new AggregateException(failures);
                disposalFailure = disposalFailure is null ? failure : new AggregateException(disposalFailure, failure);
            }
            if (disposed)
                phase616 = library == 0 ? NifNativeInitializationPhase616.Closed : NifNativeInitializationPhase616.RetainedFailure;

            return disposalFailure is null ? ValueTask.CompletedTask : ValueTask.FromException(disposalFailure);
        }
    }
}
