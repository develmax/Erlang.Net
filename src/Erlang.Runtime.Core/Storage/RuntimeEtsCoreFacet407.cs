using System.Collections.Frozen;

namespace Erlang;

// Explicit isolated native-kernel experiment only. Never auto-registers or
// relaxes the original ETS image's complete51 capture gate.
internal static class RuntimeEtsCoreFacet407
{
    internal static IReadOnlyDictionary<(string Function, int Arity), ErlangFunction> CreateBodies(ProcessRuntime owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        var result = new Dictionary<(string Function, int Arity), ErlangFunction>();
        foreach (var signature in new (string Function, int Arity)[] {
            (RuntimeEtsFacetNames407.New, 2), (RuntimeEtsFacetNames407.Whereis, 1),
            (RuntimeEtsFacetNames407.Insert, 2), (RuntimeEtsFacetNames407.Lookup, 2),
            (RuntimeEtsFacetNames407.Member, 2), (RuntimeEtsFacetNames407.Delete, 1), (RuntimeEtsFacetNames407.Delete, 2), (RuntimeEtsFacetNames407.Select, 2),
            (RuntimeEtsLookupNames411.Function, 3), (RuntimeEtsLookupNames411.Function, 4), (RuntimeEtsMatchNames411.Function, 2) })
        {
            var bound = signature;
            result.Add(bound, (context, arguments) => Invoke(
                owner,
                context,
                bound,
                arguments
            ));
        }

        return result.ToFrozenDictionary();
    }

    private static async ValueTask<Term> Invoke(
        ProcessRuntime owner,
        ProcessContext context,
        (string Function, int Arity) signature,
        IReadOnlyList<Term> arguments
    )
    {
        if (!ReferenceEquals(context.Runtime, owner)) throw new ArgumentException(RuntimeEtsFacetNames407.Foreign);
        if (arguments.Count != signature.Arity) throw new ArgumentException(RuntimeEtsFacetNames407.Signature);
        context.Cancellation.ThrowIfCancellationRequested();
        try
        {
            var core = owner.EtsFoundation;

            return signature switch
            {
                (RuntimeEtsFacetNames407.New, 2) => core.CoreNew407(context, arguments[0], arguments[1]),
                (RuntimeEtsFacetNames407.Whereis, 1) => core.CoreWhereis407(context, arguments[0]),
                (RuntimeEtsFacetNames407.Insert, 2) => await core.CoreInsert407(context, arguments[0], arguments[1]).ConfigureAwait(false),
                (RuntimeEtsFacetNames407.Lookup, 2) => core.CoreLookup407(context, arguments[0], arguments[1]),
                (RuntimeEtsLookupNames411.Function, 3) => core.CoreLookupElement411(
                    context,
                    arguments[0],
                    arguments[1],
                    arguments[2],
                    false,
                    Nil.Value
                ),
                (RuntimeEtsLookupNames411.Function, 4) => core.CoreLookupElement411(
                    context,
                    arguments[0],
                    arguments[1],
                    arguments[2],
                    true,
                    arguments[3]
                ),
                (RuntimeEtsFacetNames407.Select, 2) => core.CoreSelect409(context, arguments[0], arguments[1]),
                (RuntimeEtsMatchNames411.Function, 2) => core.CoreMatch411(context, arguments[0], arguments[1]),
                (RuntimeEtsFacetNames407.Member, 2) => core.CoreMember407(context, arguments[0], arguments[1]),
                (RuntimeEtsFacetNames407.Delete, 2) => await core.CoreDeleteKey407(context, arguments[0], arguments[1]).ConfigureAwait(false),
                (RuntimeEtsFacetNames407.Delete, 1) => await core.CoreDeleteTable407(context, arguments[0]).ConfigureAwait(false),
                _ => throw new InvalidOperationException(RuntimeEtsFacetNames407.Signature)
            };
        }
        catch (RuntimeEtsCoreFailure407 failure)
        {
            var entries = new List<KeyValuePair<Term, Term>> { new(Term.A(RuntimeExceptionNames.Module), Term.A(RuntimeEtsFacetNames407.Formatter)) };
            string? cause = failure.Cause switch
            {
                RuntimeEtsCoreCause407.Type => RuntimeEtsFacetNames407.Type,
                RuntimeEtsCoreCause407.Id => RuntimeEtsFacetNames407.Id,
                RuntimeEtsCoreCause407.Access => RuntimeEtsFacetNames407.Access,
                RuntimeEtsCoreCause407.AlreadyExists => RuntimeEtsFacetNames407.AlreadyExists,
                RuntimeEtsCoreCause407.BadKey => RuntimeEtsLookupNames411.BadKey,
                _ => null
            };
            if (cause is not null) entries.Add(new(Term.A(RuntimeEtsFacetNames407.Cause), Term.A(cause)));
            throw context.CaptureNativeFailureWithInfo(
                new ErlangException(ErlangErrorReasons.BadArgument),
                RuntimeEtsFacetNames407.Module,
                signature.Function,
                arguments,
                new MapTerm(entries)
            );
        }
    }
}
