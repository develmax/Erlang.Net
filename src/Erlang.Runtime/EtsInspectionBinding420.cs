namespace Erlang;

internal sealed class EtsInspectionBinding420
{
    private readonly ModuleRegistry registry;
    private readonly (string Function, int Arity) signature;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal EtsInspectionBinding420(ProcessRuntime runtime, ModuleRegistry registry, (string Function, int Arity) signature)
    {
        Runtime = runtime;
        this.registry = registry;
        this.signature = signature;
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, Runtime) || !ReferenceEquals(context.Runtime.Modules, registry))
            throw new ArgumentException(EtsInspectionNames420.Foreign, nameof(context));
        if (arguments.Count != signature.Arity) throw new ArgumentException(EtsInspectionNames420.Signature);
        Runtime.RequireMatchSpecActor419(context);
        context.Cancellation.ThrowIfCancellationRequested();
        try
        {
            var core = Runtime.EtsFoundation;

            return ValueTask.FromResult(signature.Function switch
            {
                EtsInspectionNames420.SuperUser => core.CoreSuperUser420(context, arguments[0]),
                EtsInspectionNames420.First => core.CoreRawKey420(
                    context,
                    arguments[0],
                    Nil.Value,
                    false
                ),
                EtsInspectionNames420.Next => core.CoreRawKey420(
                    context,
                    arguments[0],
                    arguments[1],
                    true
                ),
                EtsInspectionNames420.Binary => core.CoreBinaryInfo420(context, arguments[0], arguments[1]),
                _ => throw new InvalidOperationException(EtsInspectionNames420.Signature)
            });

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
        catch (ErlangException error)
        {
            if (error.AutomaticStackTraceCaptured || error.ExplicitStackTrace) throw;
            throw context.CaptureNativeFailureWithInfo(
                error,
                EtsInspectionNames420.Module,
                signature.Function,
                arguments,
                new MapTerm([new KeyValuePair<Term, Term>(Term.A(RuntimeExceptionNames.Module), Term.A(EtsInspectionNames420.Formatter))])
            );
        }
    }

    private ErlangException Failure(ProcessContext context, IReadOnlyList<Term> arguments, string? cause)
    {
        var entries = new List<KeyValuePair<Term, Term>> { new(Term.A(RuntimeExceptionNames.Module), Term.A(EtsInspectionNames420.Formatter)) };
        if (cause is not null) entries.Add(new(Term.A(RuntimeEtsFacetNames407.Cause), Term.A(cause)));

        return context.CaptureNativeFailureWithInfo(
            new ErlangException(ErlangErrorReasons.BadArgument),
            EtsInspectionNames420.Module,
            signature.Function,
            arguments,
            new MapTerm(entries)
        );
    }
}
