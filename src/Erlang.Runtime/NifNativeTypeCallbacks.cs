using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static class NifNativeTypeCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsAtom(nint env, nuint value)
        => Test(env, value, NifNativeTypeKind.Atom);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsBinary(nint env, nuint value)
        => Test(env, value, NifNativeTypeKind.Binary);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsReference(nint env, nuint value)
        => Test(env, value, NifNativeTypeKind.Reference);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsFunction(nint env, nuint value)
        => Test(env, value, NifNativeTypeKind.Function);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsPid(nint env, nuint value)
        => Test(env, value, NifNativeTypeKind.Pid);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsPort(nint env, nuint value)
        => Test(env, value, NifNativeTypeKind.Port);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsEmptyList(nint env, nuint value)
        => Test(env, value, NifNativeTypeKind.EmptyList);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsList(nint env, nuint value)
        => Test(env, value, NifNativeTypeKind.List);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsTuple(nint env, nuint value)
        => Test(env, value, NifNativeTypeKind.Tuple);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsNumber(nint env, nuint value)
        => Test(env, value, NifNativeTypeKind.Number);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsMap(nint env, nuint value)
        => Test(env, value, NifNativeTypeKind.Map);

    private static int Test(nint env, nuint value, NifNativeTypeKind kind)
    {
        try
        {
            // OTP's pure predicates do not inspect their environment argument.
            Term term = NifNativeCallEnvironment.ResolveTermOwner(value, env).Get(value);
            bool matches = kind switch
            {
                NifNativeTypeKind.Atom => term is Atom,
                NifNativeTypeKind.Binary => term is BitString { IsBinary: true },
                NifNativeTypeKind.Reference => term is ReferenceTerm,
                NifNativeTypeKind.Function => term is FunctionTerm,
                NifNativeTypeKind.Pid => term is Pid,
                NifNativeTypeKind.Port => term is PortTerm,
                NifNativeTypeKind.EmptyList => term is Nil,
                NifNativeTypeKind.List => term is Cons or Nil,
                NifNativeTypeKind.Tuple => term is TupleTerm,
                NifNativeTypeKind.Number => term is Integer or FloatTerm,
                NifNativeTypeKind.Map => term is MapTerm,
                _ => false
            };

            return matches ? 1 : 0;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return 0;
        }
    }
}
