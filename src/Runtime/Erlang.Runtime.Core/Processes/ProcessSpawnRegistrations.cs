namespace Erlang;

internal static class ProcessSpawnRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.Register(
            ErlangModuleNames.Module,
            ProcessGroupLeaderNames.GroupLeader,
            0,
            (context, _) => ValueTask.FromResult<Term>(context.Runtime.GetGroupLeader(context))
        );
        registry.Register(
            ErlangModuleNames.Module,
            ProcessGroupLeaderNames.GroupLeader,
            2,
            async (context, arguments) => arguments[0] is Pid leader && arguments[1] is Pid target
            ? CoreModules.Boolean(await context.Runtime.SetGroupLeaderAsync(context, leader, target).ConfigureAwait(false))
            : throw new ErlangException(ErlangErrorReasons.BadArgument)
        );
        foreach (string name in new[] { ErlangModuleNames.Spawn, ErlangModuleNames.SpawnLink, ErlangModuleNames.SpawnMonitor })
            registry.Register(
                ErlangModuleNames.Module,
                name,
                3,
                (context, arguments) =>
            {
                string module = CoreModules.AtomName(arguments[0]);
                string function = CoreModules.AtomName(arguments[1]);
                Term[] values = Cons.Items(arguments[2]).ToArray();

                ValueTask<Term> Body(ProcessContext child) => child.Runtime.Modules.Call(
                    child,
                    module,
                    function,
                    values
                );
                if (name == ErlangModuleNames.SpawnMonitor)
                {
                    var monitored = context.Runtime.SpawnMonitorTraced(
                        context,
                        Body,
                        false,
                        SequenceSpawnDescription.Module(
                            (Atom)arguments[0],
                            (Atom)arguments[1],
                            arguments[2],
                            SequenceSpawnDescription.DefaultOptions(name)
                        )
                    );

                    return ValueTask.FromResult<Term>(Term.Tuple(monitored.Process.Pid, monitored.Reference));
                }

                return ValueTask.FromResult<Term>(context.Runtime.SpawnTraced(
                    Body,
                    context,
                    name == ErlangModuleNames.SpawnLink,
                    SequenceSpawnDescription.Module(
                        (Atom)arguments[0],
                        (Atom)arguments[1],
                        arguments[2],
                        SequenceSpawnDescription.DefaultOptions(name)
                    )
                ).Pid);
            }
            );
    }

    public static Pid StartCallback(ProcessContext context, Term callback)
    {
        if (callback is not FunctionTerm && callback is not TupleTerm { Items: [Atom, Atom] })
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return context.Runtime.SpawnTraced(
            child => child.Runtime.Modules.Call(
                child,
                ErlangModuleNames.Module,
                ErlangBasicBifNames.Apply,
                callback,
                Nil.Value
            ),
            context,
            false,
            SequenceSpawnDescription.Callback(callback)
        ).Pid;
    }
}
