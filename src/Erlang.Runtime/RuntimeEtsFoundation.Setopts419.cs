namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    internal async ValueTask<Term> CoreSetopts419(ProcessContext context, Term identifier, Term options)
    {
        RuntimeEtsFoundationTable admitted;
        EtsSetoptsPlan419 plan;
        lock (gate)
        {
            admitted = CoreResolve407(context, identifier, true);
            plan = EtsSetoptsPlan419.Parse(runtime, options, context.Cancellation);
            if (!admitted.Owner.Equals(context.Self)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
        }
        RuntimeEtsStoredObject? staged = null;
        bool consumed = false, committed = false;
        RuntimeEtsRetirement? prepared = null;
        try
        {
            if (plan.ChangeHeir && plan.Heir is not null && plan.Gift is not null)
                staged = RuntimeEtsStoredObject.Capture(
                    accounting,
                    runtime,
                    Term.Tuple(plan.Gift),
                    context.Cancellation
                );
            Task retirement = Task.CompletedTask;
            lock (gate)
            {
                var table = CoreResolve407(context, admitted.Handle, true);
                if (!ReferenceEquals(table, admitted)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Id);
                if (!table.Owner.Equals(context.Self)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
                RuntimeEtsHeir419? replacement = null;
                if (plan.ChangeHeir && plan.Heir is { } pid && runtime.ReadEtsHeirProcess419(pid) is { } generation)
                    replacement = new(pid, new(generation), staged);
                if (plan.ChangeHeir && table.Heir419?.Data is { } old) prepared = CorePrepareRetirement407(context.Self, new[] { old });
                context.Cancellation.ThrowIfCancellationRequested();
                if (plan.ChangeHeir)
                {
                    table.Heir419 = replacement;
                    consumed = replacement?.Data is not null;
                }
                if (plan.Protection is { } protection) table.Protection = protection;
                committed = true;
                retirement = CoreBeginRetirement407(prepared);
            }
            await retirement.ConfigureAwait(false);

            return Term.A(ErlangBooleanAtoms.True);
        }
        finally
        {
            try
            {
                if (!committed && prepared is not null)
                {
                    prepared.Objects = null;
                    RetirementEnded(prepared, Array.Empty<Exception>());
                }
            }
            finally
            {
                if (!consumed) staged?.Release();
            }
        }
    }
}
