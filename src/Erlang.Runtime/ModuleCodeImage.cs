namespace Erlang;

public sealed class ModuleCodeImage
{
    internal ModuleCodeImage(string module) => Module = module;

    public string Module
    {
        get;
    }
    internal NifImageBindings NifBindings => NifImageBindingStore.Get(this);
}
