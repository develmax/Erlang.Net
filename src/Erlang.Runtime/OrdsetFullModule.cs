namespace Erlang;

internal static class OrdsetFullModule
{
    internal static void Register(ModuleRegistry registry)
    {
        foreach (string function in new[] { OrdsetFullNames.DeleteElement, OrdsetFullNames.Union, OrdsetFullNames.Intersection, OrdsetFullNames.Disjoint, OrdsetFullNames.Subtract, OrdsetFullNames.Subset })
            registry.Register(
                OrdsetCoreNames.Module,
                function,
                2,
                (context, arguments) => OrdsetFullExecution.Binary(
                    context,
                    function,
                    arguments[0],
                    arguments[1]
                )
            );
        registry.Register(
            OrdsetCoreNames.Module,
            OrdsetFullNames.Intersection,
            1,
            (context, arguments) => OrdsetFullExecution.Intersection(context, arguments[0])
        );
        foreach (string function in new[] { OrdsetFullNames.FromList, OrdsetFullNames.Union })
            registry.Register(
                OrdsetCoreNames.Module,
                function,
                1,
                (context, arguments) => OrdsetFullExecution.Delegate(context, function, arguments[0])
            );
        registry.Register(
            OrdsetCoreNames.Module,
            OrdsetFullNames.Fold,
            3,
            (context, arguments) => OrdsetFullCallbacks.Fold(
                context,
                arguments[0],
                arguments[1],
                arguments[2]
            )
        );
        foreach (string function in new[] { OrdsetFullNames.Filter, OrdsetFullNames.Map, OrdsetFullNames.FilterMap })
            registry.Register(
                OrdsetCoreNames.Module,
                function,
                2,
                (context, arguments) => OrdsetFullCallbacks.Transform(
                    context,
                    function,
                    arguments[0],
                    arguments[1]
                )
            );
    }
}
