namespace Erlang;

internal static class BeamfileChunkNative418
{
    internal static bool TryCapture(
        ProcessRuntime? owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        if (owner is null || module != BeamfileNativeNames418.Module
            || function != BeamfileNativeNames418.Chunk || arity != BeamfileNativeNames418.Arity)
        {
            body = null!;

            return false;
        }
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime, owner) || arguments.Count != BeamfileNativeNames418.Arity)
                throw new InvalidOperationException(BeamfileNativeNames418.Foreign);
            owner.RequireBeamfileActor418(context);

            return ValueTask.FromResult(BeamfileChunkReader418.Read(arguments[0], arguments[1], context.Cancellation));
        };

        return true;
    }
}
