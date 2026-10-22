using System.Diagnostics;
using Erlang;
using static Erlang.Tests.BeamDeepFrameAccess;

namespace Erlang.Tests;

internal static class BeamDeepFrameHostChecks
{
    internal static async Task Run()
    {
        var runtime = new ProcessRuntime();
        var ready = new TaskCompletionSource<(ProcessContext Owner, ProcessHandle Child, ProcessContext Context)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var childEntered = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceUnwound = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var hold = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clrSamples = new List<int>();
        object? subjectImage = null;
        bool cancelled = false;
        bool returned = false;
        bool actualWaiting = false;
        int before = -1;
        int after = -1;
        int heldFrames = -1;
        int heldFramesAfter = -1;
        Exception? failure = null;
        Exception? cleanupFailure = null;

        try
        {
            PreparedBeamImage[] images = BeamLifecycleHostChecks.PublishDeep(runtime);
            subjectImage = Image(images.Single(image => image.Name == BeamDeepFrameNames.Subject));

            ProcessHandle owner = runtime.Spawn(async observer =>
            {
                try
                {
                    foreach (int depth in new[] { 8, 8192, 16384 })
                    {
                        FunctionTerm callback = Measurement(depth, BeamDeepFrameNames.ProbeFunction);
                        Term result = await runtime.Modules.Call(
                            observer,
                            BeamDeepFrameNames.Subject,
                            BeamDeepFrameNames.Probe,
                            [Term.I(depth), callback]
                        );
                        if (!result.Equals(Term.I(depth)) || Active(observer, subjectImage) != 0
                            || SourceFrames(observer, BeamDeepFrameNames.ProbeFunction, depth) != 0)
                            throw new InvalidDataException(BeamDeepFrameNames.RecursionNotSettled);
                    }
                    FunctionTerm heldCallback = Measurement(BeamDeepFrameNames.HeldDepth, BeamDeepFrameNames.HeldFunction);
                    ProcessHandle worker = runtime.Spawn(
                        async execution =>
                    {
                        childEntered.TrySetResult(execution);
                        try
                        {
                            Term result = await runtime.Modules.Call(
                                execution,
                                BeamDeepFrameNames.Subject,
                                BeamDeepFrameNames.HeldProbe,
                                [observer.Self, Term.I(BeamDeepFrameNames.HeldDepth), heldCallback]
                            );
                            returned = true;

                            return result;
                        }
                        catch (OperationCanceledException) when (execution.Cancellation.IsCancellationRequested)
                        {
                            cancelled = true;
                            throw;
                        }
                        finally
                        {
                            sourceUnwound.TrySetResult();
                        }
                    },
                        observer
                    );
                    Term? message = await observer.ReceiveAsync<Term>(
                        value => value is TupleTerm { Items.Count: 3 } tuple
                        && tuple.Items[0] is Atom { Name: BeamDeepFrameNames.HeldMessage }
                        && tuple.Items[1].Equals(worker.Pid)
                        && tuple.Items[2].Equals(Term.I(BeamDeepFrameNames.HeldDepth)) ? value : null,
                        BeamDeepFrameNames.Deadline
                    );
                    if (message is null)
                        throw new TimeoutException(BeamDeepFrameNames.MissingDeepReady);
                    ProcessContext active = await childEntered.Task.WaitAsync(BeamDeepFrameNames.Deadline);
                    ready.TrySetResult((observer, worker, active));
                    await hold.Task.WaitAsync(observer.Cancellation);

                    return Nil.Value;
                }
                catch (Exception error)
                {
                    ready.TrySetException(error);
                    throw;
                }
            });
            var held = await ready.Task.WaitAsync(BeamDeepFrameNames.CaptureDeadline);
            CancellationToken token = held.Context.Cancellation;
            var watch = Stopwatch.StartNew();
            while (!Waiting(held.Context))
            {
                if (held.Child.Completion.IsCompleted || watch.Elapsed >= BeamDeepFrameNames.Deadline)
                    throw new TimeoutException(BeamDeepFrameNames.NotActuallyWaiting);
                await Task.Delay(5);
            }
            actualWaiting = Waiting(held.Context);
            before = Active(held.Context, subjectImage);
            heldFrames = SourceFrames(held.Context, BeamDeepFrameNames.HeldFunction, BeamDeepFrameNames.HeldDepth);
            if (!actualWaiting || before <= 0 || heldFrames != BeamDeepFrameNames.HeldDepth + 1 || token.IsCancellationRequested)
                throw new InvalidDataException(BeamDeepFrameNames.MissingDeepSourceLease);
            if (clrSamples.Count != 4 || clrSamples.Any(value => value <= 0 || value > BeamDeepFrameNames.MaximumSampleClrFrames)
                || clrSamples.Skip(1).Any(value => value > clrSamples[0] + BeamDeepFrameNames.MaximumClrFrameGrowth))
                throw new InvalidDataException(BeamDeepFrameNames.ClrStackBoundExceeded);

            await runtime.DisposeAsync().AsTask().WaitAsync(BeamDeepFrameNames.CaptureDeadline);
            await sourceUnwound.Task.WaitAsync(BeamDeepFrameNames.Deadline);
            await Unwound(held.Context).WaitAsync(BeamDeepFrameNames.Deadline);
            await Unwound(held.Owner).WaitAsync(BeamDeepFrameNames.Deadline);
            Term reason = await held.Child.Completion.WaitAsync(BeamDeepFrameNames.Deadline);
            Term ownerReason = await owner.Completion.WaitAsync(BeamDeepFrameNames.Deadline);
            after = Active(held.Context, subjectImage);
            heldFramesAfter = SourceFrames(held.Context, BeamDeepFrameNames.HeldFunction, BeamDeepFrameNames.HeldDepth);
            if (!cancelled || returned || !token.IsCancellationRequested || after != 0 || heldFramesAfter != 0 || !reason.Equals(Term.A(BeamDeepFrameNames.Shutdown)))
                throw new InvalidDataException(BeamDeepFrameNames.CancellationNotSettled);

        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            hold.TrySetResult();
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(BeamDeepFrameNames.CaptureDeadline);
            }
            catch (Exception error)
            {
                cleanupFailure = error;
            }
        }
        if (failure is not null || cleanupFailure is not null)
            throw new AggregateException(new[] { failure, cleanupFailure }.OfType<Exception>());

        FunctionTerm Measurement(int depth, string sourceFunction) => new(
            0,
            (execution, _) =>
        {
            if (execution is not ProcessContext context || subjectImage is null)
                throw new InvalidDataException(BeamDeepFrameNames.InvalidProbeProcess);
            // Capture synchronously at the physical leaf; no Task.Run or stack reconstruction.
            int physical = new StackTrace(false).FrameCount;
            int source = SourceFrames(context, sourceFunction, depth);
            int references = Active(context, subjectImage);
            if (source != depth + 1 || references <= 0)
                throw new InvalidDataException(BeamDeepFrameNames.MissingProbeSourceDepth);
            clrSamples.Add(physical);

            return ValueTask.FromResult<Term>(Term.A(BeamDeepFrameNames.Measured));
        }
        );
    }
}
