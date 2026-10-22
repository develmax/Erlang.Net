namespace Erlang;

internal static class PrimFileEncodingNative410
{
    internal static bool TryCapture(
        ModuleRegistry owner,
        string module,
        string function,
        int arity,
        out ErlangFunction body
    )
    {
        body = null!;
        if (module != PrimFileEncodingNames410.ModuleName || arity != 1)
            return false;
        if (function is not (PrimFileEncodingNames410.ToNative or PrimFileEncodingNames410.FromNative
            or PrimFileEncodingNames410.Normalize or PrimFileEncodingNames410.Translatable))
            return false;
        body = (context, arguments) =>
        {
            if (!ReferenceEquals(context.Runtime.Modules, owner))
                throw new InvalidOperationException(PrimFileEncodingNames410.Foreign);
            context.Cancellation.ThrowIfCancellationRequested();

            return new PrimFileEncodingWork410(context).Run(function, arguments[0]);
        };

        return true;
    }
}
