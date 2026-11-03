namespace Erlang;

public static class RuntimeBootstrapFileSystem
{
    internal static readonly ErlangFunction SpawnBody = SpawnSystem;
    internal static readonly ErlangFunction IsSystemBody = IsSystem;

    public static void Register(ProcessRuntime runtime, ModuleRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        if (!ReferenceEquals(runtime.Modules, registry))
            throw new ArgumentException(RuntimeBootstrapFileNames.ForeignRegistry, nameof(registry));
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(RuntimeBootstrapFileNames.WindowsRequired);
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallBootstrapFileSystem();
    }

    private static ValueTask<Term> SpawnSystem(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        string module = CoreModules.AtomName(arguments[0]);
        string function = CoreModules.AtomName(arguments[1]);
        Term[] values = Cons.Items(arguments[2]).ToArray();
        if (context.Runtime.TrySpawnManagedCollector413(
            context,
            module,
            function,
            values,
            out ProcessHandle? collector
        ))
            return ValueTask.FromResult<Term>(collector!.Pid);

        if (context.Runtime.TrySpawnCodePurger432(
            context,
            module,
            function,
            values,
            out ProcessHandle? purger
        ))
            return ValueTask.FromResult<Term>(purger!.Pid);

        return ValueTask.FromResult<Term>(context.Runtime.SpawnTraced(
            RuntimeVmSpawnInput641.Mfa641(module, function, values),
            context,
            false,
            SequenceSpawnDescription.Module(
                (Atom)arguments[0],
                (Atom)arguments[1],
                arguments[2],
                Nil.Value
            ),
            systemProcess: true
        ).Pid);
    }

    private static ValueTask<Term> IsSystem(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        if (arguments[0] is not Pid pid)
            throw new ErlangException(ErlangErrorReasons.BadArgument);

        return ValueTask.FromResult<Term>(CoreModules.Boolean(context.Runtime.IsSystemProcess(pid)));
    }
}
