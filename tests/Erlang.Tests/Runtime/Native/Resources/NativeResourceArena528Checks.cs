using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.NativeResourceArena528Names;

namespace Erlang.Tests;

internal static class NativeResourceArena528Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint resource);

    internal static async Task ArenaSurvivesImageCloseAndSupportsForeignFree()
    {
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.Ready;
            object arena = Property(runtime, Arena);
            object first = NativeResourceArena528Access.Owner(runtime);
            object second = NativeResourceArena528Access.Owner(runtime);
            object remainder = NativeResourceArena528Access.Owner(runtime);
            try
            {
                nint child = (nint)Call(first, "Allocate", (nuint)ChildBytes)!;
                Marshal.WriteByte(child, Marker);
                NativeResourceArena528Access.Close(first);
                NativeResource524Access.Check((int)Property(first, "Count") == 1 && Marshal.ReadByte(child) == Marker);
                bool held = false;
                try
                {
                    Call(arena, "ReclaimFinal528");
                }
                catch (InvalidOperationException)
                {
                    held = true;
                }
                NativeResource524Access.Check(held && (int)Property(first, "Count") == 1);
                nint local = (nint)Call(second, "Allocate", (nuint)LocalBytes)!;
                using (NativeResourceArena528Access.Scope(second))
                {
                    NativeResourceArena528Access.Free(child);
                    NativeResourceArena528Access.Free(local);
                }
                NativeResource524Access.Check((int)Property(first, "Count") == 0 && (int)Property(second, "Count") == 0);
                nint foreign = (nint)Call(second, "Allocate", (nuint)ForeignBytes)!;
                Exception? failure = null;
                var thread = new Thread(() =>
                {
                    try
                    {
                        NativeResourceArena528Access.Free(foreign);
                    }
                    catch (Exception error)
                    {
                        failure = error;
                    }
                });
                thread.Start();
                NativeResource524Access.Check(thread.Join(Deadline));
                NativeResource524Access.Check(failure is null && (int)Property(second, "Count") == 0);
                NativeResourceArena528Access.Close(second);
                nint leakedUntilVmExit = (nint)Call(remainder, "Allocate", (nuint)RemainderBytes)!;
                NativeResourceArena528Access.Close(remainder);
                NativeResource524Access.Check((int)Property(remainder, "Count") == 1);
                await runtime.DisposeAsync();
                NativeResource524Access.Check((int)Property(remainder, "Count") == 0);
                bool gone = false;
                try
                {
                    NativeResourceArena528Access.Static(PointerIndex, "Resolve528", leakedUntilVmExit);
                }
                catch (InvalidOperationException)
                {
                    gone = true;
                }
                NativeResource524Access.Check(gone);
                object snapshot = Property(arena, "Snapshot");
                NativeResource524Access.Check((int)Property(snapshot, "JoinedOwners") == 0
                    && (int)Property(Property(snapshot, "Allocations"), "Count") == 0);
            }
            finally
            {
                NativeResourceArena528Access.Close(first);
                NativeResourceArena528Access.Close(second);
                NativeResourceArena528Access.Close(remainder);
            }
        }
    }

    internal static async Task NewDestructorFreesOldImageChildAllocation()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.Ready;
        var first = NativeResource527Access.Pending(runtime);
        var next = NativeResource527Access.Pending(runtime);
        object allocatorA = NativeResourceArena528Access.ProviderOwner(runtime, first.Provider);
        object allocatorB = NativeResourceArena528Access.ProviderOwner(runtime, next.Provider);
        int oldCalls = 0, newCalls = 0;
        byte observed = 0;
        Exception? callbackFailure = null;
        Destructor oldDestructor = (_, _) => Interlocked.Increment(ref oldCalls);
        Destructor newDestructor = (_, resource) =>
        {
            try
            {
                nint child = Marshal.ReadIntPtr(resource);
                observed = Marshal.ReadByte(child);
                NativeResourceArena528Access.Free(child);
                Interlocked.Increment(ref newCalls);
            }
            catch (Exception error)
            {
                callbackFailure = error;
            }
        };
        IDisposable? root = null;
        try
        {
            object oldLoad = NativeResource527Access.Begin(runtime, first);
            using var oldTransaction = (IDisposable)oldLoad;
            var type = NativeResource527Access.Open(
                oldLoad,
                NativeResourceArena528Names.TypeName,
                1,
                Marshal.GetFunctionPointerForDelegate(oldDestructor)
            );
            NativeResource527Access.Publish(oldLoad, first);
            nint child = (nint)Call(allocatorA, "Allocate", (nuint)ChildBytes)!;
            Marshal.WriteByte(child, Marker);
            object payload = Call(
                Property(runtime, Domain),
                "Allocate524",
                type.Type!,
                (nuint)PayloadBytes
            )!;
            Marshal.WriteIntPtr((nint)Property(payload, "Pointer524"), child);
            root = (IDisposable)Call(payload, "RetainRoot524")!;
            Call(payload, "ReleaseNative524");
            object newLoad = NativeResource527Access.Begin(runtime, next);
            using var newTransaction = (IDisposable)newLoad;
            var replacement = NativeResource527Access.Open(
                newLoad,
                NativeResourceArena528Names.TypeName,
                2,
                Marshal.GetFunctionPointerForDelegate(newDestructor)
            );
            NativeResource524Access.Check(ReferenceEquals(type.Type, replacement.Type));
            NativeResource527Access.Publish(newLoad, next);
            await NativeResource527Access.Retire(runtime, first);
            NativeResourceArena528Access.Close(allocatorA);
            NativeResource524Access.Check(first.Owner.Disposed && (int)Property(allocatorA, "Count") == 1 && Marshal.ReadByte(child) == Marker);
            root.Dispose();
            await ((Task)Property(payload, "Retired524")).WaitAsync(Deadline);
            NativeResource524Access.Check(callbackFailure is null && observed == Marker && oldCalls == 0 && newCalls == 1
                && (int)Property(allocatorA, "Count") == 0);
            await NativeResource527Access.Retire(runtime, next);
            NativeResourceArena528Access.Close(allocatorB);
        }
        finally
        {
            root?.Dispose();
            NativeResourceArena528Access.Close(allocatorA);
            NativeResourceArena528Access.Close(allocatorB);
            GC.KeepAlive(oldDestructor);
            GC.KeepAlive(newDestructor);
        }
    }
}
