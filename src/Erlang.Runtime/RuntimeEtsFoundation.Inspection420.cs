using System.Runtime.CompilerServices;
namespace Erlang;

internal sealed partial class RuntimeEtsFoundation
{
    private readonly ConditionalWeakTable<ProcessContext, EtsInspectionFlag420> superUsers420 = new();

    private bool IsSuperUser420(ProcessContext context) => superUsers420.TryGetValue(context, out _);

    internal Term CoreSuperUser420(ProcessContext context, Term enabled)
    {
        lock (gate)
        {
            RequireActor(context);
            if (enabled is Atom { Name: ErlangBooleanAtoms.True }) _ = superUsers420.GetValue(context, _ => new EtsInspectionFlag420());
            else if (enabled is Atom { Name: ErlangBooleanAtoms.False }) superUsers420.Remove(context);
            else throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);

            return Term.A(EtsInspectionNames420.Ok);
        }
    }

    internal Term CoreRawKey420(
        ProcessContext context,
        Term identifier,
        Term key,
        bool next
    )
    {
        lock (gate)
        {
            if (identifier is not Atom && (identifier is not ResourceReferenceTerm resource || resource.Entry.Kind != ReferenceResourceKind.EtsTable)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Type);
            var table = CoreResolve407(context, identifier, false);
            var options = coreOptions407.GetValue(table, _ => throw new InvalidOperationException(EtsIndexNames420.InvalidIndex));
            if (options.Kind == EtsCoreTableKind407.OrderedSet) return CoreTraverse416(
                context,
                identifier,
                key,
                next,
                false,
                false
            );
            var index = table.Primary420 ?? throw new InvalidOperationException(EtsIndexNames420.InvalidIndex);
            EtsKeyGroup420? current = next ? CoreKeyGroup420(table, key, context.Cancellation) : null;
            if (next && current is null) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.None);
            bool after = !next;
            foreach (var group in index.Groups)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                if (!after)
                {
                    if (ReferenceEquals(group, current)) after = true;
                    continue;
                }

                // Unlike public first/next, raw inspection includes retained pseudo-deleted keys.
                return CoreKey407(CoreRead407(group.KeyPayload, context.Cancellation), options.KeyPosition);
            }

            return Term.A(EtsTraversalNames416.End);
        }
    }

    internal Term CoreBinaryInfo420(ProcessContext context, Term identifier, Term key)
    {
        lock (gate)
        {
            if (identifier is not Atom && (identifier is not ResourceReferenceTerm resource || resource.Entry.Kind != ReferenceResourceKind.EtsTable)) throw new RuntimeEtsCoreFailure407(RuntimeEtsCoreCause407.Type);
            var table = CoreResolve407(context, identifier, false);
            _ = CoreKeyGroup420(table, key, context.Cancellation);

            // The actual copied payload codec stores all binary data in owned managed byte[].
            // It has no refcounted offheap Binary headers. External resource/image roots are not byte-array Binary backing.
            return Nil.Value;
        }
    }
}
