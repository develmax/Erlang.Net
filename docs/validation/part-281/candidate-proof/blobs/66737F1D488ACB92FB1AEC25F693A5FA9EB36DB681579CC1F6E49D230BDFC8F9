namespace Erlang;

internal static class ProcessInfoRegistrations
{
    public static void Register(ModuleRegistry registry) => registry.Register(
        ErlangModuleNames.Module,
        ProcessInfoNames.Function,
        2,
        async (context, arguments) =>
        {
            bool singleton = arguments[1] is Atom or TupleTerm { Items.Count: 2 };
            Term[] options = singleton ? [arguments[1]] : Cons.Items(arguments[1]).ToArray();
            var keys = new ProcessInfoItem[options.Length];
            for (int index = 0; index < options.Length; index++)
            {
                keys[index] = ProcessInfoItem.Parse(options[index]);
            }
            if (arguments[0] is not Pid pid)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            context.Runtime.RequireProcessInfoTarget(pid);
            Term result = await context.Runtime.QueryProcessInfo(
                context,
                pid,
                keys,
                singleton
            );
            context.Cancellation.ThrowIfCancellationRequested();

            return result;
        }
    );
}
