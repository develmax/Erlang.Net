namespace Erlang;

internal sealed unsafe partial class NifNativeLibraryOwner
{
    private ErlangFunction BindFunction521((string Name, int Arity) signature, NifNativeFunction521 function)
    {
        if (function.Kind == DirtyNativeKind419.Normal)
            return (context, arguments) => ValueTask.FromResult(Invoke(
                context,
                arguments,
                function.Pointer,
                signature.Arity
            ));

        return (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, initializationContext.Runtime))
                throw new InvalidOperationException(NifNativeDiagnostics.ForeignRuntime);
            if (arguments.Count != signature.Arity)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            ProcessTermFrame506? custody = null;
            bool transferred = false;
            try
            {
                if (context.Runtime.OwnedTermsPilot506)
                {
                    custody = context.TermHeap506.Enter();
                    foreach (Term argument in arguments)
                        custody.RetainLocalGraph507(argument);
                }

                NifImagePhysicalPin521 pin = NifImagePhysicalScope521.Prepare521(custody);
                transferred = true;

                return context.Runtime.ScheduleDirtyNative419(
                    context,
                    function.Kind,
                    image.Module,
                    signature.Name,
                    arguments,
                    (owner, values) => Invoke(
                        owner,
                        values,
                        function.Pointer,
                        signature.Arity
                    ),
                    pin.Register521
                );
            }
            catch
            {
                if (!transferred)
                    custody?.Dispose();
                throw;
            }
        };
    }
}
