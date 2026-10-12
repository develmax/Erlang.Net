namespace Erlang;

public static class ErlangSourceWrapperSignatures
{
    public static bool Contains(string function, int arity) => arity == 1 && function is ModuleCodeLifecycleNames.PurgeModule or ErlangStandardStreamNames.DisplayString;
}
