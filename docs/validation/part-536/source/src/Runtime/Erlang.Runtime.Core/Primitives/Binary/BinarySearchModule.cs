namespace Erlang;

internal static class BinarySearchModule
{
    internal static void Register(ModuleRegistry registry)
    {
        registry.RegisterCanonicalRuntimeRead535(
            BinaryModuleNames.Module,
            BinaryModuleNames.CompilePattern,
            1,
            (context, arguments) => BinarySearchPatterns.Compile(context, arguments[0])
        );
        foreach (int arity in new[] { 2, 3 })
        {
            registry.RegisterCanonicalRuntimeRead535(
                BinaryModuleNames.Module,
                BinaryModuleNames.Match,
                arity,
                (context, arguments) => BinarySearchExecution.Run(context, arguments, BinarySearchOperation.Match)
            );
            registry.RegisterCanonicalRuntimeRead535(
                BinaryModuleNames.Module,
                BinaryModuleNames.Matches,
                arity,
                (context, arguments) => BinarySearchExecution.Run(context, arguments, BinarySearchOperation.Matches)
            );
            registry.RegisterCanonicalRuntimeRead535(
                BinaryModuleNames.Module,
                BinaryModuleNames.Split,
                arity,
                (context, arguments) => BinarySearchExecution.Run(context, arguments, BinarySearchOperation.Split)
            );
        }
    }
}
