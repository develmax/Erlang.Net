namespace Erlang.Tests;

public static partial class NifLifetimeHostChecks
{
    public static async Task RetirementRequestsImmediateHalt()
    {
        var runtime = new ProcessRuntime();
        var first = Register(runtime.Modules, NifRetirementHaltHostNames.FirstModule);
        var second = Register(runtime.Modules, NifRetirementHaltHostNames.SecondModule);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSecond = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstOwner = new NifLifetimeTestOwner(async () =>
        {
            await NifAdmissionHostBridge.Failure(runtime.DisposeAsync().AsTask(), NifLifetimeHostNames.CleanupReentry);
            await runtime.RequestImmediateHalt(0).WaitAsync(NifLifetimeHostNames.Deadline);
            // Keep this retirement active until the halt actor reaches another owner.
            await secondEntered.Task.WaitAsync(NifLifetimeHostNames.Deadline);
        });
        var secondOwner = new NifLifetimeTestOwner(async () =>
        {
            secondEntered.TrySetResult();
            await releaseSecond.Task.WaitAsync(NifLifetimeHostNames.Deadline);
        });
        Task? retirement = null;
        try
        {
            await Install(
                runtime,
                first.Image,
                first.Owner,
                firstOwner,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
            await Install(
                runtime,
                second.Image,
                second.Owner,
                secondOwner,
                (_, _) => ValueTask.FromResult<Term>(Nil.Value)
            );
            retirement = (Task)NifAdmissionHostBridge.Call(runtime.Modules, NifLifetimeHostNames.Retire, first.Image)!;
            await secondEntered.Task.WaitAsync(NifLifetimeHostNames.Deadline);
            Require(firstOwner.Calls == 1 && secondOwner.Calls == 1 && !runtime.HaltQuiescence.IsCompleted);
            releaseSecond.TrySetResult();
            await retirement.WaitAsync(NifLifetimeHostNames.Deadline);
            await runtime.HaltQuiescence.WaitAsync(NifLifetimeHostNames.Deadline);
            Require(firstOwner.Calls == 1 && secondOwner.Calls == 1);
        }
        finally
        {
            releaseSecond.TrySetResult();
            secondEntered.TrySetResult();
            var failures = new List<Exception>();
            await CaptureCleanupFailure(retirement, failures);
            await CaptureCleanupFailure(runtime.DisposeAsync().AsTask(), failures);
            if (failures.Count != 0)
                throw new AggregateException(failures);
        }
    }
}
