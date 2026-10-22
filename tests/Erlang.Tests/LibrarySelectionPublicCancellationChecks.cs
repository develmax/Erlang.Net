using System.Runtime.ExceptionServices;
using static Erlang.Tests.LibrarySelectionHostData;
using static Erlang.Tests.LibrarySelectionCancellationPhaseAccess;

namespace Erlang.Tests;

internal static class LibrarySelectionPublicCancellationChecks
{
    internal static Task DuringSetup() => Run(false);

    internal static Task AfterPurgerPermit() => Run(true);

    private static async Task Run(bool afterPermit)
    {
        var failures = new List<Exception>();
        var fixture = new LibrarySelectionPublicCancellationFixture(afterPermit
            ? LibrarySelectionPublicCancellationNames.PermitPhase : LibrarySelectionPublicCancellationNames.SetupPhase);
        try
        {
            fixture.Start();
            await fixture.Reached.Task.WaitAsync(LibrarySelectionPublicCancellationNames.Deadline);
            Require(fixture.InitialOwnersWereManaged && fixture.Bootstrap.Length == 2 && !fixture.Factory.IsCompleted);
            // No observer may leak to this unrelated public factory call.
            await using (var isolated = await ProcessRuntime.CreateAsync(new RuntimeLibrarySelection([]))
                .WaitAsync(LibrarySelectionPublicCancellationNames.Deadline))
                Require(isolated.LibrarySelection.Modules.Count == 0);
            if (afterPermit)
            {
                Require(fixture.Worker is not null);
                Require(Purger(fixture.Runtime).CurrentCount == 0);
                Require(!Done(fixture.Worker!).IsCompleted && !Unwound(fixture.Worker!).IsCompleted);
                Require(ActiveLeases(fixture.Worker!) == 0);
                Require(fixture.Bootstrap.All(image => fixture.Runtime.Modules.PendingPurge(image) is null));
                Require(fixture.Runtime.Modules.HasOldCode(ListBeamHostNames.Module)
                    && fixture.Runtime.Modules.HasOldCode(QueueModuleNames.Module));
            }
            fixture.Cancellation.Cancel();
            await fixture.CleanupStarted.Task.WaitAsync(LibrarySelectionPublicCancellationNames.Deadline);
            if (afterPermit)
            {
                // Retirement marking precedes the initializer/actor drain; waiting
                // for full DisposeAsync while this scheduler lane is held can deadlock.
                await fixture.RetirementMarked.Task.WaitAsync(LibrarySelectionPublicCancellationNames.Deadline);
                // Done can precede real body unwind; the public factory must wait.
                Require(!Unwound(fixture.Worker!).IsCompleted);
                Require(!LibrarySelectionPublicCancellationAccess.Dispatcher(fixture.Runtime).IsCompleted);
            }
            Require(!fixture.Factory.IsCompleted);
            fixture.Release.Set();
            await fixture.ExpectCancellation();
            Require(LibrarySelectionPublicCancellationAccess.Disposed(fixture.Runtime));
            Require(LibrarySelectionPublicCancellationAccess.Dispatcher(fixture.Runtime).IsCompletedSuccessfully);
            Require(LibrarySelectionPublicCancellationAccess.Initializer(fixture.Runtime).IsCompleted);
            Require(fixture.Runtime.LibrarySelection.Modules.Count == 0);
            lock (RuntimeGate(fixture.Runtime))
                Require(ProcessesWhileLocked(fixture.Runtime).Length == 0);
            Require(fixture.Bootstrap.All(image => fixture.Runtime.Modules.PendingPurge(image) is null));
            SemaphoreSlim purger = Purger(fixture.Runtime);
            Require(purger.CurrentCount == 1 && purger.Wait(0));
            purger.Release();
            Require(purger.CurrentCount == 1);
            if (afterPermit)
            {
                Require(Unwound(fixture.Worker!).IsCompletedSuccessfully && ActiveLeases(fixture.Worker!) == 0);
                Require((await Done(fixture.Worker!)).Equals(Term.A(ProcessExitReasons.Shutdown)));
            }
            else
            {
                Require(ReferenceEquals(fixture.Bootstrap[0], fixture.Runtime.Modules.CurrentCodeImage(ListBeamHostNames.Module)));
                Require(ReferenceEquals(fixture.Bootstrap[1], fixture.Runtime.Modules.CurrentCodeImage(QueueModuleNames.Module)));
                Require(fixture.Runtime.Modules.SourceOwner(ListBeamHostNames.Module, ListBeamHostNames.Sort, 1) is null);
                Require(fixture.Runtime.Modules.SourceOwner(QueueModuleNames.Module, QueueModuleNames.New, 0) is null);
                Require(!fixture.Runtime.Modules.HasOldCode(ListBeamHostNames.Module)
                    && !fixture.Runtime.Modules.HasOldCode(QueueModuleNames.Module));
            }
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
        finally
        {
            await fixture.Cleanup(failures);
        }
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(LibrarySelectionPublicCancellationNames.Failure, failures);
    }
}
