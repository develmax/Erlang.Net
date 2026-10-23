namespace Erlang;

public sealed partial class RuntimeRegexEngine411
{
    private static ValueTask<Term> UnicodeTrap453(
        ProcessContext context,
        string function,
        params Term[] arguments
    )
    {
        if (context.Runtime.Modules.SourceOwner(ReNames411.Module, function, arguments.Length) is not PreparedBeamImage)
            throw new NotSupportedException(ReNames411.Unsupported);

        return context.Runtime.Modules.Call(
            context,
            ReNames411.Module,
            function,
            arguments
        );
    }
}
