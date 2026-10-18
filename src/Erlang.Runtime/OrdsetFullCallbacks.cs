namespace Erlang;

internal static class OrdsetFullCallbacks
{
    internal static async ValueTask<Term> Fold(
        ProcessContext context,
        Term function,
        Term accumulator,
        Term set
    )
    {
        using var tail = context.EnterSourceTailCall(OrdsetCoreNames.Module, OrdsetFullNames.Fold);

        return await context.Runtime.Modules.Call(
            context,
            ListModuleNames.Module,
            ListModuleNames.FoldLeft,
            [function, accumulator, set]
        );
    }

    internal static async ValueTask<Term> Transform(
        ProcessContext context,
        string operation,
        Term function,
        Term set
    )
    {
        using var tail = context.EnterSourceTailCall(OrdsetCoreNames.Module, operation);
        if (operation == OrdsetFullNames.Filter)
            return await context.Runtime.Modules.Call(
                context,
                ListModuleNames.Module,
                ListModuleNames.Filter,
                [function, set]
            );
        string listOperation = operation == OrdsetFullNames.Map ? ListModuleNames.Map : ListModuleNames.FilterMap;
        Term transformed;
        using (OrdsetFullTrace.Enter(
            context,
            new(
                OrdsetCoreNames.Module,
                operation,
                2,
                operation == OrdsetFullNames.Map ? OrdsetFullNames.MapCall : OrdsetFullNames.FilterMapCall
            )
        ))
            transformed = await context.Runtime.Modules.Call(
                context,
                ListModuleNames.Module,
                listOperation,
                [function, set]
            );

        return await OrdsetFullExecution.Delegate(context, OrdsetFullNames.FromList, transformed);
    }
}
