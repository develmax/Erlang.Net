using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.ProcessOwnedTerm504Names;

namespace Erlang.Tests;

internal static class ProcessOwnedTerm504Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task Graph()
    {
        object heap = Create();
        object other = Create();
        object? leaf = null, first = null, second = null, foreign = null;
        try
        {
            byte[] source = [1, 2, 3, 4, 5];
            leaf = Call(heap, CreateBinary, source)!;
            source[0] = 99;
            byte[] read = (byte[])Call(leaf, ReadBinary)!;
            Check(read.SequenceEqual(new byte[] { 1, 2, 3, 4, 5 }));
            read[1] = 99;
            Check(((byte[])Call(leaf, ReadBinary)!)[1] == 2);
            object leafId = Get(leaf, Id);
            first = Call(heap, CreateReferences, References(leafId))!;
            second = Call(heap, CreateReferences, References(Get(first, Id)))!;
            Call(
                first,
                WriteReference,
                0,
                Get(second, Id)
            );
            ((IDisposable)leaf).Dispose();
            object swept = Call(heap, Collect)!;
            Check((int)Get(swept, ReclaimedNodes) == 1 && (long)Get(swept, ReclaimedBytes) == 5);
            Check((long)Get(Get(heap, Snapshot), NativeBytes) == 2 * sizeof(long));
            Check((int)Get(Get(heap, Snapshot), Nodes) == 2);
            Check(((Array)Call(first, ReadReferences)!).GetValue(0)!.Equals(Get(second, Id)));
            Expect<ArgumentException>(() => Call(heap, Root, leafId));
            foreign = Call(other, CreateBinary, new byte[] { 8 })!;
            Expect<ArgumentException>(() => Call(heap, Root, Get(foreign, Id)));
            Expect<ArgumentException>(() => Call(
                first,
                WriteReference,
                0,
                Get(foreign, Id)
            ));
            Check((int)Get(Call(heap, Collect)!, ReclaimedNodes) == 0);
            ((IDisposable)first).Dispose();
            ((IDisposable)second).Dispose();
            swept = Call(heap, Collect)!;
            Check((int)Get(swept, ReclaimedNodes) == 2);
            Check((long)Get(swept, ReclaimedBytes) == 2 * sizeof(long));
            Check((long)Get(Get(heap, Snapshot), NativeBytes) == 0);
            Check((int)Get(Get(heap, Snapshot), RootLeases) == 0);
            Expect<ObjectDisposedException>(() => Call(first, ReadReferences));
            Check(((byte[])Call(foreign, ReadBinary)!).SequenceEqual(new byte[] { 8 }));
        }
        finally
        {
            (leaf as IDisposable)?.Dispose();
            (first as IDisposable)?.Dispose();
            (second as IDisposable)?.Dispose();
            (foreign as IDisposable)?.Dispose();
            await ((IAsyncDisposable)heap).DisposeAsync();
            await ((IAsyncDisposable)other).DisposeAsync();
        }
    }

    internal static async Task Retirement()
    {
        object heap = Create();
        object root = Call(heap, CreateBinary, new byte[] { 11, 12, 13 })!;
        object duplicate = Call(heap, Root, Get(root, Id))!;
        try
        {
            Task retirement = (Task)Call(heap, Retire)!;
            Check(!retirement.IsCompleted && (bool)Get(Get(heap, Snapshot), Closing));
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            try
            {
                await retirement.WaitAsync(cancellation.Token);
                throw new InvalidOperationException(Failure);
            }
            catch (OperationCanceledException) { }
            Check(!retirement.IsCompleted);
            Check(((byte[])Call(root, ReadBinary)!).SequenceEqual(new byte[] { 11, 12, 13 }));
            Expect<ObjectDisposedException>(() => Call(heap, Root, Get(root, Id)));
            Expect<ObjectDisposedException>(() => Call(heap, CreateBinary, Array.Empty<byte>()));
            ((IDisposable)root).Dispose();
            Check(!retirement.IsCompleted);
            Check(((byte[])Call(duplicate, ReadBinary)!).Length == 3);
            ((IDisposable)duplicate).Dispose();
            await retirement.WaitAsync(Deadline);
            Check((int)Get(Get(heap, Snapshot), Nodes) == 0);
            Check((long)Get(Get(heap, Snapshot), NativeBytes) == 0);
            Expect<ObjectDisposedException>(() => Call(duplicate, ReadBinary));
        }
        finally
        {
            ((IDisposable)root).Dispose();
            ((IDisposable)duplicate).Dispose();
            await ((IAsyncDisposable)heap).DisposeAsync();
        }
    }

    private static Array References(object id)
    {
        Array values = Array.CreateInstance(id.GetType(), 1);
        values.SetValue(id, 0);

        return values;
    }

    private static object Create()
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(HeapType, throwOnError: true)!,
            Members,
            null,
            [],
            null
        )!;

    private static object Get(object target, string name)
        => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    private static object? Call(object target, string name, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(name, Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(error.InnerException).Throw();
            throw;
        }
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        Check(false);
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
