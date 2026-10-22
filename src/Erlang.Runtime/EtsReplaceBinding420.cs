namespace Erlang;

internal sealed class EtsReplaceBinding420
{
    private readonly ModuleRegistry registry;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal EtsReplaceBinding420(ProcessRuntime runtime, ModuleRegistry registry)
    {
        Runtime = runtime;
        this.registry = registry;
        Body = Invoke;
    }

    private async ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, Runtime) || !ReferenceEquals(context.Runtime.Modules, registry))
            throw new ArgumentException(EtsReplaceNames420.Foreign, nameof(context));
        if (arguments.Count != EtsReplaceNames420.Arity) throw new ArgumentException(EtsReplaceNames420.Signature);
        Runtime.RequireMatchSpecActor419(context);
        context.Cancellation.ThrowIfCancellationRequested();
        try
        {
            return await Runtime.EtsFoundation.CoreReplace420(context, arguments[0], arguments[1]).ConfigureAwait(false);
        }
        catch (EtsMutationFailure414 failure)
        {
            throw context.CaptureNativeFailureWithInfo(
                new ErlangException(ErlangErrorReasons.BadArgument),
                EtsReplaceNames420.Module,
                EtsReplaceNames420.Function,
                arguments,
                new MapTerm([new KeyValuePair<Term, Term>(Term.A(RuntimeExceptionNames.Module), Term.A(RuntimeEtsFacetNames407.Formatter)), new KeyValuePair<Term, Term>(Term.A(RuntimeEtsFacetNames407.Cause), Term.A(failure.Cause))])
            );
        }
        catch (ErlangException error)
        {
            if (error.AutomaticStackTraceCaptured || error.ExplicitStackTrace) throw;
            throw context.CaptureNativeFailureWithInfo(
                error,
                EtsReplaceNames420.Module,
                EtsReplaceNames420.Function,
                arguments,
                new MapTerm([new KeyValuePair<Term, Term>(Term.A(RuntimeExceptionNames.Module), Term.A(RuntimeEtsFacetNames407.Formatter))])
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
            var entries = new List<KeyValuePair<Term, Term>>
            {
                new(Term.A(RuntimeExceptionNames.Module), Term.A(RuntimeEtsFacetNames407.Formatter))
            };
            if (cause is not null) entries.Add(new(Term.A(RuntimeEtsFacetNames407.Cause), Term.A(cause)));
            throw context.CaptureNativeFailureWithInfo(
                new ErlangException(ErlangErrorReasons.BadArgument),
                EtsReplaceNames420.Module,
                EtsReplaceNames420.Function,
                arguments,
                new MapTerm(entries)
            );
        }
    }
}
