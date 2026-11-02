using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativePrintf791Callbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int RenderTerm(
        nuint term,
        long precision,
        nint write,
        nint argument
    )
    {
        try
        {
            if (write == 0) throw new InvalidOperationException(NifNativePrintf791Names.MissingWriter);
            NifNativeCallEnvironment owner = NifNativeCallEnvironment.ResolveTermOwner(term, 0);
            Term value = owner.Get(term);
            var writer = (delegate* unmanaged[Cdecl]<nint, byte*, nuint, int>)write;

            return new TermDisplayWriter(owner.Runtime).WritePrintfTo791(value, precision, bytes =>
            {
                fixed (byte* pointer = bytes)
                    return writer(argument, pointer, (nuint)bytes.Length);
            });
        }
        catch (PrintfTermSinkFailure791 sink)
        {
            return sink.Result;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return -NifNativePrintf791Names.InvalidArgument;
        }
    }
}
