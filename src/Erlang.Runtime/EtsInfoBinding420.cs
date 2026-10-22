namespace Erlang;

internal sealed class EtsInfoBinding420
{
    private readonly ModuleRegistry registry;
    private readonly (string Function, int Arity) signature;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal EtsInfoBinding420(ProcessRuntime runtime, ModuleRegistry registry, (string Function, int Arity) signature)
    {
        Runtime = runtime;
        this.registry = registry;
        this.signature = signature;
        Body = Invoke;
    }

    private async ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, Runtime) || !ReferenceEquals(context.Runtime.Modules, registry))
            throw new ArgumentException(EtsInfoNames420.Foreign, nameof(context));
        if (arguments.Count != signature.Arity) throw new ArgumentException(EtsInfoNames420.Signature);
        Runtime.RequireMatchSpecActor419(context);
        context.Cancellation.ThrowIfCancellationRequested();
        try
        {
            var core = Runtime.EtsFoundation;

            if (arguments.Count == 2 && arguments[1] is Atom { Name: EtsInfoNames420.Binary })
                return await registry.Call(
                    context,
                    EtsInfoNames420.Internal,
                    EtsInfoNames420.BinaryHelper,
                    arguments[0]
                );
            Term result = core.CoreInfo420(
                context,
                arguments[0],
                arguments.Count == 1 ? null : arguments[1],
                out byte[]? printed
            );
            if (printed is not null)
            {
                try
                {
                    Runtime.StandardStreams.Write(ErlangStandardStreamNames.StandardOutput, BitString.FromOwnedBytes(printed));
                }
                catch (Exception error) when (error is IOException or ObjectDisposedException or NotSupportedException or UnauthorizedAccessException or System.ComponentModel.Win32Exception) { }
            }

            return result;

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
                EtsInfoNames420.Module,
                signature.Function,
                arguments,
                new MapTerm([new KeyValuePair<Term, Term>(Term.A(RuntimeExceptionNames.Module), Term.A(RuntimeEtsFacetNames407.Formatter))])
            );
        }
    }

    private ErlangException Failure(ProcessContext context, IReadOnlyList<Term> arguments, string? cause)
    {
        var entries = new List<KeyValuePair<Term, Term>> { new(Term.A(RuntimeExceptionNames.Module), Term.A(RuntimeEtsFacetNames407.Formatter)) };
        if (cause is not null) entries.Add(new(Term.A(RuntimeEtsFacetNames407.Cause), Term.A(cause)));

        return context.CaptureNativeFailureWithInfo(
            new ErlangException(ErlangErrorReasons.BadArgument),
            EtsInfoNames420.Module,
            signature.Function,
            arguments,
            new MapTerm(entries)
        );
    }
}
