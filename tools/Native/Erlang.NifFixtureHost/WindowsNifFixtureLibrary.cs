using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Erlang;

namespace Erlang.NifFixtureHost;

internal sealed unsafe class WindowsNifFixtureLibrary : IDisposable
{
    internal const string ModuleName = NifFixtureNames.Module;
    private readonly NifGenerationLeaseTable generations;
    private readonly ModuleCodeImage image;
    private readonly ProcessContext caller;
    private readonly NifNativeAllocationOwner allocations = new();
    private readonly Dictionary<(string Name, int Arity), nint> functions = new();
    private readonly object nativeGate = new();
    private NifWindowsCallbacks? callbacks;
    private NifAbiEntry entry;
    private nint handle;
    private nint privateData;
    private bool initialized;
    internal int ObservedUnloadCount
    {
        get; private set;
    }
    internal int OutstandingAfterNativeUnload { get; private set; } = -1;
    internal int OwnedAllocationCount => allocations.Count;

    private WindowsNifFixtureLibrary(
        NifGenerationLeaseTable generations,
        ModuleCodeImage image,
        ProcessContext caller
    )
    {
        this.generations = generations;
        this.image = image;
        this.caller = caller;
    }

    internal static WindowsNifFixtureLibrary LoadAllowlistedFixture(
        string path,
        string trustedFixtureSha256,
        NifGenerationLeaseTable generations,
        ModuleCodeImage image,
        object sourceOwner,
        ProcessContext caller,
        Term loadInfo
    )
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            throw new PlatformNotSupportedException(NifHostDiagnostics.LibraryPlatform);
        string fullPath = Path.GetFullPath(path);
        if (!Path.IsPathFullyQualified(path) || !string.Equals(Path.GetExtension(fullPath), NifFixtureNames.LibraryExtension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(NifHostDiagnostics.AbsoluteLibraryPath, nameof(path));
        using var trustedFile = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
        );
        if (trustedFixtureSha256.Length != NifAbiLayout.HashHexCharacters || !Convert.ToHexString(SHA256.HashData(trustedFile)).Equals(trustedFixtureSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(NifHostDiagnostics.UntrustedLibraryHash);
        if (image.Module != ModuleName)
            throw new InvalidOperationException(NifHostDiagnostics.ModuleIdentity);
        var instance = new WindowsNifFixtureLibrary(generations, image, caller);
        generations.Bind(image, sourceOwner, instance.Unload);
        try
        {
            using var lease = generations.Acquire(caller, image);
            instance.Initialize(fullPath, loadInfo);

            return instance;
        }
        catch
        {
            generations.Retire(image);
            throw;
        }
    }

    private void Initialize(string path, Term loadInfo)
    {
        if (Marshal.SizeOf<NifAbiEntry>() != NifAbiLayout.EntryBytes || Marshal.SizeOf<NifAbiFunction>() != NifAbiLayout.FunctionBytes)
            throw new PlatformNotSupportedException(NifHostDiagnostics.ManagedLayout);
        callbacks = new();
        handle = NativeLibrary.Load(path);
        var initialize = (delegate* unmanaged[Cdecl]<nint, nint>)NativeLibrary.GetExport(handle, NifFixtureNames.Initializer);
        nint pointer = initialize(callbacks.Pointer);
        if (pointer == 0)
            throw new InvalidOperationException(NifHostDiagnostics.NullNativeEntry);
        entry = Marshal.PtrToStructure<NifAbiEntry>(pointer);
        if (entry.Major != NifAbiLayout.MajorVersion || entry.Minor != NifAbiLayout.MinorVersion || Marshal.PtrToStringUTF8(entry.Name) != ModuleName ||
            Marshal.PtrToStringUTF8(entry.Variant) != NifFixtureNames.Variant || entry.FunctionCount != NifFixtureNames.FunctionCount || entry.Functions == 0 ||
            entry.Load == 0 || entry.Unload == 0 || entry.Reload != 0 || entry.Upgrade != 0)
            throw new NotSupportedException(NifHostDiagnostics.UnsupportedNativeEntry);
        for (int index = 0; index < entry.FunctionCount; index++)
        {
            NifAbiFunction function = ((NifAbiFunction*)entry.Functions)[index];
            string name = Marshal.PtrToStringUTF8(function.Name) ?? throw new InvalidOperationException(NifHostDiagnostics.MissingFunctionName);
            if (function.Flags != 0 || function.Function == 0 ||
                !(name == NifFixtureNames.Increment && function.Arity == NifFixtureNames.IncrementArity || name == NifFixtureNames.Greeting && function.Arity == NifFixtureNames.GreetingArity))
                throw new NotSupportedException(NifHostDiagnostics.UntrustedFunction);
            functions.Add((name, checked((int)function.Arity)), function.Function);
        }
        using var scope = new NifCallbackScope(allocations);
        using var environment = new NifCallEnvironment(caller, image, 0);
        nuint info = environment.Add(loadInfo);
        nint data = 0;
        int result = ((delegate* unmanaged[Cdecl]<nint, nint*, nuint, int>)entry.Load)(environment.Pointer, &data, info);
        if (result == 0)
        {
            privateData = data;
            initialized = true;
        }
        NifCallbackScope.ThrowPending();
        environment.ThrowPending();
        if (result != 0)
            throw new InvalidOperationException(NifHostDiagnostics.LoadVeto);
        caller.Cancellation.ThrowIfCancellationRequested();
    }

    internal Term Invoke(
        ProcessContext context,
        string name,
        params Term[] arguments
    )
    {
        using var lease = generations.Acquire(context, image, requireCurrent: false);
        lock (nativeGate)
        {
            context.Cancellation.ThrowIfCancellationRequested();
            if (!functions.TryGetValue((name, arguments.Length), out nint function))
                throw new NotSupportedException(NifHostDiagnostics.MissingDispatch);
            using var scope = new NifCallbackScope(allocations);
            using var environment = new NifCallEnvironment(context, image, privateData);
            nuint[] handles = arguments.Select(environment.Add).ToArray();
            nuint result;
            fixed (nuint* values = handles)
                result = ((delegate* unmanaged[Cdecl]<nint, int, nuint*, nuint>)function)(environment.Pointer, handles.Length, values);
            NifCallbackScope.ThrowPending();
            environment.ThrowPending();
            context.Cancellation.ThrowIfCancellationRequested();

            return environment.Get(result);
        }
    }

    private void Unload()
    {
        lock (nativeGate)
        {
            try
            {
                if (initialized)
                {
                    initialized = false;
                    using var scope = new NifCallbackScope(allocations);
                    using var environment = new NifCallEnvironment(caller, image, privateData);
                    ((delegate* unmanaged[Cdecl]<nint, nint, void>)entry.Unload)(environment.Pointer, privateData);
                    OutstandingAfterNativeUnload = allocations.Count;
                    NifCallbackScope.ThrowPending();
                    environment.ThrowPending();
                    ObservedUnloadCount = ((delegate* unmanaged[Cdecl]<int>)NativeLibrary.GetExport(handle, NifFixtureNames.UnloadCounter))();
                }
            }
            finally
            {
                privateData = 0;
                allocations.ReclaimAll();
                if (handle != 0)
                    NativeLibrary.Free(handle);
                handle = 0;
                callbacks?.Dispose();
                callbacks = null;
                functions.Clear();
            }
        }
    }

    public void Dispose() => generations.Retire(image);
}
