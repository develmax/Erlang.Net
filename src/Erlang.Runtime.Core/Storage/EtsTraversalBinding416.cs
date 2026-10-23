namespace Erlang;

internal sealed class EtsTraversalBinding416
{
    private readonly ModuleRegistry registry;
    private readonly (string Function, int Arity) signature;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal EtsTraversalBinding416(ProcessRuntime runtime, ModuleRegistry registry, (string Function, int Arity) signature)
    {
        Runtime = runtime;
        this.registry = registry;
        this.signature = signature;
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, Runtime) || !ReferenceEquals(context.Runtime.Modules, registry)) throw new ArgumentException(EtsTraversalNames416.Foreign);
        if (arguments.Count != signature.Arity) throw new ArgumentException(EtsTraversalNames416.Signature);
        context.Cancellation.ThrowIfCancellationRequested();
        bool backward = signature.Function is EtsTraversalNames416.Last or EtsTraversalNames416.Previous or EtsTraversalNames416.LastLookup or EtsTraversalNames416.PreviousLookup;
        bool lookup = signature.Function is EtsTraversalNames416.FirstLookup or EtsTraversalNames416.LastLookup or EtsTraversalNames416.NextLookup or EtsTraversalNames416.PreviousLookup;
        try
        {
            return ValueTask.FromResult(Runtime.EtsFoundation.CoreTraverse416(
                context,
                arguments[0],
                signature.Arity == 2 ? arguments[1] : Nil.Value,
                signature.Arity == 2,
                backward,
                lookup
            ));
        }
        catch (RuntimeEtsCoreFailure407 failure)
        {
            string? cause = failure.Cause switch
            {
                RuntimeEtsCoreCause407.Type => RuntimeEtsFacetNames407.Type,
                RuntimeEtsCoreCause407.Id => RuntimeEtsFacetNames407.Id,
                RuntimeEtsCoreCause407.Access => RuntimeEtsFacetNames407.Access,
                _ => null
            };
            var entries = new List<KeyValuePair<Term, Term>> { new(Term.A(RuntimeExceptionNames.Module), Term.A(RuntimeEtsFacetNames407.Formatter)) };
            if (cause is not null) entries.Add(new(Term.A(RuntimeEtsFacetNames407.Cause), Term.A(cause)));
            throw context.CaptureNativeFailureWithInfo(
                new ErlangException(ErlangErrorReasons.BadArgument),
                EtsTraversalNames416.Module,
                signature.Function,
                arguments,
                new MapTerm(entries)
            );
        }
    }
}
