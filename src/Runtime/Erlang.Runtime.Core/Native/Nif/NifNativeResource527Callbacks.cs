using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativeResource527Callbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nint Allocate(nint token, nuint size)
    {
        try
        {
            NativeResourceType524 type = NativeResourceTypes527.Resolve527(token);

            return type.Runtime.NativeResources524.Allocate524(type, size).Pointer524;
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Keep(nint value)
    {
        try
        {
            NativeResourceAddresses524.Resolve524(value).Keep524();
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static void Release(nint value)
    {
        try
        {
            NativeResourceAddresses524.Resolve524(value).ReleaseNative524();
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint Size(nint value)
    {
        try
        {
            return NativeResourceAddresses524.Resolve524(value).SizeOfResource527;
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint Make(nint env, nint value)
    {
        try
        {
            var owner = NifNativeCallEnvironment.Resolve(env);
            NativeResourcePayload524 payload = NativeResourceAddresses524.Resolve524(value);
            RequireRuntime(owner, payload);

            return owner.Add(owner.Runtime.ReferenceResources.MakeNativeResource524(payload));
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static nuint MakeBinary(
        nint env,
        nint value,
        nint data,
        nuint size
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.Resolve(env);
            NativeResourcePayload524 payload = NativeResourceAddresses524.Resolve524(value);
            RequireRuntime(owner, payload);

            return owner.Add(owner.Runtime.NativeResources524.Binary524(payload, data, size));
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return 0;
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Get(
        nint env,
        nuint value,
        nint token,
        nint* output
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(value, env);
            Term term = owner.Get(value);
            NativeResourcePayload524? payload = NativeResourceRoots524.Direct524(term);
            if (payload is null || payload.Type.Token != token) return 0;
            RequireRuntime(owner, payload);
            if (output == null) throw new ArgumentException(NativeResource527Names.NullOutput);
            *output = payload.Pointer524;

            return 1;
        }
        catch (Exception error)
        {
            NifNativeCallbackScope.Fail(error);

            return 0;
        }
    }

    private static void RequireRuntime(NifNativeCallEnvironment owner, NativeResourcePayload524 payload)
    {
        if (!ReferenceEquals(owner.Runtime, payload.Runtime))
            throw new ArgumentException(NativeResource524Names.Foreign);
    }
}
