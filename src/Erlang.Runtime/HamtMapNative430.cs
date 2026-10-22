namespace Erlang;

internal static class HamtMapNative430
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (owner is null || IntPtr.Size != sizeof(ulong) || !HamtNativeNames430.Signatures.Contains((module, function, arity))) return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != arity)
                throw new InvalidOperationException(HamtNativeNames430.Foreign);
            owner.RequireBeamfileActor418(context);
            owner.RequireHamtInput430(arguments[0]);
            try
            {
                Term result = function switch
                {
                    HamtNativeNames430.Info => HamtIndexProtocol429.Info(owner, arguments[0]),
                    HamtNativeNames430.Children => HamtIndexProtocol429.Children(owner, arguments[0]),
                    HamtNativeNames430.Type => owner.ReadHamtTermType430(arguments[0]),
                    _ => throw new InvalidOperationException(HamtNativeNames430.Foreign)
                };

                return ValueTask.FromResult(result);
            }
            catch (ErlangException error)
            {
                throw context.CaptureNativeFailure(
                    error,
                    module,
                    function,
                    arguments
                );
            }
        };

        return true;
    }
}
