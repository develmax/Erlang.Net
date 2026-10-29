namespace Erlang;

internal static class NodeSpawnRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (string function in new[] { ErlangModuleNames.Spawn, ErlangModuleNames.SpawnLink, ErlangModuleNames.SpawnMonitor })
            foreach (int arity in new[] { 2, 4 })
                registry.RegisterCanonicalRuntimeRead535(
                    ErlangModuleNames.Module,
                    function,
                    arity,
                    (context, arguments) => Start(context, function, arguments)
                );
    }

    private static async ValueTask<Term> Start(ProcessContext context, string function, IReadOnlyList<Term> arguments)
    {
        Func<ProcessContext, ValueTask<Term>> body;
        SequenceSpawnDescription description;
        if (arguments[0] is not Atom node)
            throw Invalid(context, function, arguments);
        if (arguments.Count == 2)
        {
            Term callback = arguments[1];
            bool valid = function == ErlangModuleNames.Spawn ? callback is FunctionTerm or TupleTerm { Items: [Atom, Atom] } : callback is FunctionTerm { Arity: 0 };
            if (!valid)
                throw Invalid(context, function, arguments);
            body = RuntimeVmSpawnInput641.Callback641(callback, Array.Empty<Term>());
            description = SequenceSpawnDescription.Callback(callback, SequenceSpawnDescription.DefaultOptions(function));
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
            body = RuntimeVmSpawnInput641.Mfa641(module.Name, name.Name, values);
            description = SequenceSpawnDescription.Module(
                module,
                name,
                arguments[3],
                SequenceSpawnDescription.DefaultOptions(function)
            );
        }
        if (node.Name != context.Runtime.Node)
        {
            var options = ProcessSpawnOptions.Parse(description.Options);
            var remote = await context.Runtime.DistributionPending.TrySpawnConnectedAsync(
                context,
                node.Name,
                description,
                options
            );
            if (remote?.Value is { } value)
                return value;
            Term failure = remote?.Failure ?? Term.A(NodeSpawnNames.Disconnected);
            if (!failure.Equals(Term.A(NodeSpawnNames.Disconnected)) && !failure.Equals(Term.A(DistributionSpawnNames.NotSupported)))
                throw Invalid(
                    context,
                    function,
                    arguments,
                    failure.Equals(Term.A(ProcessSpawnOptionNames.BadOption))
                    ? Term.A(ErlangErrorReasons.BadArgument) : failure
                );
            body = child =>
            {
                child.Exit(failure);

                return ValueTask.FromResult<Term>(Nil.Value);
            };
        }
        try
        {
            if (function == ErlangModuleNames.SpawnMonitor)
            {
                var monitored = context.Runtime.SpawnMonitorTraced(
                    context,
                    body,
                    false,
                    node.Name == context.Runtime.Node ? description : null
                );

                return Term.Tuple(monitored.Process.Pid, monitored.Reference);
            }

            return context.Runtime.SpawnTraced(
                body,
                context,
                function == ErlangModuleNames.SpawnLink,
                node.Name == context.Runtime.Node ? description : null
            ).Pid;
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
