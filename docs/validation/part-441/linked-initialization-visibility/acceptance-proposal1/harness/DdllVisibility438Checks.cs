using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Erlang;

internal static class DdllVisibility438Checks
{
    internal static async Task<object> Run(DriverParentHarnessInput423 input)
    {
        DriverParentHarnessAccess423.Require(OperatingSystem.IsWindows() && input.SystemInfo.SchedulerThreads >= 3);
        var results = new List<object>();
        var failures = new List<Exception>();
        foreach (DdllVisibility438Mode mode in Enum.GetValues<DdllVisibility438Mode>())
        {
            try { results.Add(await Held(input, mode)); }
            catch (Exception error) { failures.Add(new InvalidDataException(mode.ToString(), error)); }
        }
        try { results.Add(await DdllVisibility438TwoCallbacks.Run(input)); }
        catch (Exception error) { failures.Add(error); }
        File.WriteAllText(Path.Combine(input.OutputDirectory, "visibility-all.json"), JsonSerializer.Serialize(
            new { Passed = failures.Count == 0, Results = results, Failures = failures.Select(error => error.ToString()).ToArray() },
            DriverParentHarnessJson423.Options));
        if (failures.Count != 0) throw new AggregateException(failures);
        return results;
    }

    private static async Task<object> Held(DriverParentHarnessInput423 input, DdllVisibility438Mode mode)
    {
        string name = "visibility438-" + mode;
        var fixture = await DriverParentHarnessFixture423.Create(input, name, false);
        var runtime = fixture.Runtime;
        var owner = Parent(runtime);
        var child = Child(runtime);
        var failures = new List<string>();
        var expectedFailures = new List<string>();
        DriverParentHarnessActor423? opener = null, reader = null, crossOpener = null;
        DdllVisibility438Events? events = null;
        Task<PortTerm>? opening = null;
        Task<Term>? reading = null;
        Task<Term>? loading = null;
        Task? closing = null;
        PortTerm? crossPort = null;
        DdllParentRow422? crossRow = null;
        ErlDriverLinkedScope438? capturedScope = null;
        bool rootPending = false, readerAttempted = false, physicalFailure = false;
        Term? completeRead = null;
        try
        {
            bool rootInit = mode == DdllVisibility438Mode.DynamicInitializer;
            string driver = rootInit ? DdllVisibility438Names.Plain
                : mode == DdllVisibility438Mode.CrossImageRemove ? DdllVisibility438Names.Cross
                : mode == DdllVisibility438Mode.Failed ? DdllVisibility438Names.Failed : DdllVisibility438Names.Healthy;
            if (mode == DdllVisibility438Mode.CrossImageRemove)
            {
                await Load(fixture, DdllVisibility438Names.Healthy);
                crossOpener = new(runtime);
                crossPort = await Open(crossOpener, runtime, DdllVisibility438Names.Healthy);
                crossRow = Row(owner, DdllVisibility438Names.Healthy + "_b");
            }
            events = new(fixture.Environment, driver);
            if (mode == DdllVisibility438Mode.CrossImageRemove)
                DdllVisibility438Events.Put(fixture.Environment,
                    DdllVisibility438Names.RemoveKey + driver, DdllVisibility438Names.Healthy + ".dll");
            if (rootInit)
            {
                DdllVisibility438Events.Put(fixture.Environment, DdllVisibility438Names.PhaseKey + driver, DdllVisibility438Names.RootPhase);
                loading = Load(fixture, driver);
            }
            else
            {
                await Load(fixture, driver);
                opener = new(runtime);
                opening = Open(opener, runtime, driver);
            }
            events.WaitEntered();
            var root = (DdllLinkedVisibility438)DriverParentHarnessAccess423.Field(owner, DdllVisibility438Names.Scope);
            capturedScope = root.Scope;
            if (crossRow is not null) RejectOtherOwnerRegister(owner, root.Scope, crossRow);
            rootPending = !root.Completion.Task.IsCompleted;
            DriverParentHarnessAccess423.Require(opening is null || !opening.IsCompleted);
            DriverParentHarnessAccess423.Require(rootPending);
            Refuses(() => owner.EndLinkedInitialization438(root.Scope with { Session = root.Scope.Session + ".foreign" }, true));
            Refuses(() => owner.EndLinkedInitialization438(root.Scope with { Token = checked(root.Scope.Token + 1) }, true));
            if (!rootInit)
            {
                DdllParentRow422 staged = Row(owner, driver + "_b");
                Refuses(() => owner.RemoveLinked(staged.Identity, root.Scope with { Token = 0 }));
            }
            if (mode == DdllVisibility438Mode.Healthy)
                await DdllVisibility438ForeignSession.Run(input, owner, root);
            DriverParentHarnessAccess423.Require(ReferenceEquals(root,
                DriverParentHarnessAccess423.Field(owner, DdllVisibility438Names.Scope)) && !root.Completion.Task.IsCompleted);
            reader = new(runtime);
            var attempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            reading = reader.Invoke(context =>
            {
                // A synchronous real native-body call returns an incomplete
                // ValueTask here only after Stable has actually taken its wait.
                ValueTask<Term> result = owner.LoadedDrivers(context);
                DriverParentHarnessAccess423.Require(!result.IsCompleted);
                attempted.SetResult();
                return result.AsTask();
            });
            await attempted.Task.WaitAsync(DdllVisibility438Names.Deadline);
            readerAttempted = true;
            DriverParentHarnessAccess423.Require(!reading.IsCompleted && !root.Completion.Task.IsCompleted);
            if (mode == DdllVisibility438Mode.PhysicalChildFailure)
            {
                var process = (Process)DriverParentHarnessAccess423.Field(child, "process");
                process.Kill(true);
                await child.PhysicalExit.WaitAsync(DdllVisibility438Names.Deadline);
                physicalFailure = true;
            }
            else if (mode == DdllVisibility438Mode.CancelReader)
            {
                await fixture.Actor!.Invoke(context =>
                {
                    runtime.Exit(context, reader.Process.Pid, Term.A("kill"));
                    return Task.FromResult(true);
                });
                try { await reading; throw new InvalidDataException("Canceled reader returned"); }
                catch (OperationCanceledException error) { expectedFailures.Add(error.ToString()); }
                DriverParentHarnessAccess423.Require(!root.Completion.Task.IsCompleted && !reader.Process.Completion.IsCompleted);
            }
            else if (mode == DdllVisibility438Mode.RuntimeClose)
            {
                closing = runtime.DisposeAsync().AsTask();
                DriverParentHarnessAccess423.Require(!closing.IsCompleted && !root.Completion.Task.IsCompleted);
            }
            events.Release.Set();
            if (physicalFailure)
            {
                await ExpectedFault(reading, expectedFailures);
                if (opening is not null) await ExpectedFault(opening, expectedFailures);
                DriverParentHarnessAccess423.Require(root.Completion.Task.IsFaulted);
            }
            else
            {
                if (loading is not null)
                {
                    Term result = await loading.WaitAsync(DdllVisibility438Names.Deadline);
                    DriverParentHarnessAccess423.Require(result.Equals(Term.Tuple(Term.A("error"), Term.A("driver_init_failed"))));
                }
                PortTerm? port = opening is null || mode == DdllVisibility438Mode.RuntimeClose ? null : await opening.WaitAsync(DdllVisibility438Names.Deadline);
                if (mode is not DdllVisibility438Mode.CancelReader and not DdllVisibility438Mode.RuntimeClose)
                {
                    completeRead = await reading.WaitAsync(DdllVisibility438Names.Deadline);
                    DriverParentHarnessAccess423.Require(Contains(completeRead, driver + "_a") == (mode is DdllVisibility438Mode.Healthy or DdllVisibility438Mode.CrossImageRemove)
                        && Contains(completeRead, driver + "_b") == (mode is DdllVisibility438Mode.Healthy or DdllVisibility438Mode.CrossImageRemove));
                    if (mode == DdllVisibility438Mode.CrossImageRemove)
                    {
                        DriverParentHarnessAccess423.Require(crossRow!.Node.List is null
                            && Contains(completeRead, DdllVisibility438Names.Healthy + "_a")
                            && !Contains(completeRead, DdllVisibility438Names.Healthy + "_b"));

                    }
                    Refuses(() => owner.EndLinkedInitialization438(capturedScope!, true));
                    // Actual source-MFA path runs after the same native gate;
                    // keep the complete returned term in the report.
                    Term publicRead = await fixture.Actor!.Invoke(async context => await runtime.Modules.Call(context,
                        DdllVisibility438Names.Module, DdllVisibility438Names.Loaded));
                    DriverParentHarnessAccess423.Require(publicRead.Equals(completeRead));
                    if (crossPort is not null)
                        await crossOpener!.Invoke(async context => await runtime.Modules.Call(context,
                            DdllVisibility438Names.Runtime, DdllVisibility438Names.Close, crossPort));
                    if (port is not null)
                    {
                        Term status = await opener!.Invoke(async context => await runtime.Modules.Call(context,
                            DdllVisibility438Names.Runtime, DdllVisibility438Names.Control, port, Term.I(DdllVisibility438Names.StatusCommand), Nil.Value));
                        DriverParentHarnessAccess423.Require(ErlangIoData.Flatten(status, false, false).ToArray().SequenceEqual(new byte[] { 0 }));
                        await opener.Invoke(async context => await runtime.Modules.Call(context,
                            DdllVisibility438Names.Runtime, DdllVisibility438Names.Close, port));
                    }
                }
                else if (mode == DdllVisibility438Mode.RuntimeClose)
                {
                    try { await reading.WaitAsync(DdllVisibility438Names.Deadline); }
                    catch (Exception error) when (error is not TimeoutException) { expectedFailures.Add(error.ToString()); }
                    await closing!.WaitAsync(DdllVisibility438Names.Deadline);
                }
                await root.Completion.Task.WaitAsync(DdllVisibility438Names.Deadline);
                DriverParentHarnessAccess423.Require(events.Completed.WaitOne(DdllVisibility438Names.Deadline));
            }
        }
        catch (Exception error) { failures.Add(error.ToString()); }
        finally
        {
            events?.Release.Set();
            foreach (Task? pending in new Task?[] { opening, loading, reading, closing })
                if (pending is not null)
                    try { await pending.WaitAsync(DdllVisibility438Names.Deadline); }
                    catch (Exception error)
                    {
                        if (error is not TimeoutException && (physicalFailure || mode is DdllVisibility438Mode.CancelReader or DdllVisibility438Mode.RuntimeClose))
                            expectedFailures.Add(error.ToString());
                        else failures.Add(error.ToString());
                    }
            try { await fixture.DisposeAsync(); }
            catch (Exception error) { if (physicalFailure) expectedFailures.Add(error.ToString()); else failures.Add(error.ToString()); }
            foreach (IAsyncDisposable? actor in new IAsyncDisposable?[] { opener, reader, crossOpener })
                if (actor is not null)
                    try { await actor.DisposeAsync(); }
                    catch (Exception error) { failures.Add(error.ToString()); }
            try
            {
                DriverParentHarnessAccess423.ZeroLeases(runtime, fixture.Environment, true);
                DriverParentHarnessAccess423.Require(child.PhysicalExit.IsCompletedSuccessfully);
                if (!physicalFailure)
                    DriverParentHarnessAccess423.Require(child.PhysicalCleanup.IsCompletedSuccessfully && child.Completion.IsCompletedSuccessfully);
            }
            catch (Exception error) { failures.Add(error.ToString()); }
            events?.Dispose();
        }
        var report = new { Mode = mode.ToString(), Passed = failures.Count == 0, RootPending = rootPending,
            ReaderAttempted = readerAttempted, PhysicalFailureDeliberate = physicalFailure,
            FullRead = completeRead is null ? null : DriverParentHarnessTerms423.Typed(completeRead),
            Failures = failures, ExpectedFaults = expectedFailures };
        File.WriteAllText(Path.Combine(fixture.Directory, "visibility.json"), JsonSerializer.Serialize(report, DriverParentHarnessJson423.Options));
        DriverParentHarnessAccess423.Require(report.Passed);
        return report;
    }

