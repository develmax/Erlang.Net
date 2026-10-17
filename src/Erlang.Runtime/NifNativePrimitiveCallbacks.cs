using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Erlang;

namespace Erlang;

internal static unsafe class NifNativePrimitiveCallbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nint PrivateData(nint env)
    {
        try
        {
            return NifNativeCallEnvironment.Resolve(env).PrivateData;
        }
        catch (Exception exception)
        {
            NifNativeCallbackScope.Fail(exception);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nint Allocate(nuint size)
    {
        try
        {
            return NifNativeCallbackScope.Current.Allocations.Allocate(size);
        }
        catch (Exception exception)
        {
            NifNativeCallbackScope.Fail(exception);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nint Reallocate(nint pointer, nuint size)
    {
        try
        {
            var scope = NifNativeCallbackScope.Current;
            if (!scope.ReallocationEnabled)
                throw new InvalidOperationException(NifNativeDiagnostics.ReallocationNotGranted);
            try
            {
                return scope.Allocations.Reallocate(pointer, size);
            }
            catch (OutOfMemoryException)
            {
                // C allocation failure returns NULL without altering pending host/guest state.
                return 0;
            }
        }
        catch (Exception exception)
        {
            NifNativeCallbackScope.Fail(exception);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Free(nint pointer)
    {
        try
        {
            NifNativeCallbackScope.Current.Allocations.Free(pointer);
        }
        catch (Exception exception)
        {
            NifNativeCallbackScope.Fail(exception);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int GetInt(
        nint env,
        nuint term,
        int* output
    )
    {
        try
        {
            Term value = NifNativeCallEnvironment.ResolveTermOwner(term, env).Get(term);
            if (value is not Integer integer || integer.Value < int.MinValue || integer.Value > int.MaxValue)
                return 0;
            if (output == null)
                throw new InvalidOperationException(NifNativeDiagnostics.NullIntegerOutput);
            *output = (int)integer.Value;

            return 1;
        }
        catch (Exception exception)
        {
            NifNativeCallbackScope.Fail(exception);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeInt(nint env, int value)
    {
        try
        {
            return NifNativeImmediateOwner.Resolve(env).Add(Term.I(value));
        }
        catch (Exception exception)
        {
            NifNativeCallbackScope.Fail(exception);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint BadArgument(nint env)
    {
        try
        {
            var environment = NifNativeCallEnvironment.Resolve(env);
            environment.Raise(environment.Add(NifNativeNames.BadArgument));
        }
        catch (Exception exception)
        {
            NifNativeCallbackScope.Fail(exception);
        }

        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeString(
        nint env,
        byte* text,
        int encoding
    ) => NifNativeStringCallbacks.MakeZeroTerminated(env, text, encoding);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint RaiseException(nint env, nuint reason)
    {
        try
        {
            var environment = NifNativeCallEnvironment.Resolve(env);
            environment.Raise(reason);
        }
        catch (Exception exception)
        {
            NifNativeCallbackScope.Fail(exception);
        }

        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int IsException(nint env, nuint term)
    {
        try
        {
            return term == 0 && NifNativeCallEnvironment.Resolve(env).Failure.HasGuestException ? 1 : 0;
        }
        catch (Exception exception)
        {
            NifNativeCallbackScope.Fail(exception);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int HasPendingException(nint env, nuint* reason)
    {
        try
        {
            var environment = NifNativeCallEnvironment.Resolve(env);
            if (!environment.Failure.HasGuestException)
                return 0;
            if (reason != null)
                *reason = environment.PendingReasonHandle;

            return 1;
        }
        catch (Exception exception)
        {
            NifNativeCallbackScope.Fail(exception);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeTuple(nint env, uint count, nint cursor)
    {
        try
        {
            var environment = NifNativeCallEnvironment.Resolve(env);

            return environment.Add(Term.Tuple(NifNativeVariadicCursor.Read(environment, count, cursor)));
        }
        catch (Exception exception)
        {
            NifNativeCallbackScope.Fail(exception);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeList(nint env, uint count, nint cursor)
    {
        try
        {
            var environment = NifNativeCallEnvironment.Resolve(env);

            return environment.Add(Term.List(NifNativeVariadicCursor.Read(environment, count, cursor)));
        }
        catch (Exception exception)
        {
            NifNativeCallbackScope.Fail(exception);

            return 0;
        }
    }
}
