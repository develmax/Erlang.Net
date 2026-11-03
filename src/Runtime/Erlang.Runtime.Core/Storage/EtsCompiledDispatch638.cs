namespace Erlang;

internal readonly struct EtsCompiledDispatch638(
    MatchSpecProgram? legacy,
    MatchSavedReader634? native
) : IDisposable
{
    internal MatchSpecResult RunCompiled638(ProcessContext context, Term against)
        => native is null
            ? (legacy ?? throw new InvalidOperationException(EtsCompiledDispatch638Names.Invalid)).RunNative419(context, against)
            : new(
                true,
                native.CopyResult634(context.Runtime, context.Cancellation),
                false,
                false
            );

    internal MatchSpecResult RunCursor638(ProcessContext context, Term against)
        => native is null
            ? (legacy ?? throw new InvalidOperationException(EtsCompiledDispatch638Names.Invalid)).Run(context, against, MatchSpecRunMode.Production)
            : new(
                true,
                native.CopyResult634(context.Runtime, context.Cancellation),
                false,
                false
            );

    public void Dispose() => native?.Dispose();
}
