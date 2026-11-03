using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using static Erlang.Tests.ModuleBinary805Names;

namespace Erlang.Tests;

internal static class ModuleBinary805Checks
{
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static object Get(object value, string name) => value.GetType().GetProperty(name, Members)!.GetValue(value)!;

    private static object? Call(object value, string name, params object[] arguments)
    {
        try
        {
            return value.GetType().GetMethod(name, Members)!.Invoke(value, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

    private static long Words(object heap) => (long)Get(Get(heap, Snapshot), LiveWords);

    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        byte[] expected = Enumerable.Range(0, Bytes).Select(value => (byte)value).ToArray();
        byte[] image = BeamLiteralExportHostBytes.Image(ExternalTermFormat.Encode(new BitString(expected)));
        await using PreparedBeamImage prepared = runtime.Modules.PrepareBeam(image);
        object code = Get(prepared, ModuleLiteral800Names.Code);
        var literals = (IReadOnlyList<Term>)Get(Get(code, ModuleLiteral800Names.Structure), ModuleLiteral800Names.Literals);
        BitString literal = (BitString)literals.Single();
        object storage = Get(literal, Owned);
        object moduleHeap = Get(storage, ModuleLiteral800Names.Owner);
        object native = Get(Get(literal, Backing), Native);
        nint pointer = (nint)Get(native, ModuleBinary805Names.Pointer);
        Task bytesRetired = (Task)Get(native, Retired);
        Require(Words(moduleHeap) == NativeWords);
        Require(Marshal.ReadInt64(pointer) == 0 && Marshal.ReadInt64(pointer + IntPtr.Size) == 0);
        Require(Marshal.ReadInt64(pointer + RefcountOffsetWords * IntPtr.Size) == 1);
        Require(Marshal.ReadInt64(pointer + SizeOffsetWords * IntPtr.Size) == Bytes);
        byte[] actual = new byte[Bytes];
        Marshal.Copy(
            pointer + PrefixWords * IntPtr.Size,
            actual,
            0,
            Bytes
        );
        Require(actual.SequenceEqual(expected) && literal.ToArray().SequenceEqual(expected));
        // This is the actual Host-detachment implementation, not a fabricated
        // portable stand-in or a native owner retained by a CLR-only marker.
        MethodInfo detach = typeof(ProcessRuntime).Assembly.GetType(Detached, true)!.GetMethod(
            Copy,
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [typeof(ProcessRuntime), typeof(Term)],
            null
        )!;
        BitString portable = (BitString)detach.Invoke(null, [runtime, literal])!;
        Require(Get(portable, Owned) is null && Get(Get(portable, Backing), Native) is null);
        ModuleCodeImage identity = (ModuleCodeImage)Get(Get(prepared, ModuleLiteral800Names.Execution), ModuleLiteral800Names.Image);
        SourceCodeLease? admitted = null;
        IDisposable? processFrame = null;
        object? processHeap = null;
        int slot = -1;
        BitString? processValue = null;
        Task? closing = null;
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        try
        {
            await actor.InvokeAsync(context =>
            {
                admitted = runtime.Modules.ReserveSourceCode(context, identity);
                using IDisposable flow = admitted.Attach(prepared);
                processHeap = Get(context, Heap);
                long before = Words(processHeap);
                processFrame = (IDisposable)Call(processHeap, Enter)!;
                slot = (int)Call(processFrame, Reserve)!;
                BitString sourceSlice = literal.SliceBits(BitsPerByte, (Bytes - 1) * BitsPerByte);
                processValue = (BitString)Call(
                    processFrame,
                    Adopt,
                    slot,
                    sourceSlice
                )!;
                Require(Words(processHeap) - before == NativeWords);
                Require(ReferenceEquals(Get(Get(processValue, Backing), Native), native));
                Require(!ReferenceEquals(Get(Get(processValue, Owned), ModuleLiteral800Names.Owner), moduleHeap));
                Require(processValue.ToArray().SequenceEqual(expected.Skip(1)));

                return Task.FromResult(true);
            });
            closing = prepared.DisposeAsync().AsTask();
            Require(!closing.IsCompleted && !bytesRetired.IsCompleted);
            admitted!.Dispose();
            admitted = null;
            await closing.WaitAsync(Timeout);
            Require(!bytesRetired.IsCompleted && processValue!.ToArray().SequenceEqual(expected.Skip(1)));
            Require(Marshal.ReadInt64(pointer + RefcountOffsetWords * IntPtr.Size) == 1);
            Call(processFrame!, Clear, slot);
            Call(processHeap!, Collect);
            await bytesRetired.WaitAsync(Timeout);
            Require(portable.ToArray().SequenceEqual(expected));
            GC.KeepAlive(literal);
            GC.KeepAlive(processValue);
            GC.KeepAlive(native);
        }
        finally
        {
            admitted?.Dispose();
            processFrame?.Dispose();
            if (closing is not null) await closing.WaitAsync(Timeout);
        }
    }
}
