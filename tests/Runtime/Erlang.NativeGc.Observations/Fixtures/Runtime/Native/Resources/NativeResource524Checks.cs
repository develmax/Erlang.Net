using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

internal static class NativeResource524Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    // Finite substrate control, not a genuine DSO/OTP compatibility test.
    internal static async Task ViewAndGuestCollection()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        object heap = Property(context, "TermHeap506");
        var provider = Provider(runtime);
        int destroyed = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref destroyed);
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        nint pointer = (nint)Property(payload, "Pointer524");
        for (int index = 0; index < PayloadBytes; index++) Marshal.WriteByte(pointer, index, (byte)index);
        BitString binary = Binary(runtime, payload);
        NativeResource524Access.Check(binary.ToArray().SequenceEqual(Enumerable.Range(0, PayloadBytes).Select(index => (byte)index)));
        // This proves a view of the same real allocation, not a byte[] snapshot.
        Marshal.WriteByte(pointer, 0, 231);
        NativeResource524Access.Check(binary.ToArray()[0] == 231);
        using var frame = (IDisposable)Call(heap, "Enter")!;
        int slot = (int)Call(frame, "Reserve")!;
        Call(
            frame,
            "Set",
            slot,
            binary
        );
        using (var enclosing = (IDisposable)Call(heap, ModuleLiteral801Names.Enter)!)
        {
            int enclosingSlot = (int)Call(enclosing, ModuleLiteral801Names.Reserve)!;
            Term actualBinary = (Term)Call(frame, ModuleLiteral801Names.ReadSlot, slot)!;
            TupleTerm actualParent = (TupleTerm)Call(
                enclosing,
                ModuleLiteral801Names.CreateTuple,
                enclosingSlot,
                (object)new Term[] { actualBinary }
            )!;
            NativeResource524Access.Check(!(bool)Property(Property(actualParent, ModuleLiteral801Names.Storage),
                ModuleLiteral801Names.LiteralOnly));
            NativeResource524Access.Check(!(bool)Property(Property(actualParent, ModuleLiteral801Names.Storage),
                ModuleLiteral801Names.ReadonlyCapture));
        }
        // Immediate replacement must still release the OLD frame backing via
        // normal publication; actual native headers remain until genuine GC.
        Call(
            frame,
            ModuleLiteral801Names.Set,
            slot,
            Term.I(ModuleLiteral801Names.ImmediateMarker)
        );
        NativeResource524Access.Check(((Term)Call(frame, ModuleLiteral801Names.ReadSlot, slot)!).Equals(Term.I(ModuleLiteral801Names.ImmediateMarker)));
        Call(payload, "ReleaseNative524");
        NativeResource524Access.Check(destroyed == 0);
        Call(frame, "Clear", slot);
        NativeResource524Access.Check(destroyed == 0);
        Call(heap, "Collect");
        await ((Task)Property(payload, "Retired524")).WaitAsync(Deadline);
        NativeResource524Access.Check(destroyed == 1);
        bool retired = false;
        try
        {
            _ = binary.ToArray();
        }
        catch (ObjectDisposedException)
        {
            retired = true;
        }
        NativeResource524Access.Check(retired); // The CLR wrapper is deliberately still strongly held.
        await ((Task)Call(provider.Bindings, "RetireAsync")!).WaitAsync(Deadline);
        NativeResource524Access.Check(provider.Owner.Disposed);
        GC.KeepAlive(destructor);
    }

    internal static async Task PhysicalDestructorPinsImage()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        var provider = Provider(runtime);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int sourceThread = Environment.CurrentManagedThreadId, callbackThread = 0;
        Destructor destructor = (_, _) =>
        {
            callbackThread = Environment.CurrentManagedThreadId;
            entered.Set();
            release.Wait(Deadline);
        };
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        var root = (IDisposable)Call(payload, "RetainRoot524")!;
        Task? retiring = null;
        try
        {
            Call(payload, "ReleaseNative524");
            retiring = (Task)Call(provider.Bindings, "RetireAsync")!;
            NativeResource524Access.Check(!retiring.IsCompleted && !provider.Owner.Disposed);
            root.Dispose();
            NativeResource524Access.Check(entered.Wait(Deadline));
            NativeResource524Access.Check(callbackThread != sourceThread && !retiring.IsCompleted && !provider.Owner.Disposed);
        }
        finally
        {
            root.Dispose();
            release.Set();
            await ((Task)Property(payload, "Retired524")).WaitAsync(Deadline);
            if (retiring is not null) await retiring.WaitAsync(Deadline);
            GC.KeepAlive(destructor);
        }
        NativeResource524Access.Check(provider.Owner.Disposed);
    }
}
