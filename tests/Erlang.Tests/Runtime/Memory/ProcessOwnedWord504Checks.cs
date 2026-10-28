using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.ProcessOwnedWord504Names;

namespace Erlang.Tests;

internal static class ProcessOwnedWord504Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task Roots()
    {
        object arena = Create(0);
        object lease = Call(arena, Allocate, 4)!;
        try
        {
            Check((int)Get(Get(arena, Snapshot), WordBytes) == IntPtr.Size);
            Check((long)Get(Get(arena, Snapshot), ReservedWords) == 4);
            Check((long)Get(Get(arena, Snapshot), RootedWords) == 0);
            for (int index = 0; index < 4; index++)
                Check(Call(lease, Read, index) is null);
            WeakReference weak = Store(lease);
            Collect();
            Check(weak.IsAlive && ReadExists(lease));
            Check((long)Get(Get(arena, Snapshot), RootedWords) == 1);
            Call(
                lease,
                Write,
                0,
                null
            );
            Collect();
            Check(!weak.IsAlive && Call(lease, Read, 0) is null);
            Check((long)Get(Get(arena, Snapshot), RootedWords) == 0);
        }
        finally
        {
            ((IDisposable)lease).Dispose();
            await ((IAsyncDisposable)arena).DisposeAsync();
        }
        Check((long)Get(Get(arena, Snapshot), ReservedWords) == 0);
        Check((int)Get(Get(arena, Snapshot), ActiveLeases) == 0);
    }

    internal static async Task Retirement()
    {
        object arena = Create(4);
        object lease = Call(arena, Allocate, 4)!;
        object payload = new();
        try
        {
            Call(
                lease,
                Write,
                0,
                payload
            );
            await Task.Run(() => ExpectLimit(() => Call(arena, Allocate, 1)));
            Check((long)Get(Get(arena, Snapshot), ReservedWords) == 4);
            Check((long)Get(Get(arena, Snapshot), RootedWords) == 1);
            Check(ReferenceEquals(Call(lease, Read, 0), payload));
            Task retirement = (Task)Call(arena, Retire)!;
            Check(!retirement.IsCompleted && (bool)Get(Get(arena, Snapshot), Closing));
            Expect<ObjectDisposedException>(() => Call(arena, Allocate, 0));
            Check(ReferenceEquals(Call(lease, Read, 0), payload));
            Expect<ArgumentOutOfRangeException>(() => Call(
                lease,
                Write,
                4,
                payload
            ));
            ((IDisposable)lease).Dispose();
            await retirement.WaitAsync(Deadline);
            Check((long)Get(Get(arena, Snapshot), ReservedWords) == 0);
            Check((long)Get(Get(arena, Snapshot), RootedWords) == 0);
            Expect<ObjectDisposedException>(() => Call(lease, Read, 0));
        }
        finally
        {
            ((IDisposable)lease).Dispose();
            await ((IAsyncDisposable)arena).DisposeAsync();
        }
    }

    internal static async Task Frames()
    {
        object arena = Create(0);
        object execution = CreateTyped(ExecutionType, arena);
        object registers = CreateTyped(RegistersType, execution);
        object? clone = null;
        Term escaped = Term.Tuple(Term.I(504), Term.I(3));
        try
        {
            Call(registers, Allocate, 3);
            Call(
                registers,
                WriteY,
                2,
                escaped
            );
            Check((long)Get(Get(arena, Snapshot), ReservedWords) == 3);
            clone = Call(registers, Clone)!;
            Check((long)Get(Get(arena, Snapshot), ReservedWords) == 6);
            Check(ReferenceEquals(Call(clone, ReadY, 2), escaped));
            Call(
                clone,
                Trim,
                1,
                2
            );
            Check((long)Get(Get(arena, Snapshot), ReservedWords) == 5);
            Check(ReferenceEquals(Call(clone, ReadY, 1), escaped));
            Call(registers, Deallocate, 3);
            Check((long)Get(Get(arena, Snapshot), ReservedWords) == 2);
            Check(ReferenceEquals(Call(clone, ReadY, 1), escaped));
            ((IDisposable)clone).Dispose();
            Check((long)Get(Get(arena, Snapshot), ReservedWords) == 0);
            Check((long)Get(Get(arena, Snapshot), RootedWords) == 0);
            Check(escaped.Equals(Term.Tuple(Term.I(504), Term.I(3))));
        }
        finally
        {
            (clone as IDisposable)?.Dispose();
            ((IDisposable)registers).Dispose();
            await ((IAsyncDisposable)execution).DisposeAsync();
            await ((IAsyncDisposable)arena).DisposeAsync();
        }
    }

    internal static async Task ActiveOperation()
    {
        object arena = Create(0);
        object execution = CreateTyped(ExecutionType, arena);
        object registers = CreateTyped(RegistersType, execution);
        IDisposable operation = (IDisposable)Call(execution, Enter)!;
        try
        {
            Call(registers, Allocate, 2);
            Call(
                registers,
                WriteY,
                0,
                Term.I(504)
            );
            Task retirement = ((IAsyncDisposable)execution).DisposeAsync().AsTask();
            Check(!retirement.IsCompleted);
            Expect<ObjectDisposedException>(() => Call(execution, Enter));
            // An already admitted asynchronous matcher may finish using its roots.
            Call(
                registers,
                WriteY,
                1,
                Term.I(505)
            );
            Check(Call(registers, ReadY, 1)!.Equals(Term.I(505)));
            Check((long)Get(Get(arena, Snapshot), ReservedWords) == 2);
            operation.Dispose();
            await retirement.WaitAsync(Deadline);
            Check((long)Get(Get(arena, Snapshot), ReservedWords) == 0);
            Check((long)Get(Get(arena, Snapshot), RootedWords) == 0);
            Expect<ObjectDisposedException>(() => Call(registers, ReadY, 0));
            Expect<ObjectDisposedException>(() => Call(registers, Allocate, 1));
        }
        finally
        {
            operation.Dispose();
            ((IDisposable)registers).Dispose();
            await ((IAsyncDisposable)execution).DisposeAsync();
            await ((IAsyncDisposable)arena).DisposeAsync();
        }
    }

    private static object CreateTyped(string type, params object?[] arguments)
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(type, throwOnError: true)!,
            Members,
            null,
            arguments,
            null
        )!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Store(object lease)
    {
        object payload = new();
        Call(
            lease,
            Write,
            0,
            payload
        );

        return new(payload);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ReadExists(object lease) => Call(lease, Read, 0) is not null;

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static object Create(long maximum)
        => Activator.CreateInstance(
            typeof(ProcessRuntime).Assembly.GetType(ArenaType, throwOnError: true)!,
            Members,
            null,
            [maximum],
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

    private static void ExpectLimit(Action action)
    {
        try
        {
            action();
        }
        catch (Exception error) when (error.GetType().Name == LimitType)
        {
            return;
        }

        Check(false);
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
