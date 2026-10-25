namespace Erlang;

internal static class ErlangApplyDispatch
{
    internal static ValueTask<Term> Invoke(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        var (module, function, values) = Prepare(context, arguments);
        if (BeamModuleApplyTailScope.TryTransfer(
            context,
            module,
            function,
            values,
            out var pending
        ))
            return pending;

        return InvokeTarget(
            context,
            module,
            function,
            values
        );
    }

    internal static (string Module, string Function, Term[] Arguments) Prepare(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term module = arguments[ErlangApplyLayout.ModuleArgument];
        Term function = arguments[ErlangApplyLayout.FunctionArgument];
        Term argumentList = arguments[ErlangApplyLayout.ArgumentList];
        try
        {
            while (true)
            {
                context.Cancellation.ThrowIfCancellationRequested();
                if (function is not Atom || module is not Atom)
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                if (module is not Atom { Name: ErlangModuleNames.Module }
                    || function is not Atom { Name: ErlangBasicBifNames.Apply }
                    || !context.Runtime.Modules.IsCanonicalModuleApply()
                    || argumentList is not Cons { Head: var nestedModule, Tail: Cons { Head: var nestedFunction, Tail: Cons { Head: var nestedArguments, Tail: Nil } } })
                    break;
                module = nestedModule;
                function = nestedFunction;
                argumentList = nestedArguments;
            }
            Term[] values = Arguments(argumentList);

            return (((Atom)module).Name, ((Atom)function).Name, values);
        }
        catch (ErlangException error)
        {
            throw RuntimeExceptionCapture.Call(
                context,
                error,
                context.CaptureExecutionChain(),
                ErlangModuleNames.Module,
                ErlangBasicBifNames.Apply,
                [module, function, argumentList],
                true
            );
        }
    }

    private static Term[] Arguments(Term argumentList)
    {
        var values = new List<Term>();
        Term remaining = argumentList;
        while (remaining is Cons cell)
        {
            if (values.Count == ErlangApplyLimits.MaximumArgumentCount)
                throw new ErlangException(ErlangErrorReasons.SystemLimit);
            values.Add(cell.Head);
            remaining = cell.Tail;
        }
        if (remaining is not Nil)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return values.ToArray();
    }

    internal static async ValueTask<Term> InvokeTarget(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments
    )
    {
        ModuleCallDispatchKind dispatch = ModuleCallDispatchKind.Direct;
        try
        {
            return await context.Runtime.Modules.CallWithDispatch(
                context,
                module,
                function,
                arguments,
                out dispatch
            );
        }
        catch (ErlangException exception) when (dispatch == ModuleCallDispatchKind.Direct && !exception.ExplicitStackTrace && exception.StackTraceTerm is Nil)
        {
            throw RuntimeExceptionCapture.Call(
                context,
                exception,
                context.CaptureExecutionChain(),
                module,
                function,
                arguments,
                false
            );
        }
    }
}
