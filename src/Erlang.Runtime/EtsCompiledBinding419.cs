namespace Erlang;

internal sealed class EtsCompiledBinding419
{
    private readonly ModuleRegistry registry;
    private readonly (string Function, int Arity) signature;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal EtsCompiledBinding419(ProcessRuntime runtime, ModuleRegistry registry, (string Function, int Arity) signature)
    {
        Runtime = runtime;
        this.registry = registry;
        this.signature = signature;
        Body = Invoke;
    }

    private async ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, Runtime) || !ReferenceEquals(context.Runtime.Modules, registry))
            throw new ArgumentException(EtsCompiledNames419.Foreign, nameof(context));
        if (arguments.Count != signature.Arity) throw new ArgumentException(EtsCompiledNames419.Signature);
        Runtime.RequireMatchSpecActor419(context);
        try
        {
            return signature.Function switch
            {
                EtsCompiledNames419.Compile => EtsCompiledOperations419.Compile(context, arguments[0]),
                EtsCompiledNames419.IsCompiled => EtsCompiledOperations419.IsCompiled(context, arguments[0]),
                EtsCompiledNames419.Run => await EtsCompiledOperations419.Run(
                    context,
                    arguments[0],
                    arguments[1],
                    arguments[2]
                ),
                _ => throw new InvalidOperationException(EtsCompiledNames419.Signature)
            };
        }
        catch (EtsCompiledRunFailure419 failure)
        {
            throw Failure(context, new ErlangException(ErlangErrorReasons.BadArgument), failure.ActualArguments);
        }
        catch (ErlangException error)
        {
            throw Failure(context, error, arguments);
        }
    }

    private ErlangException Failure(ProcessContext context, ErlangException error, IReadOnlyList<Term> arguments)
        => context.CaptureNativeFailureWithInfo(
            error,
            EtsCompiledNames419.Module,
            signature.Function,
            arguments,
            new MapTerm([new KeyValuePair<Term, Term>(Term.A(RuntimeExceptionNames.Module), Term.A(RuntimeEtsFacetNames407.Formatter))])
        );
}
