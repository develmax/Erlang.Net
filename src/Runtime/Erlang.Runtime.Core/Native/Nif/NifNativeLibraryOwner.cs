namespace Erlang;

internal sealed unsafe class NifNativeLibraryOwner : IAsyncDisposable
{
    private readonly object gate = new();
    private readonly ModuleCodeImage image;
    private readonly ProcessContext initializationContext;
    private readonly NifNativeAllocationOwner allocations = new();
    private readonly Dictionary<(string Name, int Arity), nint> functions = new();
    private NifNativeWindowsCallbacks? windowsCallbacks;
    private NifNativeLinuxCapsuleLease? linuxCapsule;
    private NifNativeAbiEntry entry;
    private nint library;
    private FileStream? libraryFile;
    private nint privateData;
    private bool initializationStarted;
    private bool reallocationEnabled;
    private bool initialized;
    private bool disposed;
    private Exception? disposalFailure;
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
            allocations.RegisterWindow434(image.Module);
            NifNativePlatformLoader.VerifyPlatform();
            try
            {
                libraryFile = profile.OpenLibraryFile();
            }
            catch (Exception openFailure) when (openFailure is FileNotFoundException or DirectoryNotFoundException ||
                profile.ExpectedLibrarySha256 is null && openFailure is (UnauthorizedAccessException or IOException))
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
                windowsCallbacks.BindIndependentEnvironments(initializationContext.Runtime, image);
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
            if (OperatingSystem.IsLinux())
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
            entry = NifNativeEntryReader.Read(entryPointer);
            ValidateEntry(descriptor);
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
                throw new NifNativeLoadVetoException(result);
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
    }

    public ValueTask DisposeAsync()
    {
        using var lifecycle = NifNativeLifecycleGate.Enter(CancellationToken.None);
        lock (gate)
        {
            if (disposed)
                return disposalFailure is null ? ValueTask.CompletedTask : ValueTask.FromException(disposalFailure);
            var retirementState = new NifNativeFailureState();
            using var retirementScope = new NifNativeCallbackScope(allocations, retirementState, reallocationEnabled);
            using var retirementEnvironment = new NifNativeCallEnvironment(
                initializationContext,
                image,
                privateData,
                retirementState
            );
            using var retirementInvocation = retirementScope.BindEnvironment(retirementEnvironment);
            disposed = true;
            Exception? failure = null;
            bool nativeClosed = false;
            try
            {
                if (initialized)
                {
                    initialized = false;
                    if (entry.Unload != 0)
                    {
                        ((delegate* unmanaged[Cdecl]<nint, nint, void>)entry.Unload)(retirementEnvironment.Pointer, privateData);
                        OutstandingAfterNativeUnload = allocations.Count;
                        retirementState.ThrowPending();
                    }
                }
            }
            catch (Exception unloadFailure)
            {
                failure = unloadFailure;
            }
            finally
            {
                try
                {
                    if (linuxCapsule is { } capsule)
                    {
                        capsule.Dispose();
                        linuxCapsule = null;
                    }
                    else
                    {
                        NifNativePlatformLoader.Close(library);
                    }
                    nativeClosed = true;
                    library = 0;
                    retirementState.ThrowPending();
                }
                catch (Exception closeFailure)
                {
                    failure = failure is null ? closeFailure : new AggregateException(failure, closeFailure);
                }
                if (nativeClosed)
                {
                    privateData = 0;
                    // Target ELF/CRT destructors finish before failed-init/veto containment.
                    try
                    {
                        allocations.ReclaimAll();
                    }
                    catch (Exception reclaimFailure)
                    {
                        failure = failure is null ? reclaimFailure : new AggregateException(failure, reclaimFailure);
                    }
                    var previousFile = libraryFile;
                    libraryFile = null;
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
                        allocations.TryEndDomainMembership();
                    }
                    catch (Exception membershipFailure)
                    {
                        failure = failure is null ? membershipFailure : new AggregateException(failure, membershipFailure);
                    }
                }
                else
                {
                    NifNativeLinuxNamespaceRetention.Retain(this);
                }
            }
            disposalFailure = failure;
            if (failure is not null)
                return ValueTask.FromException(failure);

            return ValueTask.CompletedTask;
        }
    }
}
