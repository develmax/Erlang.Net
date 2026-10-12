namespace Erlang;

internal static class TermDisplayRegistrations
{
    public static void Register(ModuleRegistry registry) => registry.Register(
        ErlangModuleNames.Module,
        TermDisplaySyntax.Function,
        1,
        (context, arguments) =>
        {
            byte[] bytes = new TermDisplayWriter(context.Runtime).Write(arguments[0]);
            try
            {
                context.Runtime.StandardStreams.Write(ErlangStandardStreamNames.StandardOutput, BitString.FromOwnedBytes(bytes));
            }
            catch (Exception exception) when (exception is IOException or ObjectDisposedException or NotSupportedException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                // OTP display/1 discards the native printf result.
            }

            return ValueTask.FromResult<Term>(Term.A(ErlangBooleanAtoms.True));
        }
    );
}
