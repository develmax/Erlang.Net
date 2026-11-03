using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;

namespace Erlang.Tests;

internal static class NativeCons539ResourceChecks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task CopiedCellKeepsResourceUntilLastEnvironmentClear()
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create();
        var provider = Provider(runtime);
        int destroyed = 0;
        Destructor callback = (_, _) => Interlocked.Increment(ref destroyed);
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(callback));
        nint bytes = (nint)Property(payload, NativeCons539ResourceNames.ResourcePointer);
        for (int index = 0; index < PayloadBytes; index++) Marshal.WriteByte(bytes, index, 90);
        BitString binary = Binary(runtime, payload);
        object image = Property(provider.Provider, NativeCons539ResourceNames.Image);
        using var first = (IDisposable)New(NativeCons539ResourceNames.Environment, runtime, image);
        using var second = (IDisposable)New(NativeCons539ResourceNames.Environment, runtime, image);
        using var table = (IDisposable)New(NativeCons539ResourceNames.Table);
        Call(
            table,
            NativeCons539ResourceNames.Bind,
            runtime,
            image
        );
        nint callbacks = (nint)Property(table, NativeCons539ResourceNames.Pointer);
        nuint head = (nuint)Call(first, NativeCons539ResourceNames.Add, binary)!;
        nuint tail = (nuint)Call(first, NativeCons539ResourceNames.Add, Term.I(539))!;
        bool nativeReleased = false;
        Exception? failure = null;
        try
        {
            nuint cell = Make(
                callbacks,
                first,
                head,
                tail
            );
            NativeResource524Access.Check(cell != 0);
            Call(payload, NativeCons539ResourceNames.ReleaseNative);
            nativeReleased = true;
            nuint copy = Copy(callbacks, second, cell);
            NativeResource524Access.Check(copy != 0 && destroyed == 0);
            Call(first, NativeCons539ResourceNames.Clear);
            var copied = (Cons)Call(second, NativeCons539ResourceNames.Get, copy)!;
            NativeResource524Access.Check(copied.Head is BitString actual && actual.ToArray().SequenceEqual(Enumerable.Repeat((byte)90, PayloadBytes)) && copied.Tail.Equals(Term.I(539)));
            NativeResource524Access.Check(destroyed == 0);
            Call(second, NativeCons539ResourceNames.Clear);
            await ((Task)Property(payload, NativeCons539ResourceNames.Retired)).WaitAsync(Deadline);
            NativeResource524Access.Check(destroyed == 1);
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            if (!nativeReleased)
                try
                {
                    Call(payload, NativeCons539ResourceNames.ReleaseNative);
                }
                catch (Exception error)
                {
                    failure = Combine(failure, error);
                }
            foreach (object env in new object[] { first, second })
                try
                {
                    Call(env, NativeCons539ResourceNames.Clear);
                }
                catch (Exception error)
                {
                    failure = Combine(failure, error);
                }
            try
            {
                await ((Task)Property(payload, NativeCons539ResourceNames.Retired)).WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failure = Combine(failure, error);
            }
            try
            {
                await ((Task)Call(provider.Bindings, NativeCons539ResourceNames.Retire)!).WaitAsync(Deadline);
            }
            catch (Exception error)
            {
                failure = Combine(failure, error);
            }
            GC.KeepAlive(callback);
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static Exception Combine(Exception? previous, Exception error)
        => previous is null ? error : new AggregateException(previous, error);

    private static object New(string type, params object[] arguments)
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(type, true)!,
            Members,
            null,
            arguments,
            null
        )!;

    private static IDisposable Scope(object env)
        => (IDisposable)New(
            NativeCons539ResourceNames.Scope,
            New(NativeCons539ResourceNames.AllocationOwner),
            Property(env, NativeCons539ResourceNames.Failure)
        );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint MakeCell(nint environment, nuint head, nuint tail);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint CopyTerm(nint environment, nuint value);

    private static nuint Make(
        nint table,
        object env,
        nuint head,
        nuint tail
    )
    {
        using var scope = Scope(env);
        nuint result = Marshal.GetDelegateForFunctionPointer<MakeCell>(Marshal.ReadIntPtr(table, NativeCons539ResourceNames.MakeCellSlot * IntPtr.Size))(
            (nint)Property(env, NativeCons539ResourceNames.Pointer), head, tail);
        Call(Property(env, NativeCons539ResourceNames.Failure), NativeCons539ResourceNames.ThrowPending);

        return result;
    }

    private static nuint Copy(nint table, object env, nuint source)
    {
        using var scope = Scope(env);
        nuint result = Marshal.GetDelegateForFunctionPointer<CopyTerm>(Marshal.ReadIntPtr(table, NativeCons539ResourceNames.CopySlot * IntPtr.Size))(
            (nint)Property(env, NativeCons539ResourceNames.Pointer), source);
        Call(Property(env, NativeCons539ResourceNames.Failure), NativeCons539ResourceNames.ThrowPending);

        return result;
    }
}
