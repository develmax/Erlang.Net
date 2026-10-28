using System.Reflection;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

internal static class NativeResource527Checks
{
    private const string TypeName527 = "resource527_type";
    private const string RolledBack527 = "resource527_rolled_back";

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task PhysicalMagicHeaderAndIdentity()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        var provider = Provider(runtime);
        object payload = Allocate(runtime, provider.Provider, 0);
        nint user = (nint)Property(payload, "Pointer524");
        Type layout = typeof(ProcessRuntime).Assembly.GetType("Erlang.NativeResourceMagic527", true)!;
        int offset = (int)layout.GetProperty("UserOffset527", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        int binaryOffset = (int)layout.GetProperty("BinaryBytesOffset527", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        nint binary = user - offset;
        long allocated = (long)Property(payload, "AllocationBytes");
        NativeResource524Access.Check(Marshal.ReadInt64(binary, NativeResource527TestLayout.Flags) == 1);
        NativeResource524Access.Check(Marshal.ReadInt64(binary, NativeResource527TestLayout.ReferenceCount) == 1);
        NativeResource524Access.Check(Marshal.ReadInt64(binary, NativeResource527TestLayout.OriginalSize) == allocated - binaryOffset);
        var reference = (ReferenceTerm)Call(Property(runtime, "ReferenceResources"), "MakeNativeResource524", payload)!;
        NativeResource524Access.Check(unchecked((uint)Marshal.ReadInt32(binary, NativeResource527TestLayout.Reference0)) == reference.Words[0]);
        NativeResource524Access.Check(unchecked((uint)Marshal.ReadInt32(binary, NativeResource527TestLayout.Reference1)) == reference.Words[1]);
        NativeResource524Access.Check(unchecked((uint)Marshal.ReadInt32(binary, NativeResource527TestLayout.Reference2)) == reference.Words[2]);
        BitString view = Binary(runtime, payload);
        NativeResource524Access.Check((int)Property(view, "ReferencedByteSize") == allocated - binaryOffset);
        using var root = (IDisposable)Call(payload, "RetainRoot524")!;
        NativeResource524Access.Check(Marshal.ReadInt64(binary, NativeResource527TestLayout.ReferenceCount) == 2);
        Call(payload, "ReleaseNative524");
        NativeResource524Access.Check(Marshal.ReadInt64(binary, NativeResource527TestLayout.ReferenceCount) == 1);
        root.Dispose();
        await ((Task)Property(payload, "Retired524")).WaitAsync(Deadline);
        await ((Task)Call(provider.Bindings, "RetireAsync")!).WaitAsync(Deadline);
    }

    internal static async Task CreateDuplicateOrderAndRollback()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        var image = NativeResource527Access.Pending(runtime);
        object transaction = NativeResource527Access.Begin(runtime, image);
        using var transactionLease = (IDisposable)transaction;
        var first = NativeResource527Access.Open(transaction, TypeName527, 1);
        var second = NativeResource527Access.Open(transaction, TypeName527, 1);
        NativeResource524Access.Check(first.Type is not null && second.Type is not null && !ReferenceEquals(first.Type, second.Type));
        NativeResource524Access.Check(first.Tried == 1 && second.Tried == 1);
        NativeResource527Access.Publish(transaction, image);
        NativeResource524Access.Check(ReferenceEquals(first.Type, Call(
            NativeResource527Access.Table(runtime),
            "Find527",
            "resource527_protocol",
            TypeName527
        )));
        var rejected = NativeResource527Access.Open(transaction, TypeName527, 3);
        NativeResource524Access.Check(rejected.Type is null && rejected.Tried == 3);
        var image2 = NativeResource527Access.Pending(runtime);
        object rollback = NativeResource527Access.Begin(runtime, image2);
        using var rollbackLease = (IDisposable)rollback;
        var pending = NativeResource527Access.Open(rollback, RolledBack527, 1);
        NativeResource524Access.Check(pending.Type is not null);
        ((IDisposable)rollback).Dispose();
        NativeResource524Access.Check(Call(
            NativeResource527Access.Table(runtime),
            "Find527",
            "resource527_protocol",
            RolledBack527
        ) is null);
        ((IDisposable)image2.Reservation).Dispose();
        await ((Task)Call(image2.Bindings, "RetireAsync")!).WaitAsync(Deadline);
        await image2.Owner.DisposeAsync();
        await NativeResource527Access.Retire(runtime, image);
    }

    internal static async Task TakeoverUsesNewPhysicalDestructor()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        var first = NativeResource527Access.Pending(runtime);
        int oldCalls = 0, newCalls = 0;
        Destructor oldDestructor = (_, _) => Interlocked.Increment(ref oldCalls);
        Destructor newDestructor = (_, _) => Interlocked.Increment(ref newCalls);
        object oldLoad = NativeResource527Access.Begin(runtime, first);
        using var oldLoadLease = (IDisposable)oldLoad;
        var original = NativeResource527Access.Open(
            oldLoad,
            TypeName527,
            1,
            Marshal.GetFunctionPointerForDelegate(oldDestructor)
        );
        NativeResource527Access.Publish(oldLoad, first);
        object payload = Call(
            Property(runtime, Domain),
            "Allocate524",
            original.Type!,
            (nuint)PayloadBytes
        )!;
        using var root = (IDisposable)Call(payload, "RetainRoot524")!;
        Call(payload, "ReleaseNative524");
        var next = NativeResource527Access.Pending(runtime);
        object newLoad = NativeResource527Access.Begin(runtime, next);
        using var newLoadLease = (IDisposable)newLoad;
        var replacement = NativeResource527Access.Open(
            newLoad,
            TypeName527,
            2,
            Marshal.GetFunctionPointerForDelegate(newDestructor)
        );
        NativeResource524Access.Check(ReferenceEquals(original.Type, replacement.Type) && replacement.Tried == 2);
        NativeResource527Access.Publish(newLoad, next);
        await NativeResource527Access.Retire(runtime, first);
        NativeResource524Access.Check(oldCalls == 0 && newCalls == 0);
        root.Dispose();
        await ((Task)Property(payload, "Retired524")).WaitAsync(Deadline);
        NativeResource524Access.Check(oldCalls == 0 && newCalls == 1);
        await NativeResource527Access.Retire(runtime, next);
        GC.KeepAlive(oldDestructor);
        GC.KeepAlive(newDestructor);
    }
}
