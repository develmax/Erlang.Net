namespace Erlang.Otp;

internal static class ProcLibCrashReportDependencies
{
    public static void Register(ModuleRegistry registry)
    {
        OtpLoggerRegistrations.Register(registry);
        registry.Register(
            OtpLoggerNames.Application,
            OtpLoggerNames.GetEnv,
            2,
            (context, args) => ValueTask.FromResult(OtpApplicationEnvironment.For(context.Runtime).Get(args[0], args[1]))
        );
        registry.Register(
            OtpLoggerNames.Application,
            OtpLoggerNames.GetEnv,
            3,
            (context, args) => ValueTask.FromResult(OtpApplicationEnvironment.For(context.Runtime).Get(args[0], args[1], args[2]))
        );
        registry.Register(
            OtpLoggerNames.Application,
            OtpLoggerNames.SetEnv,
            3,
            (context, args) => ValueTask.FromResult(OtpApplicationEnvironment.For(context.Runtime).Set(args[0], args[1], args[2]))
        );
        registry.Register(
            OtpLoggerNames.Application,
            OtpLoggerNames.UnsetEnv,
            2,
            (context, args) => ValueTask.FromResult(OtpApplicationEnvironment.For(context.Runtime).Unset(args[0], args[1]))
        );
        registry.Register(
            OtpLoggerNames.ErrorLogger,
            OtpLoggerNames.GetFormatDepth,
            0,
            (context, _) =>
        {
            Term configured = OtpApplicationEnvironment.For(context.Runtime).Get(
                Term.A(OtpLoggerNames.Kernel),
                Term.A(OtpLoggerNames.ErrorLoggerFormatDepth),
                Term.A(OtpLoggerNames.Unlimited)
            );

            return ValueTask.FromResult(ErrorLoggerTermLimiter.NormalizeDepth(configured));
        }
        );
        ErrorLoggerTermLimitRegistrations.Register(registry);
    }
}
