namespace Erlang;

internal static class InternalGarbageCollectionNative428
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
        if (owner is null || module != InternalGarbageCollectionNames428.Module
            || function != InternalGarbageCollectionNames428.Function
            || arity != InternalGarbageCollectionNames428.Arity) return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != arity)
                throw new InvalidOperationException(InternalGarbageCollectionNames428.Foreign);
            try
            {
                return ValueTask.FromResult(owner.CollectInternalGarbage428(context, arguments[0]));
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
