namespace Erlang;

internal static class ProcessSpawnOptionRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (int arity in new[] { 2, 3, 4, 5 })
            registry.Register(
                ErlangModuleNames.Module,
                ProcessSpawnOptionNames.Function,
                arity,
                Start
            );
    }

    private static ValueTask<Term> Start(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        bool remote = arguments.Count is 3 or 5;
        string node = context.Runtime.Node;
        if (remote)
            node = arguments[0] is Atom address ? address.Name : throw Invalid(context, arguments);
        Func<ProcessContext, ValueTask<Term>> body;
        if (arguments.Count is 2 or 3)
        {
            Term callback = arguments[remote ? 1 : 0];
            bool valid = !remote || node == context.Runtime.Node
                ? callback is FunctionTerm or TupleTerm { Items: [Atom, Atom] }
                : callback is FunctionTerm { Arity: 0 } or TupleTerm { Items: [Atom, Atom] };
            if (!valid)
                throw Invalid(context, arguments);
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
            int offset = remote ? 1 : 0;
            if (arguments[offset] is not Atom module || arguments[offset + 1] is not Atom function)
                throw Invalid(context, arguments);
            Term[] values;
            try
            {
                values = Cons.Items(arguments[offset + 2]).ToArray();
            }
            catch (ErlangException)
            {
                throw Invalid(context, arguments);
            }
            body = child => child.Runtime.Modules.Call(
                child,
                module.Name,
                function.Name,
                values
            );
        }
        ProcessSpawnOptions options;
        try
        {
            options = ProcessSpawnOptions.Parse(arguments[^1]);
        }
        catch (ErlangException)
        {
            throw Invalid(context, arguments, true);
        }
        if (node != context.Runtime.Node)
            body = child =>
            {
                child.Exit(Term.A(NodeSpawnNames.Disconnected));

                return ValueTask.FromResult<Term>(Nil.Value);
            };

        return ValueTask.FromResult(context.Runtime.SpawnWithOptions(context, body, options));
    }

    private static ErlangException Invalid(ProcessContext context, IReadOnlyList<Term> arguments, bool badOption = false)
    {
        var fields = new List<KeyValuePair<Term, Term>>
        {
            new(Term.A(ProcessSpawnOptionNames.ErrorModule), Term.A(ProcessSpawnOptionNames.ErrorFormatter))
        };
        if (badOption)
            fields.Add(new(Term.A(ProcessSpawnOptionNames.Cause), Term.A(ProcessSpawnOptionNames.BadOption)));

        return new ErlangException(ErlangErrorReasons.BadArgument)
        {
            ExplicitStackTrace = true,
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), new MapTerm(fields), ProcessSpawnOptionNames.Function)
        };
    }
}
