namespace Erlang;

internal static class ErlangHashRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        registry.RegisterIntrinsic(
            ErlangModuleNames.Module,
            ErlangHashNames.Improved,
            1,
            (_, arguments) => ValueTask.FromResult<Term>(Term.I(ErlangNativeHash.Portable2(arguments[0]) & ErlangHashRanges.DefaultMask))
        );
        registry.RegisterIntrinsic(
            ErlangModuleNames.Module,
            ErlangHashNames.Improved,
            2,
            (_, arguments) =>
        {
            ulong range = Range(arguments[1]);

            HashDiagnosticCheckpoints.Mark(30);
            uint diagnosticHash = ErlangNativeHash.Portable2(arguments[0]);
            HashDiagnosticCheckpoints.Mark(31);
            ValueTask<Term> diagnosticResult = ValueTask.FromResult<Term>(Term.I((long)(diagnosticHash % range)));
            HashDiagnosticCheckpoints.Mark(32);
            return diagnosticResult;
        }
        );
        registry.RegisterIntrinsic(
            ErlangModuleNames.Module,
            ErlangHashNames.Legacy,
            2,
            (_, arguments) =>
        {
            ulong range = Range(arguments[1]);

            return ValueTask.FromResult<Term>(Term.I((long)(1 + ErlangLegacyHash.Portable(arguments[0]) % range)));
        }
        );
    }

    private static ulong Range(Term term) => term is Integer integer && integer.Value > 0 && integer.Value <= ErlangHashRanges.Maximum ? (ulong)integer.Value : throw new ErlangException(ErlangErrorReasons.BadArgument);
}
