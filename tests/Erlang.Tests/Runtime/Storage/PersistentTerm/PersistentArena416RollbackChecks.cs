namespace Erlang.Tests;

internal static class PersistentArena416RollbackChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var foreign = new ProcessRuntime(1);
        object store = PersistentArena416HostAccess.Create(runtime);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await using var other = new RuntimeNodeFoundationHostActor(foreign);
        try
        {
            await actor.InvokeAsync(async context =>
            {
                Term key = Term.A(PersistentArena416HostNames.Key);
                await PersistentArena416HostAccess.Put(
                    store,
                    context,
                    key,
                    Term.I(1)
                );
                object before = PersistentArena416HostAccess.Info(store, context);
                object allocations = PersistentArena416HostAccess.Allocations(store);
                object known = IdentifierLifetimeHostAccess.Known(runtime);
                object reservations = known.GetType().GetField(PersistentEpoch416HostNames.Reservations,
                    PersistentArena416HostNames.Members)!.GetValue(known)!;
                long epochsBefore = PersistentArena416HostAccess.Number(reservations, PersistentArena416HostNames.Count);
                var prefix = runtime.ReferenceResources.CreateBinary(PersistentArena416HostNames.ResourceBytes);
                bool refused = false;
                try
                {
                    await PersistentArena416HostAccess.Put(
                        store,
                        context,
                        key,
                        Term.Tuple(
                            prefix,
                            new Pid(PersistentEpoch416HostNames.RollbackNode, 1, PersistentEpoch416HostNames.Creation),
                            new FunctionTerm(0, (_, _) => ValueTask.FromResult<Term>(Nil.Value))
                        )
                    );
                }
                catch (NotSupportedException)
                {
                    refused = true;
                }
                PersistentArena416HostAccess.Require(refused && epochsBefore == PersistentArena416HostAccess.Number(reservations, PersistentArena416HostNames.Count)
                    && before.Equals(PersistentArena416HostAccess.Info(store, context))
                    && allocations.Equals(PersistentArena416HostAccess.Allocations(store))
                    && PersistentArena416HostAccess.Get(store, context, key).Equals(Term.I(1)));
                bool conflict = false;
                try
                {
                    await PersistentArena416HostAccess.Put(
                        store,
                        context,
                        key,
                        Term.I(2),
                        true
                    );
                }
                catch (ErlangException error)
                {
                    conflict = error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument));
                }
                PersistentArena416HostAccess.Require(conflict && before.Equals(PersistentArena416HostAccess.Info(store, context)));

                return true;
            });
            await other.Invoke(context =>
            {
                bool refused = false;
                try
                {
                    _ = PersistentArena416HostAccess.Get(store, context, Term.A(PersistentArena416HostNames.Key));
                }
                catch (ArgumentException)
                {
                    refused = true;
                }
                PersistentArena416HostAccess.Require(refused);

                return true;
            });
            await actor.InvokeAsync(async context =>
            {
                object before = PersistentArena416HostAccess.Allocations(store);
                object known = IdentifierLifetimeHostAccess.Known(runtime);
                object reservations = known.GetType().GetField(PersistentEpoch416HostNames.Reservations,
                    PersistentArena416HostNames.Members)!.GetValue(known)!;
                long epochsBefore = PersistentArena416HostAccess.Number(reservations, PersistentArena416HostNames.Count);
                ((CancellationTokenSource)typeof(ProcessContext).GetProperty(PersistentArena416HostNames.Stop,
                    PersistentArena416HostNames.Members)!.GetValue(context)!).Cancel();
                bool refused = false;
                try
                {
                    await PersistentArena416HostAccess.Put(
                        store,
                        context,
                        Term.A(PersistentArena416HostNames.Key),
                        Term.I(3)
                    );
                }
                catch (OperationCanceledException)
                {
                    refused = true;
                }
                PersistentArena416HostAccess.Require(refused && epochsBefore == PersistentArena416HostAccess.Number(reservations, PersistentArena416HostNames.Count)
                    && before.Equals(PersistentArena416HostAccess.Allocations(store)));

                return true;
            });
        }
        finally
        {
            await PersistentArena416HostAccess.Close(store);
        }
        PersistentArena416HostAccess.Empty(store);
    }
}
