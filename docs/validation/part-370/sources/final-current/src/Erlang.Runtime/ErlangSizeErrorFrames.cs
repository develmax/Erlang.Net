namespace Erlang;

internal static class ErlangSizeErrorFrames
{
    internal static ErlangException Invalid(
        ProcessContext context,
        string function,
        IReadOnlyList<Term> arguments,
        Term reason
    ) => context.CaptureNativeFailure(
        new ErlangException(reason),
        ErlangModuleNames.Module,
        function,
        arguments
    );
}
