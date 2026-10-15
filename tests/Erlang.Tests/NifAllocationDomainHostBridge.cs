using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal sealed class NifAllocationDomainHostBridge : IAsyncDisposable
{
    private static readonly Assembly RuntimeAssembly = typeof(ProcessRuntime).Assembly;
    private static readonly Type LibraryType = Type(NifAllocationDomainHostNames.LibraryType);
    private static readonly Type ProfileType = Type(NifAllocationDomainHostNames.ProfileType);
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
        _ = LibraryType.GetMethod(NifAllocationDomainHostNames.Initialize, NifAllocationDomainHostNames.Members)
            ?? throw new InvalidOperationException(NifAllocationDomainHostNames.ReflectionContract);
    }

    internal static object Profile()
    {
        string source = (string)Type(NifAllocationDomainHostNames.NamesType).GetField(NifAllocationDomainHostNames.SourceHash, NifAllocationDomainHostNames.Statics)!.GetRawConstantValue()!;

        return Activator.CreateInstance(
            ProfileType,
            NifAllocationDomainHostNames.Members,
            null,
            [Path.GetFullPath(NifAllocationDomainHostNames.NeverOpenedLibrary), source, source, null, null],
            null
        )!;
    }

    internal void InitializeWrongModule(ModuleCodeDescriptor descriptor, CancellationToken cancellation = default)
    {
        try
        {
            Call(
                library,
                NifAllocationDomainHostNames.Initialize,
                Profile(),
                descriptor,
                Nil.Value,
                cancellation
            );
        }
        catch (NotSupportedException error) when (error.Message == (string)Type(NifAllocationDomainHostNames.DiagnosticsType).GetField(NifAllocationDomainHostNames.ModuleIdentity, NifAllocationDomainHostNames.Statics)!.GetRawConstantValue()!)
        {
            return;
        }
        throw new InvalidOperationException(NifAllocationDomainHostNames.UnexpectedInitialization);
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
        catch (NotSupportedException error) when (error.Message == (string)Type(NifAllocationDomainHostNames.DiagnosticsType).GetField(NifAllocationDomainHostNames.ModuleIdentity, NifAllocationDomainHostNames.Statics)!.GetRawConstantValue()!)
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
