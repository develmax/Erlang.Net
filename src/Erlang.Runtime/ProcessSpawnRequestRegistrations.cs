namespace Erlang;

internal static class ProcessSpawnRequestRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        for (int arity = 1; arity <= 5; arity++)
            registry.Register(
                ErlangModuleNames.Module,
                ProcessSpawnRequestNames.Function,
                arity,
                Start
            );
        registry.Register(
            ErlangModuleNames.Module,
            ProcessSpawnRequestNames.AbandonFunction,
            1,
            (context, arguments) =>
        {
            if (arguments[0] is not ReferenceTerm reference)
                throw Invalid(context, arguments, ProcessSpawnRequestNames.AbandonFunction);

            return ValueTask.FromResult<Term>(Term.A(context.Runtime.AbandonSpawnRequest(context, reference) ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False));
        }
        );
    }

    private static ValueTask<Term> Start(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        string node = context.Runtime.Node;
        Term options = Nil.Value;
        Func<ProcessContext, ValueTask<Term>> body;
        SequenceSpawnDescription description;
        int callback = arguments.Count switch
        {
            1 => 0,
            2 when arguments[0] is FunctionTerm { Arity: 0 } => 0,
            2 or 3 when arguments[1] is FunctionTerm { Arity: 0 } => 1,
            _ => -1
        };
        if (callback >= 0)
        {
            if (arguments[callback] is not FunctionTerm { Arity: 0 } function)
                throw Invalid(context, arguments);
            if (callback == 1)
                node = arguments[0] is Atom address ? address.Name : throw Invalid(context, arguments);
            if (arguments.Count == callback + 2)
                options = arguments[^1];
            body = child => function.Invoke(child, Array.Empty<Term>());
            description = SequenceSpawnDescription.Callback(function, options);
        }
        else
        {
            bool remote = arguments.Count == 5 || arguments.Count == 4 && arguments[2] is Atom;
            int offset = remote ? 1 : 0;
            if (arguments.Count < 3 || arguments[offset] is not Atom module || arguments[offset + 1] is not Atom function)
                throw Invalid(context, arguments);
            if (remote)
                node = arguments[0] is Atom address ? address.Name : throw Invalid(context, arguments);
            Term[] values;
            try
            {
                values = Cons.Items(arguments[offset + 2]).ToArray();
            }
            catch (ErlangException)
            {
                throw Invalid(context, arguments);
            }
            if (arguments.Count == offset + 4)
                options = arguments[^1];
            body = child => child.Runtime.Modules.Call(
                child,
                module.Name,
                function.Name,
                values
            );
            description = SequenceSpawnDescription.Module(
                module,
                function,
                arguments[offset + 2],
                options
            );
        }
        var optionValues = new List<Term>();
        Term optionTail = options;
        while (optionTail is Cons cell)
        {
            optionValues.Add(cell.Head);
            optionTail = cell.Tail;
        }
        bool disconnected = node != context.Runtime.Node;
        ProcessSpawnRequestOptions parsed = ProcessSpawnRequestOptions.Parse(optionValues, disconnected);
        if (optionTail is not Nil && (!disconnected || !parsed.BadOptions))
            throw Invalid(context, arguments, badOption: node == context.Runtime.Node);
        Term? failure = parsed.BadOptions ? Term.A(ProcessSpawnOptionNames.BadOption) : node != context.Runtime.Node ? Term.A(NodeSpawnNames.Disconnected) : null;

        return ValueTask.FromResult<Term>(context.Runtime.SpawnRequest(
            context,
            body,
            parsed,
            failure,
            description with { ReplyTag = parsed.Tag }
        ));
    }

    private static ErlangException Invalid(
        ProcessContext context,
        IReadOnlyList<Term> arguments,
        string function = ProcessSpawnRequestNames.Function,
        bool badOption = false
    )
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
            StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), new MapTerm(fields), function)
        };
    }
}
