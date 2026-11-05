using System.Runtime.InteropServices;
using Erlang;

internal static class UnixBridgeChecks
{
    internal static async Task Run(string probePath)
    {
        await using var runtime = new ProcessRuntime();
        await using var other = new ProcessRuntime();
        Verify(runtime, other, probePath);
    }

    internal static void VerifyDefaultSymbolScope()
    {
        nint bridge = NifNativePortableUnixApi.EnsureLoaded();
        foreach (string name in NifNativeNames.BridgeSymbols)
            if (NifNativePortableUnixApi.Symbol(0, name) != NativeLibrary.GetExport(bridge, name))
                throw new InvalidOperationException(NifNativeDiagnostics.BridgeContract);
    }

    private static unsafe void Verify(ProcessRuntime runtime, ProcessRuntime other, string probePath)
    {
        NifNativePortableRegistration.EnsureRegistered(runtime, NifNativeCapabilityProfile.ForHostLibrary(Path.GetFullPath(probePath)));
        // Repeated use by the same VM is legal; another VM must not inherit it.
        NifNativePortableRegistration.EnsureRegistered(runtime, NifNativeCapabilityProfile.ForHostLibrary(Path.GetFullPath(probePath)));
        try
        {
            NifNativePortableRegistration.EnsureRegistered(other, NifNativeCapabilityProfile.ForHostLibrary(Path.GetFullPath(probePath)));
            throw new InvalidOperationException("Foreign VM borrowed the process bridge");
        }
        catch (InvalidOperationException error) when (error.Message == NifNativePortableNames.SingleRuntime)
        {
        }
        nint bridge = NifNativePortableUnixApi.EnsureLoaded();
        var allocate = (delegate* unmanaged[Cdecl]<nint>)NativeLibrary.GetExport(bridge, "enif_alloc_env");
        var free = (delegate* unmanaged[Cdecl]<nint, void>)NativeLibrary.GetExport(bridge, "enif_free_env");
        var makeInt = (delegate* unmanaged[Cdecl]<nint, int, nuint>)NativeLibrary.GetExport(bridge, "enif_make_int");
        var getInt = (delegate* unmanaged[Cdecl]<nint, nuint, int*, int>)NativeLibrary.GetExport(bridge, "enif_get_int");
        nint env = allocate();
        if (env == 0)
            throw new InvalidOperationException("Independent native environment allocation failed");
        nint probe = 0;
        try
        {
            int value;
            nuint term = makeInt(env, -123456);
            if (getInt(env, term, &value) != 1 || value != -123456)
                throw new InvalidOperationException("Native integer callback roundtrip failed");
            probe = NifNativePortableUnixApi.Open(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(probePath) + '\0'), false);
            nint alias = NifNativePlatformLoader.NifInitializer(probe);
            if (((delegate* unmanaged[Cdecl]<nint>)alias)() == 0)
                throw new InvalidOperationException("Initializer alias failed");
            nuint* values = stackalloc nuint[17];
            for (int index = 0; index < 17; index++)
                values[index] = makeInt(env, index + 1);
            var tuple = (delegate* unmanaged[Cdecl]<nint, nuint*, nuint>)NativeLibrary.GetExport(probe, "nif_bridge_probe_tuple");
            nuint result = tuple(env, values);
            var owner = NifNativeCallEnvironment.Resolve(env);
            if (owner.Get(result) is not TupleTerm items || items.Items.Count != 17)
                throw new InvalidOperationException("Native tuple17 varargs callback failed");
            for (int index = 0; index < 17; index++)
                if (!items.Items[index].Equals(new Integer(index + 1)))
                    throw new InvalidOperationException("Native tuple17 element order differs");
        }
        finally
        {
            if (probe != 0)
                NifNativePortableUnixApi.Close(probe);
            free(env);
        }
    }
}
