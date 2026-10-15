using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Erlang;

namespace NifPrototype;

internal static unsafe class NifPrimitiveCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nint PrivateData(nint env)
    {
        try { return NifCallEnvironment.Resolve(env).PrivateData; }
        catch (Exception exception) { NifCallbackScope.Fail(exception); return 0; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nint Allocate(nuint size)
    {
        try { return NifCallbackScope.Current.Allocate(size); }
        catch (Exception exception) { NifCallbackScope.Fail(exception); return 0; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Free(nint pointer)
    {
        try { NifCallbackScope.Current.Free(pointer); }
        catch (Exception exception) { NifCallbackScope.Fail(exception); }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int GetInt(nint env, nuint term, int* output)
    {
        try
        {
            if (output == null)
                throw new InvalidOperationException("Null NIF integer output.");
            if (NifCallEnvironment.Resolve(env).Get(term) is not Integer integer || integer.Value < int.MinValue || integer.Value > int.MaxValue)
                return 0;
            *output = (int)integer.Value;

            return 1;
        }
        catch (Exception exception) { NifCallbackScope.Fail(exception); return 0; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeInt(nint env, int value)
    {
        try { return NifCallEnvironment.Resolve(env).Add(Term.I(value)); }
        catch (Exception exception) { NifCallbackScope.Fail(exception); return 0; }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint BadArgument(nint env)
    {
        try { NifCallEnvironment.Resolve(env).Fail(new ErlangException(ErlangErrorReasons.BadArgument)); }
        catch (Exception exception) { NifCallbackScope.Fail(exception); }

        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeString(nint env, byte* text, int encoding)
    {
        try
        {
            if (encoding != 1 || text == null)
                throw new NotSupportedException("Fixture only supports nonnull Latin1 NIF strings.");
            int length = 0;
            while (length < 4096 && text[length] != 0)
                length++;
            if (length == 4096)
                throw new NotSupportedException("Fixture NIF string exceeds explicit read boundary.");

            return NifCallEnvironment.Resolve(env).Add(Term.String(Encoding.Latin1.GetString(new ReadOnlySpan<byte>(text, length))));
        }
        catch (Exception exception) { NifCallbackScope.Fail(exception); return 0; }
    }
}
