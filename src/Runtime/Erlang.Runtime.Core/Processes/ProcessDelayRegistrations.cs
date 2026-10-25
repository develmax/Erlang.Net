namespace Erlang;

internal static class ProcessDelayRegistrations
{
    public static void Register(ModuleRegistry registry) => registry.Register(
        ErlangModuleNames.Module,
        ProcessDelayNames.Function,
        2,
        async (context, arguments) =>
        {
            if (arguments[1] is Integer { Value.IsZero: true })
            {
                await context.Runtime.Modules.Call(context, ErlangModuleNames.Module, ErlangProcessQueryNames.Yield);

                return arguments[0];
            }
            TimeSpan? timeout = arguments[1] switch
            {
                Atom { Name: ProcessDelayNames.Infinity } => null,
                Integer number when number.Value >= 0 && number.Value <= uint.MaxValue => TimeSpan.FromMilliseconds((double)number.Value),
                _ => throw new ErlangException(ErlangErrorReasons.TimeoutValue)
            };
            await context.ReceiveAsync<Term>(_ => null, timeout);

            return arguments[0];
        }
    );
}
