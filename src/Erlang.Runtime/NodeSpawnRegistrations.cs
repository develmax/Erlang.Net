namespace Erlang;

internal static class NodeSpawnRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (string function in new[] { ErlangModuleNames.Spawn, ErlangModuleNames.SpawnLink, ErlangModuleNames.SpawnMonitor })
            foreach (int arity in new[] { 2, 4 })
                registry.Register(
                    ErlangModuleNames.Module,
                    function,
                    arity,
                    (context, arguments) => Start(context, function, arguments)
                );
    }

    private static ValueTask<Term> Start(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        Func<ProcessContext, ValueTask<Term>> body;
        if (arguments[0] is not Atom node)
            throw Invalid(context, function, arguments);
        if (arguments.Count == 2)
        {
            Term callback = arguments[1];
            bool valid = function == ErlangModuleNames.Spawn ? callback is FunctionTerm or TupleTerm { Items: [Atom, Atom] } : callback is FunctionTerm { Arity: 0 };
            if (!valid)
                throw Invalid(context, function, arguments);
            body = child => child.Runtime.Modules.Call(
                child,
                ErlangModuleNames.Module,
                ErlangBasicBifNames.Apply,
                callback,
                Nil.Value
            );
        }
        else
        {
            if (arguments[1] is not Atom module || arguments[2] is not Atom name)
                throw Invalid(context, function, arguments);
            Term[] values;
            try
            {
                values = Cons.Items(arguments[3]).ToArray();
            }
            catch (ErlangException)
            {
                throw Invalid(context, function, arguments);
            }
            body = child => child.Runtime.Modules.Call(
                child,
                module.Name,
                name.Name,
                values
            );
        }
        if (node.Name != context.Runtime.Node)
            body = child =>
            {
                child.Exit(Term.A(NodeSpawnNames.Disconnected));

                return ValueTask.FromResult<Term>(Nil.Value);
            };
        try
        {
            if (function == ErlangModuleNames.SpawnMonitor)
            {
                var monitored = context.Runtime.SpawnMonitor(context, body);

                return ValueTask.FromResult<Term>(Term.Tuple(monitored.Process.Pid, monitored.Reference));
            }

            return ValueTask.FromResult<Term>(context.Runtime.Spawn(body, context, function == ErlangModuleNames.SpawnLink).Pid);
        }
        catch (ErlangException exception)
        {
            throw Invalid(
                context,
                function,
                arguments,
                exception.Reason
            );
        }
    }

    private static ErlangException Invalid(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        Term? reason = null
    )
    {
        var info = new MapTerm([new(Term.A(NodeSpawnNames.ErrorModule), Term.A(NodeSpawnNames.ErrorFormatter))]);

        return new ErlangException(reason ?? Term.A(ErlangErrorReasons.BadArgument))
        {
            ExplicitStackTrace = true,
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), info, function)
        };
    }
}
