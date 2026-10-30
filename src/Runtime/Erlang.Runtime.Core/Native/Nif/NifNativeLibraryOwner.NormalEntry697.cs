namespace Erlang;

internal sealed unsafe partial class NifNativeLibraryOwner
{
    private Term InvokeNormal697(
        ProcessContext context,
        IReadOnlyList<Term> arguments,
        nint function,
        int arity
    )
    {
        if (!ReferenceEquals(context.Runtime, initializationContext.Runtime))
            throw new InvalidOperationException(NifNativeDiagnostics.ForeignRuntime);
        // Acquired before any native-owner gate; active through actual native
        // return and synchronous environment/result custody cleanup.
        using var admission = context.Runtime.EnterNormalNative697(context);

        return Invoke(
            context,
            arguments,
            function,
            arity
        );
    }
}
