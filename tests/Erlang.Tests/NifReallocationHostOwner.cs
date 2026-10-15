using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal sealed class NifReallocationHostOwner
{
    private readonly object owner;
    private readonly object gate;
    private static readonly Type OwnerType = typeof(ProcessRuntime).Assembly.GetType(NifAllocationLedgerHostNames.OwnerType, true)!;

    internal NifReallocationHostOwner(object? actualOwner = null, object? libraryGate = null)
    {
        owner = actualOwner ?? Activator.CreateInstance(OwnerType, nonPublic: true)!;
        gate = libraryGate ?? new object();
    }

    internal static NifReallocationHostOwner Library(NifAllocationDomainHostBridge library)
        => new(
            typeof(NifAllocationDomainHostBridge).GetField(NifReallocationHostNames.Backend, NifReallocationHostNames.Members)!.GetValue(library)!,
            typeof(NifAllocationDomainHostBridge).GetField(NifAllocationDomainHostNames.Gate, NifReallocationHostNames.Members)!.GetValue(library)!
        );

    internal NifAllocationLedgerHostSnapshot Snapshot => ReadSnapshot(Property(owner, NifAllocationLedgerHostNames.Snapshot));

    internal nint Allocate(nuint size) => (nint)Call(NifReallocationHostNames.Allocate, size)!;

    internal nint Reallocate(nint pointer, nuint size) => (nint)Call(NifReallocationHostNames.Reallocate, pointer, size)!;

    internal void Free(nint pointer) => Call(NifReallocationHostNames.Free, pointer);

    internal void Reclaim() => Call(NifReallocationHostNames.Reclaim);

    internal static void Preflight()
    {
        NifAllocationLedgerHostOwner.Preflight();
        if (OwnerType.GetMethod(NifReallocationHostNames.Reallocate, NifReallocationHostNames.Members) is null)
            throw new InvalidOperationException(NifAllocationLedgerHostNames.ReflectionContract);
    }

    internal void SeedSyntheticSnapshot(NifAllocationLedgerHostSnapshot value) => Seed(owner, value);

    internal static object RuntimeDomain(ProcessRuntime runtime) => Property(runtime, NifReallocationHostNames.Domain);

    internal static NifAllocationLedgerHostSnapshot DomainSnapshot(object domain)
        => ReadSnapshot(Property(Property(domain, NifAllocationLedgerHostNames.Snapshot), nameof(NifAllocationDomainHostSnapshot.Allocations)));

    internal static void Seed(object target, NifAllocationLedgerHostSnapshot value)
    {
        FieldInfo field = target.GetType().GetField(NifReallocationHostNames.SnapshotStorage, NifReallocationHostNames.Members)!;
        var constructor = field.FieldType.GetConstructors(NifReallocationHostNames.Members).Single(item => item.GetParameters().Length == NifReallocationHostParameters.SnapshotFields);
        field.SetValue(target, constructor.Invoke(constructor.GetParameters().Select(parameter => Property(value, parameter.Name!)).ToArray()));
    }

    private static NifAllocationLedgerHostSnapshot ReadSnapshot(object value)
    {
        var constructor = typeof(NifAllocationLedgerHostSnapshot).GetConstructors(NifReallocationHostNames.Members).Single(item => item.GetParameters().Length == NifReallocationHostParameters.SnapshotFields);

        return (NifAllocationLedgerHostSnapshot)constructor.Invoke(constructor.GetParameters().Select(parameter => Property(value, parameter.Name!)).ToArray());
    }

    private static object Property(object target, string name)
        => target.GetType().GetProperty(name, NifReallocationHostNames.Members)!.GetValue(target)!;

    private object? Call(string method, params object[] arguments)
    {
        try
        {
            lock (gate)
                return OwnerType.GetMethod(method, NifReallocationHostNames.Members)!.Invoke(owner, arguments);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is { } error)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
            throw;
        }
    }
}
