namespace Erlang;

internal sealed class InternalSystemProcessPrimitiveBinding
{
    private readonly ProcessRuntime owner;
    private readonly ModuleRegistry registry;
    private readonly int arity;
    private readonly ErlangFunction kernel;
    internal ErlangFunction Body { get; }

    internal InternalSystemProcessPrimitiveBinding(
        ProcessRuntime owner,
        ModuleRegistry registry,
        string function,
        int arity
    )
    {
        this.owner = owner;
        this.registry = registry;
        this.arity = arity;
        if (!ReferenceEquals(owner.Modules, registry))
            throw new ArgumentException(RuntimeBootstrapFileNames.ForeignRegistry, nameof(registry));
        kernel = (function, arity) switch
        {
            (RuntimeBootstrapFileNames.IsSystemProcess, 1) => RuntimeBootstrapFileSystem.IsSystemBody,
            (RuntimeBootstrapFileNames.SpawnSystemProcess, 3) => RuntimeBootstrapFileSystem.SpawnBody,
            _ => throw new ArgumentException(RuntimeBootstrapFileNames.ForeignRegistry, nameof(function))
        };
        Body = Invoke;
    }

    private ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (!ReferenceEquals(context.Runtime, owner) || !ReferenceEquals(context.Runtime.Modules, registry)
            || arguments.Count != arity)
            throw new ArgumentException(RuntimeBootstrapFileNames.ForeignRegistry, nameof(context));
        // Original kernel operations run only with this actual VM's admitted
        // actor; static bootstrap delegates are never accepted as global authority.
        owner.RequireInternalActor419(context);

        return kernel(context, arguments);
    }
}
