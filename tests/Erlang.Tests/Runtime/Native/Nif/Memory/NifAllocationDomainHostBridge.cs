using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Erlang.Tests;

internal sealed class NifAllocationDomainHostBridge : IAsyncDisposable
{
    private static readonly Assembly RuntimeAssembly = typeof(ProcessRuntime).Assembly;
    private static readonly Type LibraryType = Type(NifAllocationDomainHostNames.LibraryType);
    private static readonly Type ProfileType = Type(NifAllocationDomainHostNames.ProfileType);
    private static readonly Type LifecycleGateType = Type(NifAllocationDomainHostNames.LifecycleGateType);
    private readonly object library;
    private readonly object backend;
    private readonly object gate;

    internal NifAllocationDomainHostBridge(ModuleCodeImage image, ProcessContext context)
    {
        library = Activator.CreateInstance(
            LibraryType,
            NifAllocationDomainHostNames.Members,
            null,
            [image, context],
            null
        )!;
        backend = Field(NifAllocationDomainHostNames.Allocations).GetValue(library)!;
        gate = Field(NifAllocationDomainHostNames.Gate).GetValue(library)!;
    }

    internal static void Preflight()
    {
        NifAllocationLedgerHostOwner.Preflight();
        _ = Property(typeof(ProcessRuntime), NifAllocationDomainHostNames.Domain);
        var snapshot = Property(typeof(ProcessRuntime), NifAllocationDomainHostNames.RuntimeSnapshot).PropertyType;
        _ = Property(snapshot, NifAllocationDomainHostNames.JoinedOwners);
        _ = Property(snapshot, nameof(NifAllocationDomainHostSnapshot.Allocations));
        _ = Field(NifAllocationDomainHostNames.Disposed);
        _ = LibraryType.GetMethod(NifAllocationDomainHostNames.OpenAndValidate, NifAllocationDomainHostNames.Members)
            ?? throw new InvalidOperationException(NifAllocationDomainHostNames.ReflectionContract);
        _ = LibraryType.GetMethod(NifAllocationDomainHostNames.InitializeLoad, NifAllocationDomainHostNames.Members)
            ?? throw new InvalidOperationException(NifAllocationDomainHostNames.ReflectionContract);
        _ = LifecycleGateType.GetMethod(NifAllocationDomainHostNames.Acquire, NifAllocationDomainHostNames.Statics)
            ?? throw new InvalidOperationException(NifAllocationDomainHostNames.ReflectionContract);
    }

    internal static object Profile()
    {
        if (File.Exists(Path.GetFullPath(NifAllocationDomainHostNames.NeverOpenedLibrary)))
            throw new InvalidOperationException(NifAllocationDomainHostNames.UnexpectedInitialization);
        string source = (string)Type(NifAllocationDomainHostNames.NamesType).GetField(NifAllocationDomainHostNames.SourceHash, NifAllocationDomainHostNames.Statics)!.GetRawConstantValue()!;

        return Activator.CreateInstance(
            ProfileType,
            NifAllocationDomainHostNames.Members,
            null,
            [Path.GetFullPath(NifAllocationDomainHostNames.NeverOpenedLibrary), source, source, null, null],
            null
        )!;
    }

    internal async Task InitializeWrongModule(ModuleCodeDescriptor descriptor, CancellationToken cancellation = default)
    {
        try
        {
            MethodInfo acquire = LifecycleGateType.GetMethod(
                NifAllocationDomainHostNames.Acquire,
                NifAllocationDomainHostNames.Statics
            )!;
            // Allocate and validate the adapter before the real grant exists.
            var admission = new NifAllocationDomainHostAdmission628(acquire.ReturnType);
            object?[] arguments = [cancellation];
            admission.Bind(Invoke(acquire, null, arguments)!);
            // Forward the actual admission awaiter: its GetResult claims the
            // permit on this continuation, with no intermediary Task owner.
            using IDisposable permit = await admission;
            Call(
                library,
                NifAllocationDomainHostNames.OpenAndValidate,
                Profile(),
                descriptor,
                cancellation,
                permit
            );
            Call(
                library,
                NifAllocationDomainHostNames.InitializeLoad,
                Nil.Value,
                cancellation,
                permit
            );
        }
        catch (InvalidOperationException error) when (IsMissingLibrary(error))
        {
            ValidateWrongEntryModule(descriptor);

            return;
        }
        throw new InvalidOperationException(NifAllocationDomainHostNames.UnexpectedInitialization);
    }

    private static bool IsMissingLibrary(InvalidOperationException error) =>
        error.GetType() == Type(NifAllocationDomainHostNames.LoadStageType) &&
        (string)Property(error.GetType(), NifAllocationDomainHostNames.Category).GetValue(error)! == NifAllocationDomainHostNames.LoadFailed &&
        error.InnerException is FileNotFoundException or DirectoryNotFoundException;

