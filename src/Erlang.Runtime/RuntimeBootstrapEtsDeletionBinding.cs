namespace Erlang;

internal sealed class RuntimeBootstrapEtsDeletionBinding
{
    private readonly ModuleRegistry registry;
    private readonly string function;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal RuntimeBootstrapEtsDeletionBinding(ProcessRuntime runtime, ModuleRegistry registry, string function)
    {
        Runtime = runtime;
        this.registry = registry;
        this.function = function;
        Body = Invoke;
    }

    private async ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, Runtime) || !ReferenceEquals(context.Runtime.Modules, registry))
            throw new ArgumentException(RuntimeEtsSelectDeleteNames412.Foreign, nameof(context));
        if (arguments.Count != 2) throw new ArgumentException(RuntimeEtsSelectDeleteNames412.Signature);
        context.Cancellation.ThrowIfCancellationRequested();
        try
        {
            return function == RuntimeEtsSelectDeleteNames412.Function
                ? await Runtime.EtsFoundation.CoreSelectDelete412(context, arguments[0], arguments[1]).ConfigureAwait(false)
                : await Runtime.EtsFoundation.CoreDeleteAll412(context, arguments[0], arguments[1]).ConfigureAwait(false);
        }
        catch (RuntimeEtsCoreFailure407 failure)
        {
            var entries = new List<KeyValuePair<Term, Term>> {
                new(Term.A(RuntimeExceptionNames.Module), Term.A(RuntimeEtsFacetNames407.Formatter)) };
            string? cause = failure.Cause switch
            {
                RuntimeEtsCoreCause407.Type => RuntimeEtsFacetNames407.Type,
                RuntimeEtsCoreCause407.Id => RuntimeEtsFacetNames407.Id,
                RuntimeEtsCoreCause407.Access => RuntimeEtsFacetNames407.Access,
                _ => null
            };
            if (cause is not null) entries.Add(new(Term.A(RuntimeEtsFacetNames407.Cause), Term.A(cause)));
            throw context.CaptureNativeFailureWithInfo(
                new ErlangException(ErlangErrorReasons.BadArgument),
                RuntimeEtsSelectDeleteNames412.Module,
                function,
                arguments,
                new MapTerm(entries)
            );
        }
    }
}
