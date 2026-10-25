namespace Erlang;

internal sealed class AtomicsConstructorBinding413
{
    private readonly ModuleRegistry registry;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal AtomicsConstructorBinding413(ProcessRuntime runtime, ModuleRegistry registry)
    {
        Runtime = runtime;
        this.registry = registry;
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, Runtime) || !ReferenceEquals(context.Runtime.Modules, registry))
            throw new ArgumentException(AtomicsNames413.Foreign, nameof(context));
        Runtime.RequireAtomicsActor413(context);
        using var invocation = Runtime.AtomicsOwner413.Enter();
        ulong length = AtomicsValues413.Word(arguments[0]);
        if (length == 0) throw new ErlangException(ErlangErrorReasons.BadArgument);
        ulong options = AtomicsValues413.Word(arguments[1]);
        if (length > ulong.MaxValue / sizeof(long)) throw new ErlangException(ErlangErrorReasons.SystemLimit);
        AtomicsArray413 array = Runtime.AtomicsOwner413.Create(length, (options & AtomicsNames413.SignedOption) != 0);
        try
        {
            ResourceReferenceTerm reference = Runtime.ReferenceResources.CreateAtomics413(new(Runtime.AtomicsOwner413, array));

            return ValueTask.FromResult<Term>(reference);
        }
        catch (Exception original)
        {
            try
            {
                Runtime.AtomicsOwner413.Retire(array);
            }
            catch (Exception cleanup)
            {
                throw new AggregateException(original, cleanup);
            }
            throw;
        }
    }
}
