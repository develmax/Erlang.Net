namespace Erlang;

internal sealed class EtsCounterBinding417
{
    private readonly ModuleRegistry registry;
    private readonly (string Function, int Arity) signature;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal EtsCounterBinding417(ProcessRuntime runtime, ModuleRegistry registry, (string Function, int Arity) signature)
    {
        Runtime = runtime;
        this.registry = registry;
        this.signature = signature;
        Body = Invoke;
    }

    private async ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, Runtime) || !ReferenceEquals(context.Runtime.Modules, registry))
            throw new ArgumentException(EtsCounterNames417.Foreign, nameof(context));
        if (arguments.Count != signature.Arity) throw new ArgumentException(EtsCounterNames417.Signature);
        context.Cancellation.ThrowIfCancellationRequested();
        try
        {
            var core = Runtime.EtsFoundation;

            return await core.CoreUpdateCounter417(
                context,
                arguments[0],
                arguments[1],
                arguments[2],
                signature.Arity == 4,
                signature.Arity == 4 ? arguments[3] : Nil.Value
            );

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
            throw Failure(context, arguments, cause);
        }
        catch (EtsMutationFailure414 failure)
        {
            throw Failure(context, arguments, failure.Cause);
        }
    }

    private ErlangException Failure(ProcessContext context, IReadOnlyList<Term> arguments, string? cause)
    {
        var entries = new List<KeyValuePair<Term, Term>> { new(Term.A(RuntimeExceptionNames.Module), Term.A(RuntimeEtsFacetNames407.Formatter)) };
        if (cause is not null) entries.Add(new(Term.A(RuntimeEtsFacetNames407.Cause), Term.A(cause)));

        return context.CaptureNativeFailureWithInfo(
            new ErlangException(ErlangErrorReasons.BadArgument),
            EtsCounterNames417.Module,
            signature.Function,
            arguments,
            new MapTerm(entries)
        );
    }
}
