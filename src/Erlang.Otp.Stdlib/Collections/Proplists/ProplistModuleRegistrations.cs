namespace Erlang;

internal static class ProplistModuleRegistrations
{
    internal static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ProplistModuleNames.Module,
            ProplistModuleNames.GetValue,
            2,
            (context, arguments) => ProplistValueLookup.ReadAsync(
                context,
                arguments[0],
                arguments[1],
                ProplistPropertyAtoms.DefaultValue
            )
        );
        registry.Register(
            ProplistModuleNames.Module,
            ProplistModuleNames.GetValue,
            3,
            (context, arguments) => ProplistValueLookup.ReadAsync(
                context,
                arguments[0],
                arguments[1],
                arguments[2]
            )
        );
    }
}
