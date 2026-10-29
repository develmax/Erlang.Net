using System.Runtime.ExceptionServices;
using static Erlang.Tests.ProcessPayloadCore641Access;
using static Erlang.Tests.ProcessPayloadCore641Names;
using static Erlang.Tests.Nonblocking641Names;

namespace Erlang.Tests;

internal static class Nonblocking641Checks
{
    internal static async Task Settlement()
    {
        await Variant(Success);
        await Variant(Faults);
        await Variant(Canceled);
    }

    private static async Task Variant(int mode)
    {
        var runtime = OwnedGuest506RuntimeFactory.Create();
        var failures = new List<Exception>();
        var dependency = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = new InvalidOperationException(DependencyFault);
        var second = new InvalidOperationException(ReleaseFault);
        var ambient = new AsyncLocal<object?>();
        object gate = Field(runtime, Gate)!;
        var probe = new Nonblocking641Probe(gate, ambient, mode == Faults ? second : null);
        object? work = null;
        Task? completion = null, joining = null;
        try
        {
            object owner = Property(runtime, Owner);
            work = Call(
                owner,
                Reserve,
                null,
                false
            )!;
            Call(work, Attach, probe);
            completion = (Task)Property(work, Completion);
            lock (gate)
            {
                ambient.Value = new object();
                try
                {
                    Call(work, EndAfter, dependency.Task);

                }
                finally
                {
                    ambient.Value = null;
                }
            }
            joining = Task.Run(async () =>
            {
                await ((Task)Call(work, Join)!).ConfigureAwait(false);
            });
            Require(!probe.Started.IsCompleted && !completion.IsCompleted && probe.Disposals == 0);
            if (mode == Faults) dependency.TrySetException(first);
            else if (mode == Canceled) dependency.TrySetCanceled();
            else dependency.TrySetResult();
            await probe.Started.WaitAsync(Deadline);
            Require(probe.Disposals == 1 && probe.DefaultContext && probe.OutsideCallerGate);
            Require(!completion.IsCompleted && !joining.IsCompleted);
            probe.Release();
            await Observe(
                completion,
                mode,
                first,
                second
            );
            await Observe(
                joining,
                mode,
                first,
                second
            );
            if (mode == Faults)
                Require(completion.Exception!.Flatten().InnerExceptions.Count == 2);
            Require(probe.Disposals == 1);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            dependency.TrySetResult();
            probe.Release();
            if (work is not null)
                try
                {
                    Call(work, End);
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (completion is not null)
                try
                {
                    await Observe(
                        completion,
                        mode,
                        first,
                        second
                    );
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            if (joining is not null)
                try
                {
                    await Observe(
                        joining,
                        mode,
                        first,
                        second
                    );
                }
                catch (Exception error)
                {
                    failures.Add(error);
                }
            try
            {
                await Observe(
                    runtime.DisposeAsync().AsTask(),
                    mode,
                    first,
                    second
                );
            }
            catch (Exception error)
            {
                failures.Add(error);
            }
        }
        if (failures.Count == 1) ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1) throw new AggregateException(Failure, failures);
    }

    private static async Task Observe(
        Task operation,
        int mode,
        Exception first,
        Exception second
    )
    {
        try
        {
            await operation.WaitAsync(Deadline);
        }
        catch (Exception error) when (mode == Faults && Expected(error, first, second))
        {
            return;
        }
        Require(mode != Faults);
    }

    private static bool Expected(Exception error, Exception first, Exception second)
    {
        Exception[] leaves = error is AggregateException aggregate
            ? aggregate.Flatten().InnerExceptions.ToArray()
            : [error];

        return leaves.Length >= 2
            && leaves.All(item => ReferenceEquals(item, first) || ReferenceEquals(item, second))
            && leaves.Any(item => ReferenceEquals(item, first))
            && leaves.Any(item => ReferenceEquals(item, second));
    }
}
