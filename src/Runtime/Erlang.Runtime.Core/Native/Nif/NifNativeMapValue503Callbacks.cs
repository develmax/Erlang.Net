using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativeMapValue503Callbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Get(
        nint env,
        nuint mapHandle,
        nuint keyHandle,
        nuint* output
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.ResolveTermOwner(mapHandle, env);
            if (owner.GetValue(mapHandle).StaticTerm is not MapTerm map)
                return NifNativeMapValue503Names.MissingOrNotMap;
            var keyOwner = NifNativeCallEnvironment.ResolveTermOwner(keyHandle, env);
            if (!ReferenceEquals(owner.Runtime, keyOwner.Runtime))
                throw new InvalidOperationException(NifNativeDiagnostics.ForeignRuntime);
            Term key = keyOwner.Get(keyHandle);
            if (!map.TryGet(key, out var value))
                return NifNativeMapValue503Names.MissingOrNotMap;
            if (output == null)
                throw new InvalidOperationException(NifNativeMapValue503Names.NullOutput);
            nuint result = owner.Add(value!);
            *output = result;

            return NifNativeMapValue503Names.Found;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return NifNativeMapValue503Names.HostFailure;
        }
    }
}
