using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.NativeResourceUpgrade705Names;

namespace Erlang.Tests;

internal static class NativeResourceUpgrade705Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task PreadmittedAcrossDomainClose()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        object domain = Property(runtime, Domain);
        object table = NativeResource527Access.Table(runtime);
        var first = NativeResource527Access.Pending(runtime);
        using var firstLoad = (IDisposable)NativeResource527Access.Begin(runtime, first);
        int oldCalls = 0, newCalls = 0;
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Destructor oldDestructor = (_, _) => Interlocked.Increment(ref oldCalls);
        Destructor newDestructor = (_, _) =>
        {
            entered.TrySetResult();
            release.Wait();
            Interlocked.Increment(ref newCalls);
        };
        var original = NativeResource527Access.Open(
            firstLoad,
            NativeResourceUpgrade705Names.TypeName,
            Create,
            Marshal.GetFunctionPointerForDelegate(oldDestructor)
        );
        NativeResource524Access.Check(original.Type is not null);
        NativeResource527Access.Publish(firstLoad, first);
        object payload = Call(
            domain,
            AllocateMethod,
            original.Type!,
            (nuint)PayloadBytes
        )!;
        NativeResource527PendingImage? next = null;
        IDisposable? nextLoad = null;
        bool released = false, published = false;
        try
        {
            next = NativeResource527Access.Pending(runtime);
            nextLoad = (IDisposable)NativeResource527Access.Begin(runtime, next);
            // Admission is before the simulated callback opens; only the real
            // operation token survives close, not a second closing validation.
            object authority = Call(nextLoad, Prepare)!;
            var before = NativeResource527Access.Open(
                nextLoad,
                BeforeTypeName,
                Create,
                Marshal.GetFunctionPointerForDelegate(newDestructor)
            );
            NativeResource524Access.Check(before.Type is not null && before.Tried == Create);
            Task close = (Task)Call(domain, BeginClose)!;
            NativeResource524Access.Check(!close.IsCompleted && !next.Owner.Disposed);
            // These are native callback opens AFTER actual domain/table close.
            // They require this exact operation's pre-admitted snapshot/custody;
            // ordinary generic lookup/create must remain closed to new loads.
            var replacement = NativeResource527Access.Open(
                nextLoad,
                NativeResourceUpgrade705Names.TypeName,
                Takeover,
                Marshal.GetFunctionPointerForDelegate(newDestructor)
            );
            var empty = NativeResource527Access.Open(
                nextLoad,
                EmptyTypeName,
                Create,
                Marshal.GetFunctionPointerForDelegate(newDestructor)
            );
            NativeResource524Access.Check(ReferenceEquals(original.Type, replacement.Type) && replacement.Tried == Takeover);
            NativeResource524Access.Check(empty.Type is not null && empty.Tried == Create);
            using (var publication = (IDisposable)Call(authority, Acquire)!)
            {
                PublishOwner(next);
                published = true;
                Call(publication, Commit, true);
                CheckNoNames(table, first.Image);
                CheckNoNames(table, next.Image);
            }
            // Empty callback-bearing type must retire its own actual version pin;
            // the live payload keeps only the taken-over callback image alive.
            Task oldRetirement = (Task)Call(first.Bindings, Retire)!;
            await oldRetirement.WaitAsync(Deadline);
            NativeResource524Access.Check(first.Owner.Disposed && oldCalls == 0 && newCalls == 0);
            Task newRetirement = (Task)Call(next.Bindings, Retire)!;
            NativeResource524Access.Check(!newRetirement.IsCompleted && !next.Owner.Disposed && !close.IsCompleted);
            Call(payload, Release);
            released = true;
            await entered.Task.WaitAsync(Deadline);
            NativeResource524Access.Check(oldCalls == 0 && newCalls == 0 && !next.Owner.Disposed
                && !newRetirement.IsCompleted && !close.IsCompleted);
            release.Set();
            await ((Task)Property(payload, Retired)).WaitAsync(Deadline);
            await newRetirement.WaitAsync(Deadline);
            await close.WaitAsync(Deadline);
            NativeResource524Access.Check(next.Owner.Disposed && oldCalls == 0 && newCalls == 1);
            CheckDrained(domain);
        }
        finally
        {
            release.Set();
            if (!released) Call(payload, Release);
            nextLoad?.Dispose();
            if (next is not null && !published)
            {
                await next.Owner.DisposeAsync();
                ((IDisposable)next.Reservation).Dispose();
            }
            await ((Task)Property(payload, Retired)).WaitAsync(Deadline);
            await ((Task)Call(first.Bindings, Retire)!).WaitAsync(Deadline);
            if (next is not null)
                await ((Task)Call(next.Bindings, Retire)!).WaitAsync(Deadline);
            GC.KeepAlive(oldDestructor);
            GC.KeepAlive(newDestructor);
        }
    }

    internal static async Task CloseBeforeAdmissionRejected()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        object domain = Property(runtime, Domain);
        object table = NativeResource527Access.Table(runtime);
        var image = NativeResource527Access.Pending(runtime);
        using var transaction = (IDisposable)NativeResource527Access.Begin(runtime, image);
        bool rejected = false;
        try
        {
            var opened = NativeResource527Access.Open(transaction, EmptyTypeName, Create);
            NativeResource524Access.Check(opened.Type is not null);
            Task close = (Task)Call(domain, BeginClose)!;
            NativeResource524Access.Check(!close.IsCompleted);
            try
            {
                Call(transaction, Prepare);
            }
            catch (ObjectDisposedException)
            {
                rejected = true;
            }
            NativeResource524Access.Check(rejected && !close.IsCompleted);
            transaction.Dispose();
            await image.Owner.DisposeAsync();
            ((IDisposable)image.Reservation).Dispose();
            await ((Task)Call(image.Bindings, Retire)!).WaitAsync(Deadline);
            await close.WaitAsync(Deadline);
            CheckNoNames(table, image.Image);
            CheckDrained(domain);
        }
        finally
        {
            transaction.Dispose();
            await image.Owner.DisposeAsync();
            ((IDisposable)image.Reservation).Dispose();
        }
    }

    private static void PublishOwner(NativeResource527PendingImage image)
        => Call(
            image.Bindings,
            Publish,
            image.Reservation,
            System.Array.Empty<KeyValuePair<(string Name, int Arity), ErlangFunction>>(),
            image.Owner
        );

    private static void CheckNoNames(object table, object image)
    {
        Call(table, EnterNames);
        try
        {
            NativeResource524Access.Check(((System.Array)Call(table, Nodes, image)!).Length == 0);
        }
        finally
        {
            Call(table, LeaveNames);
        }
    }

    private static void CheckDrained(object domain)
    {
        object snapshot = Call(domain, Snapshot)!;
        NativeResource524Access.Check((long)Property(snapshot, LiveBytes) == 0 && (int)Property(snapshot, LiveResources) == 0
            && (int)Property(snapshot, Pending) == 0 && (bool)Property(snapshot, Joined)
            && (bool)Property(snapshot, Succeeded) && Property(snapshot, Failure) is null);
    }
}
