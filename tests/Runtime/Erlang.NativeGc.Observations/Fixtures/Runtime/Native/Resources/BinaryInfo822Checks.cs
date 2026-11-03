using System.Reflection;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.BinaryInfo822ControlNames;

namespace Erlang.Tests;

internal static class BinaryInfo822Checks
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Destructor(nint environment, nint payload);

    internal static async Task Run()
    {
        foreach (bool enabled in new[] { false, true })
        {
            await OrdinaryAndWritable(enabled);
            await ActualResourceHeaders(enabled);
        }
    }

    private static Array CaptureRows(object heap) => (Array)Call(heap, Capture)!;

    private static void VerifyPhysical(Array rows)
    {
        foreach (object row in rows)
        {
            ulong identifier = (ulong)Property(row, Identifier);
            nint pointer = unchecked((nint)(nuint)identifier);
            Check(pointer != 0);
            Check((ulong)Property(row, OriginalSize) == checked((ulong)Marshal.ReadInt64(pointer, OriginalSizeOffset)));
            Check((long)Property(row, References) == Marshal.ReadInt64(pointer, ReferencesOffset));
        }
    }

    private static async Task OrdinaryAndWritable(bool enabled)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        object heap = Property(context, Heap);
        using var frame = (IDisposable)Call(heap, Enter)!;
        Check(CaptureRows(heap).Length == 0);
        int ordinary = (int)Call(frame, Reserve)!;
        Call(
            frame,
            Set,
            ordinary,
            new BitString(new byte[BinaryBytes])
        );
        Array first = CaptureRows(heap);
        Check(first.Length == 1);
        VerifyPhysical(first);
        object firstRow = first.GetValue(0)!;
        Check((ulong)Property(firstRow, OriginalSize) == BinaryBytes);
        long references = (long)Property(firstRow, References);
        VerifyPhysical(CaptureRows(heap));
        Check((long)Property(CaptureRows(heap).GetValue(0)!, References) == references);

        BitString stored = (BitString)Call(frame, Read, ordinary)!;
        int aliases = (int)Call(frame, Reserve)!;
        Call(
            frame,
            Set,
            aliases,
            stored
        );
        Check(CaptureRows(heap).Length == 1);
        int small = (int)Call(frame, Reserve)!;
        Call(
            frame,
            Set,
            small,
            new BitString(new byte[1])
        );
        Check(CaptureRows(heap).Length == 1);

        MethodInfo factory = typeof(BitString).GetMethod(InitializeWritable, BindingFlags.Static | BindingFlags.NonPublic)!;
        var writable = (BitString)factory.Invoke(null, [WritableCapacity])!;
        int writer = (int)Call(frame, Reserve)!;
        Call(
            frame,
            AdoptGuest,
            writer,
            writable
        );
        Array both = CaptureRows(heap);
        Check(both.Length == 2);
        object writerRow = both.Cast<object>().Single(row => (ulong)Property(row, OriginalSize) == WritableCapacity);
        nint writerPointer = unchecked((nint)(nuint)(ulong)Property(writerRow, Identifier));
        long flags = Marshal.ReadInt64(writerPointer);
        Check(flags != 0);
        using (var enclosing = (IDisposable)Call(heap, Enter)!)
        {
            Term actualWriter = (Term)Call(frame, Read, writer)!;
            var actualParent = (TupleTerm)Call(
                enclosing,
                ModuleLiteral801Names.CreateTuple,
                Call(enclosing, Reserve)!,
                (object)new Term[] { actualWriter }
            )!;
            Check(!(bool)Property(Property(actualParent, ModuleLiteral801Names.Storage), ModuleLiteral801Names.ReadonlyCapture));
            Check(!(bool)Property(Property(actualParent, ModuleLiteral801Names.Storage), ModuleLiteral801Names.LiteralOnly));
        }
        Check(Marshal.ReadInt64(writerPointer) == flags);
        VerifyPhysical(both);
        VerifyPhysical(CaptureRows(heap));
        Check(Marshal.ReadInt64(writerPointer) == flags);

        foreach (object owner in new object[] { runtime, runtime.Modules })
        {
            FieldInfo gateField = owner.GetType().GetField(owner is ProcessRuntime ? RuntimeGate : ModuleGate, BindingFlags.Instance | BindingFlags.NonPublic)!;
            bool refused = false;
            lock (gateField.GetValue(owner)!)
            {
                try
                {
                    _ = CaptureRows(heap);
                }
                catch (InvalidOperationException)
                {
                    refused = true;
                }
            }
            Check(refused);
        }
        foreach (int slot in new[] { ordinary, aliases, small, writer }) Call(frame, Clear, slot);
        Call(heap, Collect);
        Check(CaptureRows(heap).Length == 0);
    }

    private static async Task ActualResourceHeaders(bool enabled)
    {
        await using var runtime = OwnedGuest506RuntimeFactory.Create(enabled);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ProcessContext context = await actor.Ready;
        object heap = Property(context, Heap);
        var provider = Provider(runtime);
        int destroyed = 0;
        Destructor destructor = (_, _) => Interlocked.Increment(ref destroyed);
        object payload = Allocate(runtime, provider.Provider, Marshal.GetFunctionPointerForDelegate(destructor));
        using var frame = (IDisposable)Call(heap, Enter)!;
        int first = (int)Call(frame, Reserve)!;
        int second = (int)Call(frame, Reserve)!;
        bool released = false;
        try
        {
            // Independent imports create TWO genuine BinRefs of ONE payload.
            Call(
                frame,
                Set,
                first,
                Binary(runtime, payload)
            );
            Call(
                frame,
                Set,
                second,
                Binary(runtime, payload)
            );
            Array rows = CaptureRows(heap);
            Check(rows.Length == 2);
            VerifyPhysical(rows);
            Check((ulong)Property(rows.GetValue(0)!, Identifier) == (ulong)Property(rows.GetValue(1)!, Identifier));
            Check((ulong)Property(rows.GetValue(0)!, OriginalSize) > PayloadBytes);
            Call(payload, ReleaseNative);
            released = true;
            VerifyPhysical(CaptureRows(heap));
            Call(frame, Clear, first);
            Check(CaptureRows(heap).Length == 2); // no hidden collection in query
            Call(heap, Collect);
            Check(CaptureRows(heap).Length == 1 && destroyed == 0);
            Call(frame, Clear, second);
            Call(heap, Collect);
            await ((Task)Property(payload, Retired)).WaitAsync(Deadline);
            Check(CaptureRows(heap).Length == 0 && destroyed == 1);
            await ((Task)Call(provider.Bindings, RetireImage)!).WaitAsync(Deadline);
            Check(provider.Owner.Disposed);
        }
        finally
        {
            Call(frame, Clear, first);
            Call(frame, Clear, second);
            Call(heap, Collect);
            if (!released) Call(payload, ReleaseNative);
            await ((Task)Property(payload, Retired)).WaitAsync(Deadline);
            await ((Task)Call(provider.Bindings, RetireImage)!).WaitAsync(Deadline);
            GC.KeepAlive(destructor);
        }
    }
}
