using Erlang;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Erlang.Tests;

internal static class NativePayloadProviderHostChecks
{
    internal static async Task Run()
    {
        Preflight();
        await ActualExtents();
        await RuntimeIsolation();
        await ReadersAndReleaseRaces();
        await CancelWhileDomainHeld();
        await UncontendedCapture();
    }

    private static async Task ActualExtents()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        object provider = Provider(runtime);
        Require(Field(runtime, NativePayloadProviderHostNames.NativeDomainField) is null);
        await Expect(
            actor,
            provider,
            0,
            0
        );
        Require(Field(runtime, NativePayloadProviderHostNames.NativeDomainField) is null);
        using var first = new NativePayloadProviderHostOwner(runtime);
        nint positive = first.Payload.Allocate(NativePayloadProviderHostParameters.Initial);
        Require(positive != 0);
        Write(positive, NativePayloadProviderHostParameters.Initial);
        Read(positive, NativePayloadProviderHostParameters.Initial);
        await Expect(
            actor,
            provider,
            NativePayloadProviderHostParameters.Initial,
            NativePayloadProviderHostParameters.Initial
        );
        nint zero = first.Payload.Allocate(0);
        Require(zero != 0);
        await Expect(
            actor,
            provider,
            NativePayloadProviderHostParameters.Initial,
            NativePayloadProviderHostParameters.Initial + 1
        );
        positive = first.Payload.Reallocate(positive, NativePayloadProviderHostParameters.Grown);
        Require(positive != 0);
        Read(positive, NativePayloadProviderHostParameters.Initial);
        Write(positive, NativePayloadProviderHostParameters.Grown);
        await Expect(
            actor,
            provider,
            NativePayloadProviderHostParameters.Grown,
            NativePayloadProviderHostParameters.Grown + 1
        );
        positive = first.Payload.Reallocate(positive, NativePayloadProviderHostParameters.Shrunk);
        Require(positive != 0);
        Read(positive, NativePayloadProviderHostParameters.Shrunk);
        await Expect(
            actor,
            provider,
            NativePayloadProviderHostParameters.Shrunk,
            NativePayloadProviderHostParameters.Shrunk + 1
        );
        using (var second = new NativePayloadProviderHostOwner(runtime))
        {
            nint other = second.Payload.Allocate(NativePayloadProviderHostParameters.OtherOwner);
            Require(other != 0);
            await Expect(
                actor,
                provider,
                NativePayloadProviderHostParameters.Shrunk + NativePayloadProviderHostParameters.OtherOwner,
                NativePayloadProviderHostParameters.Shrunk + NativePayloadProviderHostParameters.OtherOwner + 1
            );
            second.Payload.Free(other);
        }
        first.Payload.Free(positive);
        first.Payload.Free(zero);
        await Expect(
            actor,
            provider,
            0,
            0
        );
        first.Dispose();
        await Expect(
            actor,
            provider,
            0,
            0
        );
    }

    private static async Task RuntimeIsolation()
    {
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        await using var firstActor = new RuntimeNodeFoundationHostActor(first);
        await using var secondActor = new RuntimeNodeFoundationHostActor(second);
        using var payload = new NativePayloadProviderHostOwner(first);
        Require(payload.Payload.Allocate(NativePayloadProviderHostParameters.Initial) != 0);
        object firstProvider = Provider(first);
        object secondProvider = Provider(second);
        await Expect(
            firstActor,
            firstProvider,
            NativePayloadProviderHostParameters.Initial,
            NativePayloadProviderHostParameters.Initial
        );
        await Expect(
            secondActor,
            secondProvider,
            0,
            0
        );
        try
        {
            _ = await secondActor.InvokeAsync(context => Query(context, firstProvider));
            throw new InvalidOperationException(NativePayloadProviderHostNames.Failure);
        }
        catch (InvalidOperationException failure) when (failure.Message == NativePayloadProviderHostNames.DifferentRuntime)
        {
        }
        try
        {
            _ = await firstActor.InvokeAsync(context => Query(context, firstProvider, Term.A(NativePayloadProviderHostNames.Unknown)));
            throw new InvalidOperationException(NativePayloadProviderHostNames.Failure);
        }
        catch (ErlangException failure) when (failure.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
        }
    }

    private static async Task ReadersAndReleaseRaces()
    {
        await using var runtime = new ProcessRuntime();
        using var owner = new NativePayloadProviderHostOwner(runtime);
        Require(owner.Payload.Allocate(NativePayloadProviderHostParameters.Initial) != 0);
        for (int iteration = 0; iteration < NativePayloadProviderHostParameters.WakeupRaces; iteration++)
        {
            var entered = Signal();
            var release = Signal();
            Task holder = Hold(owner.Domain, entered, release);
            try
            {
                await entered.Task.WaitAsync(NativePayloadProviderHostParameters.Deadline);
                Task first;
                Task second;
                if (iteration % NativePayloadProviderHostParameters.ReleaseRacePeriod == 0)
                {
                    first = Capture(owner.Domain, CancellationToken.None);
                    second = Capture(owner.Domain, CancellationToken.None);
                    Require(!first.IsCompleted && !second.IsCompleted);
                    Require(Field(owner.Domain, NativePayloadProviderHostNames.Availability) is TaskCompletionSource);
                }
                else
                {
                    // Race release with reader startup, including the publication boundary.
                    // No claim is made that a particular scheduler interleaving occurred.
                    first = Task.Run(() => Capture(owner.Domain, CancellationToken.None));
                    second = Task.Run(() => Capture(owner.Domain, CancellationToken.None));
                }
                release.TrySetResult();
                await Task.WhenAll(first, second).WaitAsync(NativePayloadProviderHostParameters.Deadline);
            }
            finally
            {
                release.TrySetResult();
                await holder.WaitAsync(NativePayloadProviderHostParameters.Deadline);
            }
        }
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await Expect(
            actor,
            Provider(runtime),
            NativePayloadProviderHostParameters.Initial,
            NativePayloadProviderHostParameters.Initial
        );
    }

    private static async Task CancelWhileDomainHeld()
    {
        await using var runtime = new ProcessRuntime();
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var killer = new RuntimeNodeFoundationHostActor(runtime);
        using var owner = new NativePayloadProviderHostOwner(runtime);
        Require(owner.Payload.Allocate(NativePayloadProviderHostParameters.Initial) != 0);
        var entered = Signal();
        var release = Signal();
        var producerEnded = Signal();
        Task holder = Hold(owner.Domain, entered, release);
        Task<Term>? query = null;
        try
        {
            await entered.Task.WaitAsync(NativePayloadProviderHostParameters.Deadline);
            object provider = TrackedProvider(runtime, producerEnded);
            query = actor.InvokeAsync(context => Query(context, provider));
            var watch = Stopwatch.StartNew();
            while (Field(owner.Domain, NativePayloadProviderHostNames.Availability) is null)
            {
                Require(watch.Elapsed < NativePayloadProviderHostParameters.Deadline);
                await Task.Yield();
            }
            Require(!query.IsCompleted && !producerEnded.Task.IsCompleted && !holder.IsCompleted);
            await killer.Invoke(context =>
            {
                runtime.Exit(context, actor.Process.Pid, Term.A(NativePayloadProviderHostNames.Kill));

                return true;
            });
            try
            {
                _ = await query.WaitAsync(NativePayloadProviderHostParameters.Deadline);
                throw new InvalidOperationException(NativePayloadProviderHostNames.Failure);
            }
            catch (OperationCanceledException)
            {
            }
            await producerEnded.Task.WaitAsync(NativePayloadProviderHostParameters.Deadline);
            Require(!holder.IsCompleted && !release.Task.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
            await holder.WaitAsync(NativePayloadProviderHostParameters.Deadline);
            if (query is not null)
            {
                try
                {
                    _ = await query.WaitAsync(NativePayloadProviderHostParameters.Deadline);
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }

    private static async Task UncontendedCapture()
    {
        await using var runtime = new ProcessRuntime();
        using var owner = new NativePayloadProviderHostOwner(runtime);
        MethodInfo method = owner.Domain.GetType().GetMethod(NativePayloadProviderHostNames.Capture, NativePayloadProviderHostNames.Members)!;
        Type snapshot = method.ReturnType.GetGenericArguments().Single();
        long bytes = (long)typeof(NativePayloadProviderHostChecks).GetMethod(NativePayloadProviderHostNames.Measure, NativePayloadProviderHostNames.Members)!.MakeGenericMethod(snapshot).Invoke(null, [owner.Domain, method])!;
        Require(bytes == 0 && Field(owner.Domain, NativePayloadProviderHostNames.Availability) is null);
    }

    private static long MeasureCapture<T>(object domain, MethodInfo method)
    {
        var capture = (Func<CancellationToken, ValueTask<T>>)method.CreateDelegate(typeof(Func<CancellationToken, ValueTask<T>>), domain);
        for (int iteration = 0; iteration < NativePayloadProviderHostParameters.WarmupCaptures; iteration++)
            _ = capture(CancellationToken.None).Result;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int iteration = 0; iteration < NativePayloadProviderHostParameters.MeasuredCaptures; iteration++)
        {
            ValueTask<T> result = capture(CancellationToken.None);
            Require(result.IsCompletedSuccessfully);
            _ = result.Result;
        }

        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static Task Hold(object domain, TaskCompletionSource entered, TaskCompletionSource release) => Task.Run(() =>
    {
        object gate = Field(domain, NativePayloadProviderHostNames.Gate)!;
        lock (gate)
        {
            entered.TrySetResult();
            if (!release.Task.Wait(NativePayloadProviderHostParameters.Deadline))
                throw new TimeoutException(NativePayloadProviderHostNames.Failure);
        }
        // Reflective HOST holders do not execute the production release helper. A real
        // synchronous snapshot release must also wake any pending notification.
        _ = domain.GetType().GetProperty(NifAllocationLedgerHostNames.Snapshot, NativePayloadProviderHostNames.Members)!.GetValue(domain);
    });

    private static async Task Capture(object domain, CancellationToken cancellation)
    {
        object value = domain.GetType().GetMethod(NativePayloadProviderHostNames.Capture, NativePayloadProviderHostNames.Members)!.Invoke(domain, [cancellation])!;
        Task task = (Task)value.GetType().GetMethod(nameof(ValueTask.AsTask))!.Invoke(value, null)!;
        await task.WaitAsync(NativePayloadProviderHostParameters.Deadline);
    }

    private static object TrackedProvider(ProcessRuntime runtime, TaskCompletionSource ended)
    {
        object actual = Provider(runtime);
        var producer = (Func<ProcessContext, ReferenceTerm, Term, Action<Term, Term>, CancellationToken, Task>)Field(actual, NativePayloadProviderHostNames.Producer)!;

        async Task Track(
            ProcessContext context,
            ReferenceTerm reference,
            Term allocators,
            Action<Term, Term> post,
            CancellationToken cancellation
        )
        {
            try
            {
                await producer(
                    context,
                    reference,
                    allocators,
                    post,
                    cancellation
                );
            }
            finally
            {
                ended.TrySetResult();
            }
        }
        object known = actual.GetType().GetProperty(NativePayloadProviderHostNames.Known, NativePayloadProviderHostNames.Members)!.GetValue(actual)!;

        return actual.GetType().GetConstructors(NativePayloadProviderHostNames.Members).Single().Invoke([
            known,
            NativePayloadProviderHostParameters.ReplyCount,
            (Func<ProcessContext, ReferenceTerm, Term, Action<Term, Term>, CancellationToken, Task>)Track,
            CancellationToken.None
        ]);
    }

    private static object Provider(ProcessRuntime runtime) => typeof(ProcessRuntime).Assembly.GetType(NativePayloadProviderHostNames.FactoryType, true)!
        .GetMethod(NativePayloadProviderHostNames.Factory, NativePayloadProviderHostNames.Members)!.Invoke(null, [runtime])!;

    private static async Task<Term> Query(ProcessContext context, object provider, Term? input = null)
    {
        object result = typeof(ProcessRuntime).Assembly.GetType(AllocatorQueryMailboxHostNames.MailboxType, true)!
            .GetMethod(AllocatorQueryMailboxHostNames.Gather, NativePayloadProviderHostNames.Members)!.Invoke(null, [context, input ?? Term.A(NativePayloadProviderHostNames.Identity), provider])!;

        return await (ValueTask<Term>)result;
    }

    private static async Task Expect(
        RuntimeNodeFoundationHostActor actor,
        object provider,
        ulong requested,
        ulong backing
    )
    {
        Term actual = await actor.InvokeAsync(context => Query(context, provider));
        Term expected = Term.List(Term.Tuple(
            Term.A(NativePayloadProviderHostNames.Instance),
            Term.I(NativePayloadProviderHostParameters.AggregateInstance),
            Term.List(
                Term.Tuple(Term.A(NativePayloadProviderHostNames.Requested), new Integer(requested)),
                Term.Tuple(Term.A(NativePayloadProviderHostNames.Backing), new Integer(backing))
            )
        ));
        Require(actual.Equals(expected));
    }

    private static object? Field(object target, string name) => target.GetType().GetField(name, NativePayloadProviderHostNames.Members)!.GetValue(target);

    private static void Write(nint pointer, nuint length)
    {
        for (int offset = 0; offset < (int)length; offset++)
            Marshal.WriteByte(pointer, offset, NativePayloadProviderHostParameters.Payload);
    }

    private static void Read(nint pointer, nuint length)
    {
        for (int offset = 0; offset < (int)length; offset++)
            Require(Marshal.ReadByte(pointer, offset) == NativePayloadProviderHostParameters.Payload);
    }

    private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NativePayloadProviderHostNames.Failure);
    }

    private static void Preflight()
    {
        NifReallocationHostOwner.Preflight();
        Require(typeof(ProcessRuntime).Assembly.GetType(NativePayloadProviderHostNames.FactoryType, true)!
            .GetMethod(NativePayloadProviderHostNames.Factory, NativePayloadProviderHostNames.Members) is not null);
    }
}
