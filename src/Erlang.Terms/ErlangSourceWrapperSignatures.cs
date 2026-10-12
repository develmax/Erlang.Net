namespace Erlang;

public static class ErlangSourceWrapperSignatures
{
    public static bool Contains(string function, int arity) => function == ProcessSuspensionNames.Suspend && arity is 1 or 2 || arity == 1 && function is ModuleCodeLifecycleNames.PurgeModule or ErlangStandardStreamNames.DisplayString;
}
