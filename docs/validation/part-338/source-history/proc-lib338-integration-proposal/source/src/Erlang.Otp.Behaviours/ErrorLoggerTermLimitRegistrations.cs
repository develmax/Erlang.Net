namespace Erlang.Otp;

internal static class ErrorLoggerTermLimitRegistrations
{
    public static void Register(ModuleRegistry registry)
        => registry.Register(ErrorLoggerTermLimitNames.Module, ErrorLoggerTermLimitNames.LimitTerm, 1, Limit);

    private static async ValueTask<Term> Limit(ProcessContext context, IReadOnlyList<Term> arguments)
    {
        Term depth = await context.Runtime.Modules.Call(context, ErrorLoggerTermLimitNames.Module, ErrorLoggerTermLimitNames.GetFormatDepth);

        return ErrorLoggerTermLimiter.Limit(arguments[0], depth);
    }
}
