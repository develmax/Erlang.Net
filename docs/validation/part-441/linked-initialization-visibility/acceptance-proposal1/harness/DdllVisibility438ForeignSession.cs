namespace Erlang;

internal static class DdllVisibility438ForeignSession
{
    internal static async Task Run(DriverParentHarnessInput423 input, RuntimeDdllParent422 target, DdllLinkedVisibility438 current)
    {
        var other = await DriverParentHarnessFixture423.Create(input, "visibility438-foreign-real-vm", false);
        await using var actor = new DriverParentHarnessActor423(other.Runtime);
        using var events = new DdllVisibility438Events(other.Environment, DdllVisibility438Names.Healthy);
        Task<PortTerm>? opening = null;
        var failures = new List<Exception>();
        try
        {
            await DdllVisibility438Checks.Load(other, DdllVisibility438Names.Healthy);
            opening = DdllVisibility438Checks.Open(actor, other.Runtime, DdllVisibility438Names.Healthy);
            events.WaitEntered();
            var foreign = (DdllLinkedVisibility438)DriverParentHarnessAccess423.Field(
                DdllVisibility438Checks.Parent(other.Runtime), DdllVisibility438Names.Scope);
            // Fresh real VM counters intentionally coincide. The actual distinct
            // authenticated session, not counter luck, has to refuse this scope.
            DriverParentHarnessAccess423.Require(foreign.Scope.OwnerImage == current.Scope.OwnerImage
                && foreign.Scope.Token == current.Scope.Token && foreign.Scope.Session != current.Scope.Session);
            DdllVisibility438Checks.Refuses(() => target.EndLinkedInitialization438(foreign.Scope, true));
            DriverParentHarnessAccess423.Require(!current.Completion.Task.IsCompleted && !foreign.Completion.Task.IsCompleted);
            events.Release.Set();
            PortTerm port = await opening.WaitAsync(DdllVisibility438Names.Deadline);
            DriverParentHarnessAccess423.Require(events.Completed.WaitOne(DdllVisibility438Names.Deadline));
            await actor.Invoke(async context => await other.Runtime.Modules.Call(context,
                DdllVisibility438Names.Runtime, DdllVisibility438Names.Close, port));
        }
        catch (Exception error) { failures.Add(error); }
        finally
        {
            events.Release.Set();
            if (opening is not null)
                try { await opening.WaitAsync(DdllVisibility438Names.Deadline); }
                catch (Exception error) { failures.Add(error); }
            try { await other.DisposeAsync(); }
            catch (Exception error) { failures.Add(error); }
            try
            {
                DriverParentHarnessAccess423.ZeroLeases(other.Runtime, other.Environment, true);
                DriverParentHarnessAccess423.Require(DdllVisibility438Checks.Child(other.Runtime).PhysicalCleanup.IsCompletedSuccessfully);
            }
            catch (Exception error) { failures.Add(error); }
        }
        if (failures.Count != 0) throw new AggregateException(failures);
    }
}
