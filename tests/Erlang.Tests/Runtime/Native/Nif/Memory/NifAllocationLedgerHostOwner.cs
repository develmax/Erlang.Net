using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Erlang.Tests;

internal sealed class NifAllocationLedgerHostOwner
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly Type OwnerType = typeof(ProcessRuntime).Assembly.GetType(NifAllocationLedgerHostNames.OwnerType, throwOnError: true)!;
    private static readonly PropertyInfo CountProperty = Property(OwnerType, NifAllocationLedgerHostNames.Count);
    private static readonly PropertyInfo SnapshotProperty = Property(OwnerType, NifAllocationLedgerHostNames.Snapshot);
    private static readonly FieldInfo SnapshotStorage = OwnerType.GetField(NifAllocationLedgerHostNames.SnapshotField, Members)
        ?? throw new InvalidOperationException(NifAllocationLedgerHostNames.ReflectionContract);
    private static readonly ConstructorInfo SnapshotConstructor = SnapshotProperty.PropertyType.GetConstructors(Members).Single(constructor => constructor.GetParameters().Length == NifAllocationLedgerHostParameters.SnapshotPropertyCount);
    private static readonly MethodInfo AllocateMethod = Method(NifAllocationLedgerHostNames.Allocate);
    private static readonly MethodInfo FreeMethod = Method(NifAllocationLedgerHostNames.Free);
    private static readonly MethodInfo ReclaimMethod = Method(NifAllocationLedgerHostNames.ReclaimAll);
    private readonly object owner = Activator.CreateInstance(OwnerType, nonPublic: true)!;

    internal int Count => (int)CountProperty.GetValue(owner)!;

    internal NifAllocationLedgerHostSnapshot Snapshot
    {
        get
        {
            object value = SnapshotProperty.GetValue(owner)!;

            return new(
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
        }
    }

    internal static void Preflight()
    {
        if (OwnerType.Assembly != typeof(ProcessRuntime).Assembly || OwnerType.Assembly.GetName().Name != NifAllocationLedgerHostNames.RuntimeAssembly || SnapshotStorage.FieldType != SnapshotProperty.PropertyType)
            throw new InvalidOperationException(NifAllocationLedgerHostNames.ReflectionContract);
        foreach (var property in typeof(NifAllocationLedgerHostSnapshot).GetProperties(Members))
            if (Property(SnapshotProperty.PropertyType, property.Name).PropertyType != property.PropertyType)
                throw new InvalidOperationException(NifAllocationLedgerHostNames.ReflectionContract);
    }

    internal nint Allocate(nuint size) => (nint)Invoke(AllocateMethod, size)!;

    internal void Free(nint pointer) => Invoke(FreeMethod, pointer);

    internal void ReclaimAll() => Invoke(ReclaimMethod);

    internal void SetSnapshotForOverflow(NifAllocationLedgerHostSnapshot value)
        => SnapshotStorage.SetValue(
            owner,
            SnapshotConstructor.Invoke([
            value.Count, value.RequestedBytes, value.BackingRequestBytes,
            value.PeakRequestedBytes, value.PeakBackingRequestBytes,
            value.AllocatedRequestedBytes, value.ReleasedRequestedBytes,
            value.AllocatedBackingRequestBytes, value.ReleasedBackingRequestBytes,
            value.SuccessfulAllocations, value.ReleasedAllocations
        ])
        );

    private static PropertyInfo Property(Type type, string name)
        => type.GetProperty(name, Members) ?? throw new InvalidOperationException(NifAllocationLedgerHostNames.ReflectionContract);

    private static MethodInfo Method(string name)
        => OwnerType.GetMethod(name, Members) ?? throw new InvalidOperationException(NifAllocationLedgerHostNames.ReflectionContract);

    private static T Read<T>(object value, string name) => (T)Property(value.GetType(), name).GetValue(value)!;

    private object? Invoke(MethodInfo method, params object[] arguments)
    {
        try
        {
            return method.Invoke(owner, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }
}
