namespace Erlang;

internal sealed class RuntimeBootstrapSchedulerBinding
{
    private readonly ModuleRegistry registry;
    internal ProcessRuntime Runtime { get; }
    internal ErlangFunction Body { get; }

    internal RuntimeBootstrapSchedulerBinding(ProcessRuntime runtime, ModuleRegistry registry)
    {
        Runtime = runtime;
        this.registry = registry;
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, Runtime) || !ReferenceEquals(context.Runtime.Modules, registry))
            throw new ArgumentException(RuntimeBootstrapSchedulerNames.Foreign, nameof(context));

        return ValueTask.FromResult<Term>(CoreModules.Boolean(Runtime.ExchangeSchedulerWallTime(context,
            arguments[0] is Atom { Name: ErlangBooleanAtoms.True })));
    }
}
