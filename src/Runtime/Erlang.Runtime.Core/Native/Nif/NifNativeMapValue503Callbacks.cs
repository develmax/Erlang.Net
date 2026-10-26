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
            var mapValue = owner.GetValue(mapHandle);
            if (mapValue.StaticTerm is not MapTerm && mapValue.Pending is not NifNativeMapNode504)
                return NifNativeMapValue503Names.MissingOrNotMap;
            var keyOwner = NifNativeCallEnvironment.ResolveTermOwner(keyHandle, env);
            if (!ReferenceEquals(owner.Runtime, keyOwner.Runtime))
                throw new InvalidOperationException(NifNativeDiagnostics.ForeignRuntime);
            Term key = keyOwner.Get(keyHandle);
            NifNativeTermValue resultValue;
            if (mapValue.StaticTerm is MapTerm map)
            {
                if (!map.TryGet(key, out var foundValue))
                    return NifNativeMapValue503Names.MissingOrNotMap;
                resultValue = new(foundValue!);
            }
            else
            {
                if (!NifNativeMapGraph504.TryGet(
                    (NifNativeMapNode504)mapValue.Pending!,
                    key,
                    owner.Runtime,
                    out var found
                ))
                    return NifNativeMapValue503Names.MissingOrNotMap;
                resultValue = found.Value;
            }
            if (output == null)
                throw new InvalidOperationException(NifNativeMapValue503Names.NullOutput);
            nuint result = owner.AddValue(resultValue);
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
