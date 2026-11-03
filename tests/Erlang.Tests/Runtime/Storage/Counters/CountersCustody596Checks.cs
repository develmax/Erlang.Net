using System.Reflection;
using static Erlang.Tests.CountersCustody596Names;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

// Real backing lease/pin controls, not proof that a process header is wired.
internal static class CountersCustody596Checks
{
    internal static Task GuestRetirementIgnoresStaleClrEntry() => Run(async f =>
    {
        IDisposable guest = f.Lease(GuestKind);
        nint pointer = (nint)Call(f.Array, ReadBinary, guest)!;
        Check(Marshal.ReadInt64(pointer, RefCountOffset) == 1);
        f.ReleaseProducer();
        guest.Dispose();
        await ((Task)Property(f.Array, Retired)).WaitAsync(Deadline);
        Check(!(bool)Property(f.Array, Allocated));
        Check(f.Reference is ReferenceTerm); // CLR value deliberately remains strongly held.
        Check(Call(Property(f.Runtime, Resources), AcquireReference, f.Reference) is null);
        GC.KeepAlive(f.Reference);
    });

    internal static Task ActivePinOutlivesLastGuestRoot() => Run(async f =>
    {
        IDisposable guest = f.Lease(GuestKind);
        IDisposable operation = f.Pin();
        f.ReleaseProducer();
        guest.Dispose();
        Check((bool)Property(f.Array, Allocated) && !((Task)Property(f.Array, Retired)).IsCompleted);
        Check((long)Call(
            f.Array,
            Cell,
            0UL,
            0
        )! == 0);
        operation.Dispose();
        await ((Task)Property(f.Array, Retired)).WaitAsync(Deadline);
        Check(!(bool)Property(f.Array, Allocated));
    });

    internal static Task HostCustodyIndependentOfGuest() => Run(async f =>
    {
        IDisposable guest = f.Lease(GuestKind);
        IDisposable host = f.Host();
        f.ReleaseProducer();
        guest.Dispose();
        Check((bool)Property(f.Array, Allocated) && !((Task)Property(f.Array, Retired)).IsCompleted);
        using (IDisposable operation = f.Pin())
            Check((ulong)Property(f.Array, OriginalSize) == (ulong)Property(f.Array, TotalBytes) - BinaryPrefixBytes);
        host.Dispose();
        await ((Task)Property(f.Array, Retired)).WaitAsync(Deadline);
        Check(!(bool)Property(f.Array, Allocated));
    });

    internal static async Task Run(Func<CountersCustody596Fixture, Task> body)
    {
        var errors = new List<Exception>();
        CountersCustody596Fixture? fixture = null;
        try
        {
            fixture = new();
            await body(fixture);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            if (fixture is not null) await fixture.Drain(errors);
        }
        if (errors.Count == 1) ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1) throw new AggregateException(CleanupFailure, errors);
    }

    private static void Check(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
