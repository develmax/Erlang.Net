namespace Erlang.Otp;

internal static class ProcLibNodeSpawn
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (string function in new[] { ProcLibNames.Spawn, ProcLibNames.SpawnLink })
            foreach (int arity in new[] { 2, 4 })
                registry.Register(
                    ProcLibNames.Module,
                    function,
                    arity,
                    (context, arguments) => Spawn(
                        context,
                        function,
                        arguments,
                        false
                    )
                );
        foreach (int arity in new[] { 3, 5 })
            registry.Register(
                ProcLibNames.Module,
                ProcLibOptionNames.SpawnOpt,
                arity,
                (context, arguments) => Spawn(
                    context,
                    ProcLibOptionNames.SpawnOpt,
                    arguments,
                    true
                )
            );
    }

    private static async ValueTask<Term> Spawn(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        bool options
    )
    {
        int localCount = arguments.Count - 1 - (options ? 1 : 0);
        var initialization = await ProcLib.PrepareSpawn(context, arguments.Skip(1).Take(localCount).ToArray(), false);
        Term[] builtinArguments = options
            ? [arguments[0], ProcLibAtoms.Module, ProcLibAtoms.Initializer, initialization.Description.Arguments, arguments[^1]]
            : [arguments[0], ProcLibAtoms.Module, ProcLibAtoms.Initializer, initialization.Description.Arguments];

        return await context.Runtime.Modules.Call(
            context,
            ProcLibNames.Erlang,
            function,
            builtinArguments
        );
    }
}
