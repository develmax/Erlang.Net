using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.NativeResource524Names;
using static Erlang.Tests.NativeMagicReference576Names;

namespace Erlang.Tests;

// Owns every test allocation before assertions. No trailing await-using can
// replace the primary failure or wait indefinitely after another phase fails.
internal sealed class NativeMagicReference576Fixture
{
    private readonly List<RuntimeNodeFoundationHostActor> actors = new();
    private readonly List<object> heaps = new();
    private readonly List<IDisposable> frames = new();
    private readonly List<Action?> restores = new();
    private object? bindings;
    private object? payload;
    private bool nativeOutstanding;

    private NativeMagicReference576Fixture(bool owned) => Runtime = OwnedGuest506RuntimeFactory.Create(owned);

    internal ProcessRuntime Runtime { get; }

    internal static Task Run(Func<NativeMagicReference576Fixture, Task> body) => RunProfile(body, true);

    internal static Task RunDefault(Func<NativeMagicReference576Fixture, Task> body) => RunProfile(body, false);

    private static async Task RunProfile(Func<NativeMagicReference576Fixture, Task> body, bool owned)
    {
        var failures = new List<Exception>();
        NativeMagicReference576Fixture? fixture = null;
        try
        {
            fixture = new(owned);
            await body(fixture);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            if (fixture is not null)
                await fixture.Drain(failures);
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(CleanupFailure, failures);
    }

    internal async Task<object> CreateHeap()
    {
        actors.EnsureCapacity(checked(actors.Count + 1));
        heaps.EnsureCapacity(checked(heaps.Count + 1));
        var actor = new RuntimeNodeFoundationHostActor(Runtime);
        actors.Add(actor);
        ProcessContext context = await actor.Ready.WaitAsync(Deadline);
        object heap = Property(context, Heap);
        heaps.Add(heap);

        return heap;
    }

    internal object CreatePayload()
    {
        var provider = Provider(Runtime);
        bindings = provider.Bindings;
        payload = Allocate(Runtime, provider.Provider, 0);
        nativeOutstanding = true;

        return payload;
    }

    internal IDisposable EnterRoot(object heap)
    {
        frames.EnsureCapacity(checked(frames.Count + 1));
        var frame = (IDisposable)Call(heap, "Enter")!;
        frames.Add(frame);

        return frame;
    }

    internal IDisposable EnterHost()
    {
        frames.EnsureCapacity(checked(frames.Count + 1));
        var scope = new NativeTermHostScope524(Runtime);
        frames.Add(scope);

        return scope;
    }

    // Synchronous scope body intentionally keeps AsyncLocal current ownership
    // in this exact execution context even when its assertions throw.
    internal void WithHost(Action body)
    {
        var failures = new List<Exception>();
        IDisposable host = EnterHost();
        try
        {
            body();
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            Attempt(host.Dispose, FrameFailure, failures);
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(CleanupFailure, failures);
    }

    internal void DisposeFrame(IDisposable frame) => frame.Dispose();

    internal int TrackRestore(Action restore)
    {
        int index = restores.Count;
        restores.Add(restore);

        return index;
    }

    internal void Restore(int index)
    {
        Action? restore = restores[index];
        restore?.Invoke();
        restores[index] = null;
    }

    // The actual counter is inspected under its own gate. If ReleaseNative524
    // throws AFTER decrement/queue transfer, cleanup must not double-release;
    // if reflection fails BEFORE entry, the still-owned count is retried.
    internal void ReleaseNative()
    {
        if (payload is null || !nativeOutstanding)
            return;
        if (ReadNativeCount(payload) == 0)
        {
            nativeOutstanding = false;

            return;
        }
        Exception? failure = null;
        try
        {
            Call(payload, "ReleaseNative524");
        }
        catch (Exception error)
        {
            failure = error;
        }
        try
        {
            nativeOutstanding = ReadNativeCount(payload) != 0;
        }
        catch (Exception error)
        {
            failure = failure is null ? error : new AggregateException(failure, error);
        }
        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static long ReadNativeCount(object value)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object gate = value.GetType().GetField("gate", flags)!.GetValue(value)!;
        lock (gate)
            return (long)value.GetType().GetField("native", flags)!.GetValue(value)!;
    }

    private async Task Drain(List<Exception> failures)
    {
        // All corruption recovery precedes any read, collection or retirement.
        for (int index = restores.Count - 1; index >= 0; index--)
        {
            int captured = index;
            Attempt(() => Restore(captured), RestoreFailure, failures);
        }
        // Reverse order also closes an active host scope before older frames.
        for (int index = frames.Count - 1; index >= 0; index--)
            Attempt(frames[index].Dispose, FrameFailure, failures);
        Attempt(ReleaseNative, NativeFailure, failures);
        foreach (object heap in heaps)
            Attempt(() => Call(heap, "Collect"), CollectionFailure, failures);
        if (payload is not null)
            await Await(() => (Task)Property(payload, "Retired524"), PayloadFailure, failures);
        foreach (RuntimeNodeFoundationHostActor actor in actors)
            await Await(() => actor.DisposeAsync().AsTask(), ActorFailure, failures);
        foreach (object heap in heaps)
            await Await(() => (Task)Call(heap, "RetireAsync")!, HeapFailure, failures);
        if (bindings is not null)
            await Await(() => (Task)Call(bindings, "RetireAsync")!, ImageFailure, failures);
        await Await(() => Runtime.DisposeAsync().AsTask(), RuntimeFailure, failures);
    }

    private static void Attempt(Action action, string phase, List<Exception> failures)
    {
        try
        {
            action();
        }
        catch (Exception error)
        {
            failures.Add(new InvalidOperationException(phase, error));
        }
    }

    private static async Task Await(Func<Task> action, string phase, List<Exception> failures)
    {
        try
        {
            await action().WaitAsync(Deadline);
        }
        catch (Exception error)
        {
            failures.Add(new InvalidOperationException(phase, error));
        }
    }
}
