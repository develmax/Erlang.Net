namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal Term CoreGiveAway419(
        ProcessContext context,
        Term identifier,
        Term receiver,
        Term gift
    )
    {
        RuntimeEtsFoundationTable admitted;
        ProcessContext target;
        lock (gate)
        {
            admitted = CoreResolve407(context, identifier, true);
            if (receiver is not Pid pid || runtime.ReadEtsHeirProcess419(pid) is not { } alive) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            target = alive;
            if (!admitted.Owner.Equals(context.Self)) throw new EtsMutationFailure414(EtsHeirNames419.NotOwner);
            if (target.Self.Equals(context.Self)) throw new EtsMutationFailure414(EtsHeirNames419.Owner);
        }
        RuntimeEtsStoredObject staged = RuntimeEtsStoredObject.Capture(
            accounting,
            runtime,
            Term.Tuple(gift),
            context.Cancellation
        );
        try
        {
            Term detached;
            using (var read = staged.Acquire()) detached = ((TupleTerm)read.Materialize(context.Cancellation)).Items[0];
            while (true)
            {
                Atom name;
                Term currentIdentifier;
                lock (gate)
                {
                    var table = CoreResolve407(context, admitted.Handle, true);
                    if (!ReferenceEquals(table, admitted)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Id);
                    name = table.Name;
                    currentIdentifier = table.Named ? name : table.Handle;
                }
                var admission = target.Mailbox.PrepareEtsTransfer419(target,
                 Term.Tuple(
                     Term.A(EtsHeirNames419.Transfer),
                     currentIdentifier,
                     context.Self,
                     detached
                 ), context.Self);
                lock (gate)
                {
                    var table = CoreResolve407(context, admitted.Handle, true);
                    if (!ReferenceEquals(table, admitted)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Id);
                    if (receiver is not Pid pid || !ReferenceEquals(runtime.ReadEtsHeirProcess419(pid), target)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
                    if (!table.Owner.Equals(context.Self)) throw new EtsMutationFailure414(EtsHeirNames419.NotOwner);
                    if (target.Self.Equals(context.Self)) throw new EtsMutationFailure414(EtsHeirNames419.Owner);
                    if (table.Named && !table.Name.Equals(name)) continue;
                    context.Cancellation.ThrowIfCancellationRequested();
                    admission.Validate(target.Mailbox);
                    MoveOwner419(table, target);
                    admission.Track(target);
                    admission.Commit(target.Mailbox);
                }
                admission.Notify();

                return Term.A(ErlangBooleanAtoms.True);
            }
        }
        finally
        {
            staged.Release();
        }
    }
}