    internal static RuntimeDdllParent422 Parent(ProcessRuntime runtime)
        => (RuntimeDdllParent422)DriverParentHarnessAccess423.Field(
            DriverParentHarnessAccess423.Field(runtime, DdllVisibility438Names.Frontend), DdllVisibility438Names.Drivers);
    internal static ErlDriverHostChild420 Child(ProcessRuntime runtime)
        => (ErlDriverHostChild420)DriverParentHarnessAccess423.Field(
            DriverParentHarnessAccess423.Field(runtime, DdllVisibility438Names.Frontend), DdllVisibility438Names.Child);
    internal static Task<Term> Load(DriverParentHarnessFixture423 fixture, string driver)
        => fixture.Actor!.Invoke(async context => await fixture.Runtime.Modules.Call(context,
            DdllVisibility438Names.Module, DdllVisibility438Names.Load,
            Term.String(fixture.Directory), Term.A(driver), Nil.Value));
    internal static Task<PortTerm> Open(DriverParentHarnessActor423 actor, ProcessRuntime runtime, string driver)
        => actor.Invoke(async context => (PortTerm)await runtime.Modules.Call(context,
            DdllVisibility438Names.Runtime, DdllVisibility438Names.Open,
            Term.Tuple(Term.A("spawn_driver"), Term.A(driver)), Term.List(Term.A("binary"))));
    internal static bool Contains(Term list, string name)
        => list is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0].Equals(Term.A("ok"))
            && Cons.Items(tuple.Items[1]).Any(item => item.Equals(Term.String(name)));
    internal static DdllParentRow422 Row(RuntimeDdllParent422 parent, string name)
    {
        lock (DriverParentHarnessAccess423.Field(parent, "gate"))
        {
            var rows = (LinkedList<DdllParentRow422>)DriverParentHarnessAccess423.Field(parent, "drivers");
            string key = Convert.ToHexString(Encoding.ASCII.GetBytes(name));
            return rows.Single(row => row.Name.Key == key);
        }
    }
    private static void RejectOtherOwnerRegister(RuntimeDdllParent422 parent, ErlDriverLinkedScope438 scope, DdllParentRow422 realOtherEntry)
    {
        DriverParentHarnessAccess423.Require(realOtherEntry.OwnerImage != scope.OwnerImage);
        ulong before = (ulong)DriverParentHarnessAccess423.Field(parent, "nextIdentity");
        ulong unexpected = 0;
        try
        {
            unexpected = parent.RegisterLinked(new(realOtherEntry.OwnerImage,
                realOtherEntry.Name.Bytes.ToArray(), realOtherEntry.NativeFlags), scope);
        }
        catch (InvalidDataException)
        {
            DriverParentHarnessAccess423.Require((ulong)DriverParentHarnessAccess423.Field(parent, "nextIdentity") == before);
            return;
        }
        finally
        {
            // A broken guard must still not expose this rejected host probe row
            // after the native gate opens. Only remove the actual returned ID.
            if (unexpected != 0) parent.RemoveLinked(unexpected, scope);
        }
        throw new InvalidDataException("Registration accepted another real image's owner under the current root");
    }
    internal static void Refuses(Action operation)
    {
        try { operation(); }
        catch (InvalidDataException) { return; }
        throw new InvalidDataException("Invalid scope was accepted");
    }
    private static async Task ExpectedFault(Task task, List<string> failures)
    {
        try { await task.WaitAsync(DdllVisibility438Names.Deadline); }
        catch (Exception error) when (error is not TimeoutException) { failures.Add(error.ToString()); return; }
        throw new InvalidDataException("Physically failed child returned success");
    }
}
