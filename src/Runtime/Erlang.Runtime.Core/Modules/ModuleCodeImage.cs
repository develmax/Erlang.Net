namespace Erlang;

public sealed partial class ModuleCodeImage
{
    internal ModuleCodeImage(string module) => Module = module;

    internal ModuleCodeImage(string module, object executionOwner) : this(module)
    {
        ArgumentNullException.ThrowIfNull(executionOwner);
        ExecutionOwner = executionOwner;
    }

    internal object? ExecutionOwner
    {
        get;
    }

    public string Module
    {
        get;
    }
    internal NifImageBindings NifBindings => NifImageBindingStore.Get(this);
}
