namespace Erlang;

internal static class ListFoldRight
{
    internal static async ValueTask<Term> Run(
        ProcessContext context,
        Term function,
        Term accumulator,
        Term list
    )
    {
        using var tail = context.EnterSourceTailCall(ListModuleNames.Module, ListModuleNames.FoldRight);
        if (function is not FunctionTerm { Arity: ListFoldRightSource.CallbackArity } callback)
        {
            throw RuntimeExceptionCapture.Call(
                context,
                new ErlangException(ErlangErrorReasons.FunctionClause),
                context.CaptureExecutionChain(),
                ListModuleNames.Module,
                ListModuleNames.FoldRight,
                [function, accumulator, list],
                false,
                ListFoldRightSource.PublicLocation
            );
        }

        var pending = new Stack<Term>();
        Term reached = list;
        while (reached is Cons cell)
        {
            await context.ReduceAsync();
            pending.Push(cell.Head);
            reached = cell.Tail;
        }

        if (reached is not Nil)
            throw Failure(
                context,
                function,
                accumulator,
                reached,
                pending.Count != 0
            );
        while (pending.TryPop(out Term? item))
        {
            await context.ReduceAsync();
            using var recursive = pending.Count == 0 ? null : context.EnterGeneratedFunction(
                ListModuleNames.Module,
                ListFoldRightSource.Helper,
                ListFoldRightSource.HelperArity,
                ListFoldRightSource.ReturnLocation,
                ListFoldRightSource.RecursiveContinuation
            );
            try
            {
                accumulator = await callback.Invoke(context, [item, accumulator]);
            }
            catch (ErlangException exception)
            {
                throw context.CaptureSourceFailure(exception);
            }
        }

        return accumulator;
    }

    internal static ErlangException Failure(
        ProcessContext context,
        Term function,
        Term accumulator,
        Term reachedTail,
        bool hasPendingContinuation
    )
    {
        using var tail = context.EnterSourceTailCall(ListModuleNames.Module, ListModuleNames.FoldRight);
        using var recursive = !hasPendingContinuation ? null : context.EnterGeneratedFunction(
            ListModuleNames.Module,
            ListFoldRightSource.Helper,
            ListFoldRightSource.HelperArity,
            ListFoldRightSource.ReturnLocation,
            ListFoldRightSource.RecursiveContinuation
        );

        return RuntimeExceptionCapture.Call(
            context,
            new ErlangException(ErlangErrorReasons.FunctionClause),
            context.CaptureExecutionChain(),
            ListModuleNames.Module,
            ListFoldRightSource.Helper,
            [function, accumulator, reachedTail],
            false,
            ListFoldRightSource.HelperLocation
        );
    }
}
