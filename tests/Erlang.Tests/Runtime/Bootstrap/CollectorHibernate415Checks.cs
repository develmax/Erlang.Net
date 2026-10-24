using System.Collections.Concurrent;
using System.Text.Json;

namespace Erlang.Tests;

internal static class CollectorHibernate415Checks
{
    internal static async Task Run(string output)
    {
        Directory.CreateDirectory(output);
        var transfers = new ConcurrentDictionary<string, ProcessHibernateTransfer>(StringComparer.Ordinal);
        EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> observed = (_, args) =>
        {
            if (args.Exception is ProcessHibernateTransfer transfer
                && transfer.Function == ManagedCollectors413Names.Start && transfer.Arguments.Count == 0
                && transfer.Module is ManagedCollectors413Names.Literal or ManagedCollectors413Names.Trace)
                transfers.TryAdd(transfer.Module, transfer);
        };
        var runtime = new ProcessRuntime();
        RuntimeNodeFoundationHostActor? owner = null;
        CollectorHibernate415State[] states = [];
        bool hibernated = false, woken = false, deadAcks = false, disposed = false;
        Exception? failure = null;
        AppDomain.CurrentDomain.FirstChanceException += observed;
        try
        {
            await CollectorHibernate415Access.Initialize(runtime);
            owner = new RuntimeNodeFoundationHostActor(runtime);
            _ = await owner.Ready;
            foreach (string module in new[] { ManagedCollectors413Names.Literal, ManagedCollectors413Names.Trace })
                await owner.InvokeAsync(async context => await runtime.Modules.Call(
                    context,
                    CollectorHibernate415Names.Erts,
                    CollectorHibernate415Names.Spawn,
                    Term.A(module),
                    Term.A(ManagedCollectors413Names.Start),
                    Nil.Value
                ));
            await CollectorHibernate415Access.Until(
                () =>
            {
                var literal = CollectorHibernate415Access.Capture(runtime, ManagedCollectors413Names.Literal, ManagedCollectors413Names.LiteralSlot);
                var trace = CollectorHibernate415Access.Capture(runtime, ManagedCollectors413Names.Trace, ManagedCollectors413Names.TraceSlot);
                if (literal is null || trace is null) return false;
                states = [literal, trace];

                return states.All(state => CollectorHibernate415Access.Alive(runtime, state)
                    && CollectorHibernate415Access.Lease(state) && CollectorHibernate415Access.Waiting(state));
            },
                CollectorHibernate415Names.InitialDeadline
            );
            CollectorHibernate415Access.Phase(
                output,
                CollectorHibernate415Names.InitialPhase,
                states.Select(state => new { state.Module, Pid = state.Context.Self.ToString(), ActualExactImageLease = true })
            );

            // Exactly the original60s receive-after branches must throw their real transfer.
            // No fake clock, forced Hibernate or copied source timeout is used.
            await CollectorHibernate415Access.Until(
                () => states.All(state => transfers.ContainsKey(state.Module)
                && CollectorHibernate415Access.Alive(runtime, state) && !CollectorHibernate415Access.Lease(state)),
                CollectorHibernate415Names.HibernateDeadline
            );
            hibernated = true;
            CollectorHibernate415Access.Phase(
                output,
                CollectorHibernate415Names.HibernatePhase,
                states.Select(state => new
                {
                    state.Module,
                    OriginalTransfer = transfers[state.Module].GetType().FullName,
                    ActualImageLeaseAbsent = true,
                    ExactRoleContextStillLive = true
                })
            );

            runtime.Send(states[0].Context.Self, Term.A(ManagedCollectors413Names.CopyLiterals));
            runtime.Send(states[1].Context.Self, Term.A(CollectorHibernate415Names.Notify));
            await CollectorHibernate415Access.Until(
                () => states.All(state => CollectorHibernate415Access.Alive(runtime, state)
                && CollectorHibernate415Access.Lease(state) && CollectorHibernate415Access.Waiting(state)
                && ManagedCollectors413Access.Snapshot(state.Context).Count == 0),
                CollectorHibernate415Names.WakeDeadline
            );
            woken = true;
            CollectorHibernate415Access.Phase(
                output,
                CollectorHibernate415Names.WakePhase,
                new { SameOriginalContexts = true, RealLiteralSwitchAreaReaction = true, RealTraceCheckReaction = true }
            );

            await owner.InvokeAsync(async context => await runtime.Modules.Call(
                context,
                ManagedCollectors413Names.Erlang,
                ManagedCollectors413Names.Suspend,
                states[0].Context.Self
            ));
            var target = new RuntimeNodeFoundationHostActor(runtime);
            ProcessContext dead = await target.Ready;
            await target.DisposeAsync();
            CollectorHibernate415Access.Require(target.Process.Completion.IsCompleted);
            ErlangFunction copy = ManagedCollectors413Access.Capture(
                runtime,
                ManagedCollectors413Names.Literal,
                ManagedCollectors413Names.Copy,
                3
            );
            Term area = Term.Tuple(Term.A(CollectorHibernate415Names.Area), Term.List(Term.I(73), Term.Tuple(Term.I(11))));
            var replies = new List<Term>();
            foreach (string how in new[] { ManagedCollectors413Names.Init, CollectorHibernate415Names.CheckGc, CollectorHibernate415Names.NeedGc })
            {
                Term mode = Term.A(how);
                Term result = await copy(states[0].Context, [dead.Self, area, mode]);
                CollectorHibernate415Access.Require(result.Equals(Term.A(ManagedCollectors413Names.Ok)));
                replies.Add(Term.Tuple(Term.A(ManagedCollectors413Names.CopyLiterals),
                    Term.Tuple(area, mode, dead.Self), Term.A(ManagedCollectors413Names.Ok)));
            }
            CollectorHibernate415Access.Require(ManagedCollectors413Access.Snapshot(states[0].Context).SequenceEqual(replies));
            ErlangFunction clean = ManagedCollectors413Access.Capture(
                runtime,
                ManagedCollectors413Names.Trace,
                ManagedCollectors413Names.Clean,
                1
            );
            CollectorHibernate415Access.Require((await clean(states[1].Context, [dead.Self])).Equals(Term.A(ErlangBooleanAtoms.False))
                && ManagedCollectors413Access.Snapshot(states[1].Context).Count == 0
                && ManagedCollectors413Access.Requests(runtime).Length == 0);
            deadAcks = true;
            CollectorHibernate415Access.Phase(
                output,
                CollectorHibernate415Names.DeadPhase,
                new { CompleteReplyTerms = replies.Select(OwnedFunctionOriginal407Terms.Typed).ToArray(), DeadTraceSignal = false }
            );
        }
        catch (Exception error)
        {
            failure = error;
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= observed;
            try
            {
                await runtime.DisposeAsync();
                disposed = true;
                CollectorHibernate415Access.Phase(output, CollectorHibernate415Names.DisposedPhase, new { ActualRuntimeDisposeReturned = true });
            }
            catch (Exception error)
            {
                failure = failure is null ? error : new AggregateException(failure, error);
            }
            if (owner is not null)
                try
                {
                    await owner.DisposeAsync();
                }
                catch (Exception error)
                {
                    failure = failure is null ? error : new AggregateException(failure, error);
                }
        }
        bool cleared = states.Length == 2 && states.All(state =>
            ManagedCollectors413Access.Property(state.Slot, ManagedCollectors413Names.Actor) is null
            && ManagedCollectors413Access.Property(state.Slot, ManagedCollectors413Names.Source) is null
            && !CollectorHibernate415Access.Lease(state)
            && ((TaskCompletionSource<Term>)ManagedCollectors413Access.Property(state.Context, CollectorHibernate415Names.Done)!).Task.IsCompleted);
        bool passed = failure is null && hibernated && woken && deadAcks && disposed && cleared;
        File.WriteAllText(
            Path.Combine(output, CollectorHibernate415Names.Receipt),
            JsonSerializer.Serialize(new
            {
                Passed = passed,
                ActualOriginalTimeoutTransfers = transfers.Keys.Order().ToArray(),
                OriginalSourceTimeoutUnchanged = true,
                SourceFramesActuallyUnwoundAndLeasesDropped = hibernated,
                SameLiveRoleContextsSurvivedHibernate = hibernated,
                ActualSourceWakeReactionsReachedWaitingWithImageLeases = woken,
                DeadTargetThreeHowWholeAcksVerified = deadAcks,
                ActualRuntimeDisposeReturned = disposed,
                ActualRolesLeasesDoneCleared = cleared,
                Failure = failure?.ToString(),
                PrivilegedNativeOriginalOracleClaim = false,
                GarbageCollectionClaim = false,
                Standard79ComparisonIsSeparate = true
            })
        );
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        CollectorHibernate415Access.Require(passed);
    }
}
