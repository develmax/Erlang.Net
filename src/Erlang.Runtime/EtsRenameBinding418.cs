namespace Erlang;

internal sealed class EtsRenameBinding418
{
    private readonly ModuleRegistry registry;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal EtsRenameBinding418(ProcessRuntime runtime, ModuleRegistry registry)
    {
        Runtime = runtime;
        this.registry = registry;
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, Runtime) || !ReferenceEquals(context.Runtime.Modules, registry))
            throw new ArgumentException(EtsRenameNames418.Foreign, nameof(context));
        if (arguments.Count != EtsRenameNames418.Arity) throw new ArgumentException(EtsRenameNames418.Signature);
        context.Cancellation.ThrowIfCancellationRequested();
        try
        {
            return ValueTask.FromResult(Runtime.EtsFoundation.CoreRename418(context, arguments[0], arguments[1]));
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
                EtsRenameNames418.Module,
                EtsRenameNames418.Rename,
                arguments,
                new MapTerm(entries)
            );
        }
    }
}
