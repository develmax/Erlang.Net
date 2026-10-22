namespace Erlang;

internal sealed class EtsMutationBinding414
{
    private readonly ModuleRegistry registry;
    private readonly (string Function, int Arity) signature;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal EtsMutationBinding414(ProcessRuntime runtime, ModuleRegistry registry, (string Function, int Arity) signature)
    {
        Runtime = runtime;
        this.registry = registry;
        this.signature = signature;
        Body = Invoke;
    }

    private async ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, Runtime) || !ReferenceEquals(context.Runtime.Modules, registry))
            throw new ArgumentException(EtsMutationNames414.Foreign, nameof(context));
        if (arguments.Count != signature.Arity) throw new ArgumentException(EtsMutationNames414.Signature);
        context.Cancellation.ThrowIfCancellationRequested();
        try
        {
            var core = Runtime.EtsFoundation;

            return signature switch
            {
                (EtsMutationNames414.InsertNew, 2) => await core.CoreInsertNew414(context, arguments[0], arguments[1]).ConfigureAwait(false),
                (EtsMutationNames414.Take, 2) => await core.CoreRemove414(
                    context,
                    arguments[0],
                    arguments[1],
                    take: true
                ).ConfigureAwait(false),
                (EtsMutationNames414.DeleteObject, 2) => await core.CoreRemove414(
                    context,
                    arguments[0],
                    arguments[1],
                    take: false
                ).ConfigureAwait(false),
                (EtsMutationNames414.Update, 3) => await core.CoreUpdateElement414(
                    context,
                    arguments[0],
                    arguments[1],
                    arguments[2],
                    false,
                    Nil.Value
                ).ConfigureAwait(false),
                (EtsMutationNames414.Update, 4) => await core.CoreUpdateElement414(
                    context,
                    arguments[0],
                    arguments[1],
                    arguments[2],
                    true,
                    arguments[3]
                ).ConfigureAwait(false),
                _ => throw new InvalidOperationException(EtsMutationNames414.Signature)
            };
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
            EtsMutationNames414.Module,
            signature.Function,
            arguments,
            new MapTerm(entries)
        );
    }
}
