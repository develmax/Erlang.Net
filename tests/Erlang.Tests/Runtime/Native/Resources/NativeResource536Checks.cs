using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.NativeResource536Access;

namespace Erlang.Tests;

internal static class NativeResource536Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task GuestGcUsesActualNormalAuxiliaryOnce()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        var provider = Provider(runtime);
        int calls = 0, kind = -1, lane = 0, thread = 0;
        bool selected = false;
        Exception? observation = null;
        Destructor callback = (_, _) =>
        {
            try
            {
                kind = ThreadType();
                lane = AuxiliaryLane();
                thread = Environment.CurrentManagedThreadId;
                selected = ActualSelectedAuxiliary(runtime, lane);
                Interlocked.Increment(ref calls);
            }
            catch (Exception error)
            {
                observation = error;
            }
        };
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(callback));
        int broker = BrokerThread(runtime);
        BitString binary = Binary(runtime, payload);
        try
        {
            await actor.Invoke(context =>
 {
     ReleaseViaGuestGc(context, payload, binary);

     return true;
 });
            await ((Task)Property(payload, NativeResource536TestNames.Retired)).WaitAsync(Deadline);
            NativeResource524Access.Check(observation is null && calls == 1 && kind == NativeResource536TestNames.NormalThread
                && lane > 0 && selected && thread != broker);
            NativeResource524Access.Check(ThreadType() == NativeResource536TestNames.ForeignThread && AuxiliaryLane() == 0);
        }
        finally
        {
            await ((Task)Call(provider.Bindings, NativeResource536TestNames.RetireImage)!).WaitAsync(Deadline);
            GC.KeepAlive(callback);
        }
    }

    internal static async Task NonGcRouteIsNotSilentlyReclassified()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        var provider = Provider(runtime);
        int kind = -1, lane = -1, calls = 0;
        Destructor callback = (_, _) =>
        {
            kind = ThreadType();
            lane = AuxiliaryLane();
            Interlocked.Increment(ref calls);
        };
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(callback));
        var root = (IDisposable)Call(payload, NativeResource536TestNames.RetainRoot)!;
        try
        {
            Call(payload, NativeResource536TestNames.ReleaseNative);
            root.Dispose();
            await ((Task)Property(payload, NativeResource536TestNames.Retired)).WaitAsync(Deadline);
            NativeResource524Access.Check(calls == 1 && kind == NativeResource536TestNames.ForeignThread && lane == 0);
        }
        finally
        {
            root.Dispose();
            await ((Task)Call(provider.Bindings, NativeResource536TestNames.RetireImage)!).WaitAsync(Deadline);
            GC.KeepAlive(callback);
        }
    }

    internal static async Task HeldNormalDestructorKeepsImageAndClosePhysical()
    {
        var runtime = OwnedGuest506RuntimeFactory.Create();
        var actor = new RuntimeNodeFoundationHostActor(runtime);
        var provider = Provider(runtime);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int kind = -1, calls = 0;
        bool released = false;
        Destructor callback = (_, _) =>
        {
            kind = ThreadType();
            entered.Set();
            released = release.Wait(Deadline);
            Interlocked.Increment(ref calls);
        };
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(callback));
        BitString binary = Binary(runtime, payload);
        Task? closing = null, retiring = null;
        try
        {
            await actor.Invoke(context =>
 {
     ReleaseViaGuestGc(context, payload, binary);

     return true;
 });
            NativeResource524Access.Check(entered.Wait(Deadline));
            retiring = (Task)Call(provider.Bindings, NativeResource536TestNames.RetireImage)!;
            NativeResource524Access.Check(!retiring.IsCompleted && !provider.Owner.Disposed);
            closing = runtime.DisposeAsync().AsTask();
            NativeResource524Access.Check(!closing.IsCompleted && !provider.Owner.Disposed);
        }
        finally
        {
            release.Set();
            await ((Task)Property(payload, NativeResource536TestNames.Retired)).WaitAsync(Deadline);
            if (retiring is not null) await retiring.WaitAsync(Deadline);
            if (closing is not null) await closing.WaitAsync(Deadline);
            await runtime.DisposeAsync();
            try
            {
                await actor.DisposeAsync();
            }
            catch (OperationCanceledException) { }
            GC.KeepAlive(callback);
        }
        NativeResource524Access.Check(released && calls == 1 && kind == NativeResource536TestNames.NormalThread && provider.Owner.Disposed);
    }
}
