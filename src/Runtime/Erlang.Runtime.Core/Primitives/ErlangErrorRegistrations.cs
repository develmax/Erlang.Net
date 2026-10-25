namespace Erlang;

internal static class ErlangErrorRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ErlangModuleNames.Error,
            2,
            (context, arguments) => throw Failure(
                context,
                arguments[0],
                arguments[1],
                null
            )
        );
        registry.Register(
            ErlangModuleNames.Module,
            ErlangModuleNames.Error,
            3,
            (context, arguments) => throw Failure(
                context,
                arguments[0],
                arguments[1],
                ErrorInfo(arguments[2])
            )
        );
        foreach (int arity in new[] { 1, 2 })
            registry.Register(
                ErlangModuleNames.Module,
                ErlangErrorMetadataNames.NifError,
                arity,
                (context, arguments) => throw new ErlangException(arguments[0])
                {
                    ExplicitStackTrace = true,
                    StackTraceTerm = context.CaptureErrorStack(Cons.From(arguments), null, ErlangErrorMetadataNames.NifError)
                }
            );
    }

    private static ErlangException Failure(
        ProcessContext context,
        Term reason,
        Term arguments,
        MapTerm? info
    ) => new(reason)
    {
        ExplicitStackTrace = true,
        StackTraceTerm = context.CaptureErrorStack(arguments, info)
    };

    private static MapTerm? ErrorInfo(Term options)
    {
        MapTerm? result = null;
        while (options is Cons item)
        {
            if (item.Head is not TupleTerm { Items.Count: 2 } option || option.Items[0] is not Atom { Name: ErlangErrorMetadataNames.ErrorInfo } || option.Items[1] is not MapTerm map)
                return null;
            result = map;
            options = item.Tail;
        }

        return options is Nil ? result : null;
    }
}