    private void ValidateWrongEntryModule(ModuleCodeDescriptor descriptor)
    {
        Type entryType = Type(NifAllocationDomainHostNames.EntryType);
        object entry = Activator.CreateInstance(entryType)!;
        nint name = Marshal.StringToCoTaskMemUTF8(NifAllocationDomainHostNames.WrongEntryModule);
        try
        {
            entryType.GetField(NifAllocationDomainHostNames.EntryName, NifAllocationDomainHostNames.Members)!.SetValue(entry, name);
            Field(NifAllocationDomainHostNames.Entry).SetValue(library, entry);
            try
            {
                Call(library, NifAllocationDomainHostNames.ValidateEntry, descriptor);
            }
            catch (NotSupportedException failure) when (failure.Message == (string)Type(NifAllocationDomainHostNames.DiagnosticsType).GetField(NifAllocationDomainHostNames.ModuleIdentity, NifAllocationDomainHostNames.Statics)!.GetRawConstantValue()!)
            {
                return;
            }
            throw new InvalidOperationException(NifAllocationDomainHostNames.UnexpectedInitialization);
        }
        finally
        {
            Field(NifAllocationDomainHostNames.Entry).SetValue(library, Activator.CreateInstance(entryType));
            Marshal.FreeCoTaskMem(name);
        }
    }

    internal nint Allocate(nuint size)
    {
        lock (gate)
            return (nint)Call(backend, NifAllocationDomainHostNames.Allocate, size)!;
    }

    internal void Free(nint pointer)
    {
        lock (gate)
            if (!(bool)Field(NifAllocationDomainHostNames.Disposed).GetValue(library)!)
                Call(backend, NifAllocationDomainHostNames.Free, pointer);
    }

    internal NifAllocationLedgerHostSnapshot OwnerSnapshot => ReadAllocation(Property(backend.GetType(), NifAllocationDomainHostNames.Snapshot).GetValue(backend)!);

    internal static NifAllocationDomainHostSnapshot Snapshot(ProcessRuntime runtime)
    {
        object value = Property(typeof(ProcessRuntime), NifAllocationDomainHostNames.RuntimeSnapshot).GetValue(runtime)!;

        return new(
            Read<int>(value, NifAllocationDomainHostNames.JoinedOwners),
            ReadAllocation(Property(value.GetType(), nameof(NifAllocationDomainHostSnapshot.Allocations)).GetValue(value)!)
        );
    }

    internal static async Task InitializeReservation(object reservation)
    {
        object pending = Invoke(
            Type(NifAllocationDomainHostNames.InitializerType).GetMethod(NifAllocationDomainHostNames.Initializer, NifAllocationDomainHostNames.Statics)!,
            null,
            [reservation, Profile(), Nil.Value, CancellationToken.None]
        )!;
        Task task = (Task)Call(pending, NifAdmissionHostNames.AsTask)!;
        try
        {
            await task.WaitAsync(NifAllocationDomainHostNames.Deadline);
        }
        catch (InvalidOperationException error) when (IsMissingLibrary(error))
        {
            return;
        }
        throw new InvalidOperationException(NifAllocationDomainHostNames.UnexpectedInitialization);
    }

    public ValueTask DisposeAsync() => ((IAsyncDisposable)library).DisposeAsync();

    private static NifAllocationLedgerHostSnapshot ReadAllocation(object value) => new(
        Read<int>(value, nameof(NifAllocationLedgerHostSnapshot.Count)),
        Read<ulong>(value, nameof(NifAllocationLedgerHostSnapshot.RequestedBytes)),
        Read<ulong>(value, nameof(NifAllocationLedgerHostSnapshot.BackingRequestBytes)),
        Read<ulong>(value, nameof(NifAllocationLedgerHostSnapshot.PeakRequestedBytes)),
        Read<ulong>(value, nameof(NifAllocationLedgerHostSnapshot.PeakBackingRequestBytes)),
        Read<ulong>(value, nameof(NifAllocationLedgerHostSnapshot.AllocatedRequestedBytes)),
        Read<ulong>(value, nameof(NifAllocationLedgerHostSnapshot.ReleasedRequestedBytes)),
        Read<ulong>(value, nameof(NifAllocationLedgerHostSnapshot.AllocatedBackingRequestBytes)),
        Read<ulong>(value, nameof(NifAllocationLedgerHostSnapshot.ReleasedBackingRequestBytes)),
        Read<long>(value, nameof(NifAllocationLedgerHostSnapshot.SuccessfulAllocations)),
        Read<long>(value, nameof(NifAllocationLedgerHostSnapshot.ReleasedAllocations))
    );

    private static Type Type(string name) => RuntimeAssembly.GetType(name, throwOnError: true)!;

    private static FieldInfo Field(string name) => LibraryType.GetField(name, NifAllocationDomainHostNames.Members) ?? throw new InvalidOperationException(NifAllocationDomainHostNames.ReflectionContract);

    private static PropertyInfo Property(Type type, string name) => type.GetProperty(name, NifAllocationDomainHostNames.Members) ?? throw new InvalidOperationException(NifAllocationDomainHostNames.ReflectionContract);

    private static T Read<T>(object value, string name) => (T)Property(value.GetType(), name).GetValue(value)!;

    private static object? Call(object target, string name, params object?[] arguments) => Invoke(target.GetType().GetMethod(name, NifAllocationDomainHostNames.Members)!, target, arguments);

    private static object? Invoke(MethodInfo method, object? target, object?[] arguments)
    {
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } failure)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
            throw;
        }
    }
}
