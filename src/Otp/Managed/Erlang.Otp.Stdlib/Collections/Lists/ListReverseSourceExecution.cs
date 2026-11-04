namespace Erlang;

internal static class ListReverseSourceExecution
{
    internal static async ValueTask<Term> Run(ProcessContext context, Term input)
    {
        using var tail = context.EnterSourceTailCall(ListModuleNames.Module, ListModuleNames.Reverse);
        if (input is Nil || input is Cons { Tail: Nil })
        {
            return input;
        }
        if (input is not Cons { Tail: Cons second } first)
        {
            throw RuntimeExceptionCapture.Call(
                context,
                new ErlangException(ErlangErrorReasons.FunctionClause),
                context.CaptureExecutionChain(),
                ListModuleNames.Module,
                ListModuleNames.Reverse,
                [input],
                false,
                ListReverseSource.Location(ListReverseSource.Head)
            );
        }
        if (second.Tail is Nil)
        {
            return Term.List(second.Head, first.Head);
        }
        using var call = context.EnterGeneratedFunction(
            ListModuleNames.Module,
            ListModuleNames.Reverse,
            1,
            ListReverseSource.Location(ListReverseSource.NativeCall),
            ListReverseSource.Continuation
        );

        return await context.Runtime.Modules.Call(
            context,
            ListModuleNames.Module,
            ListModuleNames.Reverse,
            second.Tail,
            Term.List(second.Head, first.Head)
        );
    }
}
