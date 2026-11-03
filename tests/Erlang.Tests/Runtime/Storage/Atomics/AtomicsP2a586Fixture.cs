using System.Reflection;
using static Erlang.Tests.AtomicsP2a586Names;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

internal sealed class AtomicsP2a586Fixture
{
    private readonly List<IDisposable> leases = new();
    private IDisposable? invocation, producer;
    private object? owner, array, backing;
    internal ProcessRuntime Runtime { get; } = OwnedGuest506RuntimeFactory.Create();
    internal object Array => array!;
    internal ReferenceTerm Reference { get; private set; } = null!;

    internal AtomicsP2a586Fixture()
    {
        // Initialization is intentionally separate so cleanup is available even
        // when a reflection/schema check fails after native allocation.
    }

    private void Ensure()
    {
        if (array is not null) return;
        owner = Property(Runtime, Owner);
        invocation = (IDisposable)Call(owner, Enter)!;
        array = Call(
            owner,
            Create,
            1UL,
            true
        )!;
        Call(
            array,
            BindOwner,
            owner,
            Runtime
        );
        producer = (IDisposable)Call(array, RetainBacking, Kind(ProducerKind))!;
        backing = Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(RootType, true)!,
            Members,
            null,
            [owner, array],
            null
        )!;
        Reference = (ReferenceTerm)Call(Property(Runtime, Resources), CreateReference, backing)!;
    }

    internal IDisposable Lease(string kind)
    {
        Ensure();
        leases.EnsureCapacity(checked(leases.Count + 1));
        var lease = (IDisposable)Call(array!, RetainBacking, Kind(kind))!;
        leases.Add(lease);

        return lease;
    }

    internal IDisposable Host()
    {
        Ensure();
        leases.EnsureCapacity(checked(leases.Count + 1));
        var lease = (IDisposable)Call(backing!, RetainHost)!;
        leases.Add(lease);

        return lease;
    }

    internal IDisposable Pin()
    {
        Ensure();
        leases.EnsureCapacity(checked(leases.Count + 1));
        var lease = (IDisposable)Call(array!, AcquirePin)!;
        leases.Add(lease);

        return lease;
    }

    internal void ReleaseProducer()
    {
        producer?.Dispose();
        producer = null;
    }

    internal void ReleaseInvocationForShutdown()
    {
        invocation?.Dispose();
        invocation = null;
    }

    private static object Kind(string name) => Enum.Parse(typeof(ProcessRuntime).Assembly.GetType(KindType, true)!, name);

    internal async Task Drain(List<Exception> errors)
    {
        for (int i = leases.Count - 1; i >= 0; i--)
            try
            {
                leases[i].Dispose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        try
        {
            ReleaseProducer();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        try
        {
            invocation?.Dispose();
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        // Parent P1 VM close owns actual admission-stop/root/physical join. Do
        // not call synchronous owner.Close here or await it under runtime gate.
        try
        {
            await Runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        if (array is not null)
            try
            {
                await ((Task)Property(array, Retired)).WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
    }
}
