using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.NativeResourceLogical529Names;

namespace Erlang.Tests;

internal static class NativeResourceLogical529Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task UnlinkLivePayload()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        var first = NativeResource527Access.Pending(runtime);
        int calls = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref calls);
        using var load = (IDisposable)NativeResource527Access.Begin(runtime, first);
        var original = NativeResource527Access.Open(
            load,
            ResourceName,
            NativeResourceLogical529Names.Create,
            Marshal.GetFunctionPointerForDelegate(destructor)
        );
        NativeResource527Access.Publish(load, first);
        object payload = Call(
            Property(runtime, Domain),
            NativeResourceLogical529Names.Allocate,
            original.Type!,
            (nuint)PayloadBytes
        )!;
        Task retirement = (Task)Call(runtime.Modules, Retire, first.Image)!;
        try
        {
            // The physical worker may not have started; logical names must
            // already be gone when the actual registry retirement call returns.
            NativeResource524Access.Check(Call(
                NativeResource527Access.Table(runtime),
                Find,
                ModuleName,
                ResourceName
            ) is null);
            NativeResource524Access.Check(!retirement.IsCompleted && !first.Owner.Disposed && calls == 0);
            var next = NativeResource527Access.Pending(runtime);
            using var nextLoad = (IDisposable)NativeResource527Access.Begin(runtime, next);
            var created = NativeResource527Access.Open(nextLoad, ResourceName, CreateOrTakeover);
            NativeResource524Access.Check(created.Type is not null && created.Tried == Create
                && !ReferenceEquals(created.Type, original.Type));
            NativeResource527Access.Publish(nextLoad, next);
            NativeResource524Access.Check(ReferenceEquals(created.Type,
                Call(
                    NativeResource527Access.Table(runtime),
                    Find,
                    ModuleName,
                    ResourceName
                )));
            await NativeResource527Access.Retire(runtime, next);
            NativeResource524Access.Check(!first.Owner.Disposed);
        }
        finally
        {
            Call(payload, Release);
            await ((Task)Property(payload, Retired)).WaitAsync(Deadline);
            await retirement.WaitAsync(Deadline);
            GC.KeepAlive(destructor);
        }
        NativeResource524Access.Check(first.Owner.Disposed && calls == 1);
    }

    internal static async Task HeldCallbackPin()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        var first = NativeResource527Access.Pending(runtime);
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int oldCalls = 0, newCalls = 0;
        Destructor oldDestructor = (_, _) =>
        {
            entered.TrySetResult();
            release.Wait();
            Interlocked.Increment(ref oldCalls);
        };
        Destructor newDestructor = (_, _) => Interlocked.Increment(ref newCalls);
        using var oldLoad = (IDisposable)NativeResource527Access.Begin(runtime, first);
        var original = NativeResource527Access.Open(
            oldLoad,
            ResourceName,
            NativeResourceLogical529Names.Create,
            Marshal.GetFunctionPointerForDelegate(oldDestructor)
        );
        NativeResource527Access.Publish(oldLoad, first);
        object payload = Call(
            Property(runtime, Domain),
            NativeResourceLogical529Names.Allocate,
            original.Type!,
            (nuint)PayloadBytes
        )!;
        NativeResource527PendingImage? next = null;
        Task? publication = null, retirement = null;
        IDisposable? newLoadLease = null;
        try
        {
            Call(payload, Release);
            await entered.Task.WaitAsync(Deadline);
            next = NativeResource527Access.Pending(runtime);
            object newLoad = NativeResource527Access.Begin(runtime, next);
            newLoadLease = (IDisposable)newLoad;
            var replacement = NativeResource527Access.Open(
                newLoad,
                ResourceName,
                Takeover,
                Marshal.GetFunctionPointerForDelegate(newDestructor)
            );
            NativeResource524Access.Check(ReferenceEquals(original.Type, replacement.Type) && replacement.Tried == Takeover);
            NativeResource527PendingImage target = next;
            publication = Task.Run(() => NativeResource527Access.Publish(newLoad, target));
            // Publication must not wait for user C. The selected old callback
            // keeps its own actual image pin after its short reader exits.
            await publication.WaitAsync(Deadline);
            retirement = (Task)Call(runtime.Modules, Retire, first.Image)!;
            NativeResource524Access.Check(!retirement.IsCompleted && !first.Owner.Disposed);
            NativeResource524Access.Check(ReferenceEquals(replacement.Type,
                Call(
                    NativeResource527Access.Table(runtime),
                    Find,
                    ModuleName,
                    ResourceName
                )));
        }
        finally
        {
            release.Set();
            await ((Task)Property(payload, Retired)).WaitAsync(Deadline);
            if (publication is not null) await publication.WaitAsync(Deadline);
            retirement ??= (Task)Call(runtime.Modules, Retire, first.Image)!;
            await retirement.WaitAsync(Deadline);
            newLoadLease?.Dispose();
            if (next is not null)
            {
                ((IDisposable)next.Reservation).Dispose();
                await NativeResource527Access.Retire(runtime, next);
            }
            GC.KeepAlive(oldDestructor);
            GC.KeepAlive(newDestructor);
        }
        NativeResource524Access.Check(first.Owner.Disposed && oldCalls == 1 && newCalls == 0);
    }
}
