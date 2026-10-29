using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeStore600Names;

namespace Erlang.Tests;

// Actual native524 allocation and real store materializers. No C ABI claim.
internal static class NativeStore600Checks
{
    internal static Task Ets() => Run(false);

    internal static Task Persistent() => Run(true);

    private static Task Run(bool persistent) => NativeMagicReference576Fixture.Run(async fixture =>
    {
        var failures = new List<Exception>();
        RuntimeNodeFoundationHostActor? sender = null, receiver = null;
        Func<Task>? removeStore = null;
        try
        {
            ProcessRuntime runtime = fixture.Runtime;
            object sourceHeap = await fixture.CreateHeap();
            object destinationHeap = await fixture.CreateHeap();
            object payload = fixture.CreatePayload();
            nint pointer = (nint)Property(payload, PayloadPointer);
            Marshal.WriteInt32(pointer, Sentinel);
            var identity = (ResourceReferenceTerm)Call(runtime.ReferenceResources, Make, payload)!;
            IDisposable sourceFrame = fixture.EnterRoot(sourceHeap);
            IDisposable destinationFrame = fixture.EnterRoot(destinationHeap);
            int sourceSlot = (int)Call(sourceFrame, Reserve)!;
            int destinationSlot = (int)Call(destinationFrame, Reserve)!;
            Call(
                sourceFrame,
                Set,
                sourceSlot,
                identity
            );
            var source = (ResourceReferenceTerm)Call(sourceFrame, Read, sourceSlot)!;
            sender = new(runtime);
            receiver = new(runtime);
            await sender.Ready.WaitAsync(Deadline);
            await receiver.Ready.WaitAsync(Deadline);
            Term key = Term.A(Key);
            Func<ProcessContext, Term> read;
            if (persistent)
            {
                object store = PersistentTerm408HostAccess.Store(runtime);
                removeStore = async () =>
                {
                    await sender.InvokeAsync(context =>
                        ((ValueTask<Term>)PersistentTerm408HostAccess.Call(
                            store,
                            PersistentTerm408HostNames.Erase,
                            context,
                            key
                        )!).AsTask());
                };
                await sender.InvokeAsync(context => PersistentTerm408HostAccess.Put(
                    store,
                    context,
                    key,
                    source
                ));
                read = context => (Term)PersistentTerm408HostAccess.Call(
                    store,
                    PersistentTerm408HostNames.Get,
                    context,
                    key
                )!;
            }
            else
            {
                object store = EtsFoundation405Access.Get(runtime);
                Term table = await sender.Invoke(context => EtsFoundation405Access.Create(store, context));
                removeStore = async () =>
                {
                    await sender.Invoke(context => EtsFoundation405Access.Invoke(
                        store,
                        EtsFoundation405Names.Destroy,
                        context,
                        table
                    ));
                };
                long slot = await sender.Invoke(context => EtsFoundation405Access.Store(
                    store,
                    context,
                    table,
                    Term.Tuple(key, source)
                ));
                read = context => ((TupleTerm)EtsFoundation405Access.Read(
                    store,
                    context,
                    table,
                    slot
                )).Items[1];
            }
            fixture.ReleaseNative();
            Call(sourceFrame, Clear, sourceSlot);
            Call(
                sourceHeap,
                Major,
                true,
                ushort.MaxValue
            );
            bool stale = false;
            try
            {
                _ = source.Words;
            }
            catch (ObjectDisposedException)
            {
                stale = true;
            }
            Require(stale && !((Task)Property(payload, Retired)).IsCompleted);
            // Host scope is synchronous and closed in the same actor callback.
            // The real destination header is published BEFORE its Host lease ends.
            var destination = await receiver.Invoke(context =>
            {
                using var host = new NativeTermHostScope524(runtime);
                var restored = (ResourceReferenceTerm)read(context);
                Call(
                    destinationFrame,
                    Set,
                    destinationSlot,
                    restored
                );

                return (ResourceReferenceTerm)Call(destinationFrame, Read, destinationSlot)!;
            });
            await removeStore();
            removeStore = null;
            Type roots = typeof(ProcessRuntime).Assembly.GetType(Roots, true)!;
            object actual = roots.GetMethod(Direct, Members)!.Invoke(null, [destination, runtime])!;
            Require(ReferenceEquals(actual, payload));
            nint restoredPointer = (nint)Property(actual, PayloadPointer);
            Require(restoredPointer == pointer && Marshal.ReadInt32(restoredPointer) == Sentinel);
            Marshal.WriteInt32(restoredPointer, Sentinel + 1);
            Require(Marshal.ReadInt32(pointer) == Sentinel + 1);
            Call(destinationFrame, Clear, destinationSlot);
            Call(
                destinationHeap,
                Major,
                true,
                ushort.MaxValue
            );
            await ((Task)Property(payload, Retired)).WaitAsync(Deadline);
            bool destinationStale = false;
            try
            {
                _ = destination.Words;
            }
            catch (ObjectDisposedException)
            {
                destinationStale = true;
            }
            Require(destinationStale);
            GC.KeepAlive(source);
            GC.KeepAlive(destination);
            GC.KeepAlive(payload);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (removeStore is not null)
                try
                {
                    await removeStore();
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (receiver is not null)
                try
                {
                    await receiver.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (sender is not null)
                try
                {
                    await sender.DisposeAsync().AsTask().WaitAsync(Deadline);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(CleanupFailure, failures);
    });

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }
}
