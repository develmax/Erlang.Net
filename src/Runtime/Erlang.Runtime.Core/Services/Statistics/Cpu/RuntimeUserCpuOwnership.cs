using System.ComponentModel;
using System.Numerics;

namespace Erlang;

internal static class RuntimeUserCpuOwnership
{
    private static readonly object Gate = new();
    private static readonly HashSet<ThreadState> Active = new();
    [ThreadStatic] private static ThreadState? current;
    // Safe pool-thread cache: diagnostics metadata only; it cannot root a VM.
    [ThreadStatic] private static RuntimeThreadUserCpuReader? cachedReader;

    internal static IDisposable Enter(RuntimeUserCpuAccounting owner)
    {
        lock (Gate)
        {
            ThreadState? state = current;
            bool publish = state is null;
            if (state is null)
            {
                try
                {
                    RuntimeThreadUserCpuReader reader = ReaderForCurrentThread();
                    state = new(reader, reader.ReadTicks());
                }
                catch (Exception error) when (IsProviderFailure(error))
                {
                    owner.Unavailable ??= error;

                    return Empty.Instance;
                }
            }
            else
                Advance(state);
            if (state.Failure is { } failure)
                owner.Unavailable ??= failure;
            var frame = new Frame(state, owner, state.Top);
            if (publish)
            {
                Active.Add(state);
                current = state;
            }
            state.Top = frame;

            return frame;
        }
    }

    internal static BigInteger SampleTicks(RuntimeUserCpuAccounting owner)
    {
        lock (Gate)
        {
            // Even an unused VM must not return a manufactured zero when this
            // target/provider cannot actually measure user CPU.
            try
            {
                _ = ReaderForCurrentThread().ReadTicks();
            }
            catch (Exception error) when (IsProviderFailure(error))
            {
                owner.Unavailable ??= error;
            }
            foreach (ThreadState state in Active)
                if (ReferenceEquals(state.Top?.Owner, owner))
                    Advance(state);
            if (owner.Unavailable is { } failure)
                throw new NotSupportedException(RuntimeCpuTimeNames.Unavailable, failure);

            return owner.AccumulatedTicks;
        }
    }

    private static RuntimeThreadUserCpuReader ReaderForCurrentThread()
    {
        int id = RuntimeOsThreadIdentity.Current();
        if (cachedReader is { } cached && cached.OsThreadId != id)
        {
            cached.Dispose();
            cachedReader = null;
        }

        return cachedReader ??= RuntimeThreadUserCpuReader.OpenCurrent(id);
    }

    private static void Advance(ThreadState state)
    {
        if (state.Failure is not null) return;
        try
        {
            long ticks = state.Reader.ReadTicks();
            if (ticks < state.LastTicks)
                throw new InvalidOperationException(RuntimeCpuTimeNames.CounterRegression);
            if (state.Top?.Owner is { } owner)
                owner.AccumulatedTicks += ticks - state.LastTicks;
            state.LastTicks = ticks;
        }
        catch (Exception error) when (IsProviderFailure(error))
        {
            state.Failure = error;
            for (Frame? frame = state.Top; frame is not null; frame = frame.Previous)
                frame.Owner!.Unavailable ??= error;
        }
    }

    private static bool IsProviderFailure(Exception error)
        => error is NotSupportedException or Win32Exception or InvalidOperationException
            or EntryPointNotFoundException or DllNotFoundException or OverflowException;

    private sealed class ThreadState(RuntimeThreadUserCpuReader reader, long ticks)
    {
        internal RuntimeThreadUserCpuReader Reader { get; } = reader;
        internal long LastTicks = ticks;
        internal Frame? Top;
        internal Exception? Failure;
    }

    private sealed class Frame(ThreadState state, RuntimeUserCpuAccounting owner, Frame? previous) : IDisposable
    {
        internal RuntimeUserCpuAccounting? Owner = owner;
        internal Frame? Previous = previous;
        private bool closed;

        public void Dispose()
        {
            lock (Gate)
            {
                if (closed) return;
                if (!ReferenceEquals(current, state)
                    || RuntimeOsThreadIdentity.Current() != state.Reader.OsThreadId)
                    throw new InvalidOperationException(RuntimeCpuTimeNames.ThreadIdentity);
                if (!ReferenceEquals(state.Top, this))
                    throw new InvalidOperationException(RuntimeCpuTimeNames.ScopeOrder);
                Advance(state);
                state.Top = Previous;
                Previous = null;
                Owner = null;
                closed = true;
                if (state.Top is null)
                {
                    Active.Remove(state);
                    current = null;
                }
            }
        }
    }

    private sealed class Empty : IDisposable
    {
        internal static readonly Empty Instance = new();

        public void Dispose() { }
    }
}
