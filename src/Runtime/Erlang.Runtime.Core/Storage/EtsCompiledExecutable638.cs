namespace Erlang;

// Unboxed representation avoids a new AST wrapper allocation on an initial
// cursor that never publishes a continuation. Exactly one branch is present.
internal readonly struct EtsCompiledExecutable638
{
    private readonly MatchSpecProgram? legacy;
    private readonly MatchSavedProgram634? native;

    internal EtsCompiledExecutable638(MatchSpecProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        legacy = program;
        native = null;
    }

    internal EtsCompiledExecutable638(MatchSavedProgram634 program)
    {
        ArgumentNullException.ThrowIfNull(program);
        legacy = null;
        native = program;
    }

    internal void RequireOwner638(ProcessRuntime runtime)
    {
        if (native is not null)
        {
            if (legacy is not null || !native.OwnedBy634(runtime))
                throw new ArgumentException(EtsCompiledDispatch638Names.Foreign, nameof(runtime));
        }
        else if (legacy is null)
        {
            throw new InvalidOperationException(EtsCompiledDispatch638Names.Invalid);
        }
    }

    internal EtsCompiledDispatch638 Acquire638(ProcessContext context)
    {
        RequireOwner638(context.Runtime);

        // Nothing fallible is allocated after the real reader is acquired:
        // the returned scope is an unboxed value with this one reader owner.
        return native is null
            ? new EtsCompiledDispatch638(legacy!, null)
            : new EtsCompiledDispatch638(null, native.Acquire634());
    }
}
