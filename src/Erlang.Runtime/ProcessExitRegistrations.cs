namespace Erlang;

internal static class ProcessExitRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ErlangModuleNames.Link,
            2,
            (context, arguments) =>
        {
            bool priority = Priority(arguments[1], ProcessLinkOptionNames.Priority);
            if (arguments[0] is PortTerm port)
                context.Runtime.Ports.Link(context, port, priority);
            else
                context.Runtime.Link(context, CoreModules.PidValue(arguments[0]), priority);

            return ValueTask.FromResult<Term>(Term.A(ErlangBooleanAtoms.True));
        }
        );
        foreach (string name in new[] { ErlangModuleNames.Exit, ProcessExitFunctionNames.ExitSignal })
            foreach (int arity in name == ErlangModuleNames.Exit ? new[] { 3 } : new[] { 2, 3 })
                registry.Register(
                    ErlangModuleNames.Module,
                    name,
                    arity,
                    (context, arguments) =>
                {
                    bool priority = arity == 3 && Priority(arguments[2], ProcessExitOptionNames.Priority);
                    context.Runtime.SendExitSignal(
                        context,
                        arguments[0],
                        arguments[1],
                        name == ErlangModuleNames.Exit,
                        priority
                    );

                    return ValueTask.FromResult<Term>(Term.A(ErlangBooleanAtoms.True));
                }
                );
    }

    private static bool Priority(Term options, string optionName)
    {
        bool priority = false;
        foreach (Term option in Cons.Items(options))
        {
            if (option is not Atom atom || atom.Name != optionName)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            priority = true;
        }

        return priority;
    }
}
