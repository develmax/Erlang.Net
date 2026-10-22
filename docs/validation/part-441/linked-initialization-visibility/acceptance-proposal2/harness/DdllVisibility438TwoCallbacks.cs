namespace Erlang;

internal static class DdllVisibility438TwoCallbacks
{
    internal static async Task<object> Run(DriverParentHarnessInput423 input)
    {
        var fixture = await DriverParentHarnessFixture423.Create(input, "visibility438-two-callbacks", false);
        var runtime = fixture.Runtime;
        await using var first = new DriverParentHarnessActor423(runtime);
        await using var second = new DriverParentHarnessActor423(runtime);
        await using var reader = new DriverParentHarnessActor423(runtime);
        using var firstEvents = new DdllVisibility438Events(fixture.Environment, DdllVisibility438Names.Healthy);
        using var secondEvents = new DdllVisibility438Events(fixture.Environment, DdllVisibility438Names.Failed);
        Task<Term>? firstControl = null, secondControl = null;
        PortTerm? firstPort = null, secondPort = null;
        Task<Term>? read = null;
        var failures = new List<string>();
        bool bothNativeAttempts = false, distinctRoots = false;
        Term? result = null;
        try
        {
            await DdllVisibility438Checks.Load(fixture, DdllVisibility438Names.Healthy);
            await DdllVisibility438Checks.Load(fixture, DdllVisibility438Names.Failed);
            DdllVisibility438Events.Put(fixture.Environment, DdllVisibility438Names.PhaseKey + DdllVisibility438Names.Healthy,
                DdllVisibility438Names.ControlPhase);
            DdllVisibility438Events.Put(fixture.Environment, DdllVisibility438Names.PhaseKey + DdllVisibility438Names.Failed,
                DdllVisibility438Names.ControlPhase);
            firstPort = await DdllVisibility438Checks.Open(first, runtime, DdllVisibility438Names.Healthy);
            secondPort = await DdllVisibility438Checks.Open(second, runtime, DdllVisibility438Names.Failed);
            DriverParentHarnessAccess423.Require(!firstEvents.Attempt.WaitOne(0) && !secondEvents.Attempt.WaitOne(0));
            firstControl = Add(first, runtime, firstPort);
            firstEvents.WaitEntered();
            var parent = DdllVisibility438Checks.Parent(runtime);
            var firstRoot = (DdllLinkedVisibility438)DriverParentHarnessAccess423.Field(parent, DdllVisibility438Names.Scope);
            secondControl = Add(second, runtime, secondPort);
            DriverParentHarnessAccess423.Require(secondEvents.Attempt.WaitOne(DdllVisibility438Names.Deadline));
            bothNativeAttempts = firstEvents.Attempt.WaitOne(0);
            DriverParentHarnessAccess423.Require(bothNativeAttempts && !secondEvents.Entered.WaitOne(0)
                && ReferenceEquals(firstRoot, DriverParentHarnessAccess423.Field(parent, DdllVisibility438Names.Scope)));
            firstEvents.Release.Set();
            secondEvents.WaitEntered();
            var secondRoot = (DdllLinkedVisibility438)DriverParentHarnessAccess423.Field(parent, DdllVisibility438Names.Scope);
            distinctRoots = firstRoot.Scope.Token != secondRoot.Scope.Token && firstRoot.Scope.OwnerImage != secondRoot.Scope.OwnerImage;
            DriverParentHarnessAccess423.Require(distinctRoots && firstRoot.Completion.Task.IsCompletedSuccessfully);
            var attempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            read = reader.Invoke(context =>
            {
                ValueTask<Term> pending = parent.LoadedDrivers(context);
                DriverParentHarnessAccess423.Require(!pending.IsCompleted);
                attempted.SetResult();
                return pending.AsTask();
            });
            await attempted.Task.WaitAsync(DdllVisibility438Names.Deadline);
            DriverParentHarnessAccess423.Require(!read.IsCompleted && !secondRoot.Completion.Task.IsCompleted);
            secondEvents.Release.Set();
            result = await read.WaitAsync(DdllVisibility438Names.Deadline);
            DriverParentHarnessAccess423.Require(DdllVisibility438Checks.Contains(result, DdllVisibility438Names.Healthy + "_a")
                && DdllVisibility438Checks.Contains(result, DdllVisibility438Names.Healthy + "_b")
                && !DdllVisibility438Checks.Contains(result, DdllVisibility438Names.Failed + "_a")
                && !DdllVisibility438Checks.Contains(result, DdllVisibility438Names.Failed + "_b"));
            DriverParentHarnessAccess423.Require(firstEvents.Completed.WaitOne(DdllVisibility438Names.Deadline)
                && secondEvents.Completed.WaitOne(DdllVisibility438Names.Deadline));
            foreach (Task<Term> control in new[] { firstControl!, secondControl! })
            {
                Term status = await control.WaitAsync(DdllVisibility438Names.Deadline);
                DriverParentHarnessAccess423.Require(ErlangIoData.Flatten(status, false, false).ToArray().SequenceEqual(new byte[] { 0 }));
            }
            foreach (var pair in new[] { (first, firstPort), (second, secondPort) })
                await pair.Item1.Invoke(async context => await runtime.Modules.Call(context,
                    DdllVisibility438Names.Runtime, DdllVisibility438Names.Close, pair.Item2!));
        }
        catch (Exception error) { failures.Add(error.ToString()); }
        finally
        {
            firstEvents.Release.Set();
            secondEvents.Release.Set();
            foreach (Task? task in new Task?[] { firstControl, secondControl, read })
                if (task is not null)
                    try { await task.WaitAsync(DdllVisibility438Names.Deadline); }
                    catch (Exception error) { failures.Add(error.ToString()); }
            try
            {
                await fixture.DisposeAsync();
                DriverParentHarnessAccess423.ZeroLeases(runtime, fixture.Environment, true);
                var child = DdllVisibility438Checks.Child(runtime);
                DriverParentHarnessAccess423.Require(child.PhysicalExit.IsCompletedSuccessfully
                    && child.PhysicalCleanup.IsCompletedSuccessfully && child.Completion.IsCompletedSuccessfully);
            }
            catch (Exception error) { failures.Add(error.ToString()); }
        }
        var report = new { Passed = failures.Count == 0, BothNativeAttempts = bothNativeAttempts,
            DistinctActualRoots = distinctRoots, AlreadyOpenIndependentPorts = firstPort is not null && secondPort is not null, FullRead = result is null ? null : DriverParentHarnessTerms423.Typed(result), Failures = failures };
        File.WriteAllText(Path.Combine(fixture.Directory, "two-callbacks.json"),
            System.Text.Json.JsonSerializer.Serialize(report, DriverParentHarnessJson423.Options));
        DriverParentHarnessAccess423.Require(report.Passed);
        return report;
    }

    private static Task<Term> Add(DriverParentHarnessActor423 actor, ProcessRuntime runtime, PortTerm port)
        => actor.Invoke(async context => await runtime.Modules.Call(context,
            DdllVisibility438Names.Runtime, DdllVisibility438Names.Control, port,
            Term.I(DdllVisibility438Names.AddCommand), Nil.Value));
}
