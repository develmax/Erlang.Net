namespace Erlang;

internal static class ProcessInfoRegistrations
{
    public static void Register(ModuleRegistry registry) => registry.Register(
        ErlangModuleNames.Module,
        ProcessInfoNames.Function,
        2,
        (context, arguments) =>
        {
            bool singleton = arguments[1] is Atom;
            Term[] options = singleton ? [arguments[1]] : Cons.Items(arguments[1]).ToArray();
            var keys = new Atom[options.Length];
            for (int index = 0; index < options.Length; index++)
            {
                if (options[index] is not Atom key || !ProcessInfoAtoms.IsSupported(key))
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
                keys[index] = key;
            }
            if (arguments[0] is not Pid pid || pid.Node != context.Runtime.Node)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            Term result = context.Runtime.ProcessInfo(pid, keys, singleton);
            context.Cancellation.ThrowIfCancellationRequested();

            return ValueTask.FromResult(result);
        }
    );
}
