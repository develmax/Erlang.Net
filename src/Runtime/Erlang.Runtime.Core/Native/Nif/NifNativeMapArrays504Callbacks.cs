using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativeMapArrays504Callbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Create(
        nint env,
        nuint* keys,
        nuint* values,
        nuint count,
        nuint* output
    )
    {
        try
        {
            var owner = NifNativeCallEnvironment.Resolve(env);
            NifNativeMapMutation504Callbacks.RequireOutput(output);
            if (count > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(count), NifNativeMapFamily504Names.ArraySize);
            if (count != 0 && (keys == null || values == null))
                throw new ArgumentException(NifNativeMapFamily504Names.NullArray);
            var entries = new KeyValuePair<NifNativeTermValue, NifNativeTermValue>[(int)count];
            var seen = new HashSet<Term>();
            bool duplicate = false;
            for (int index = 0; index < entries.Length; index++)
            {
                var keyOwner = NifNativeCallEnvironment.ResolveTermOwner(keys[index], env);
                var valueOwner = NifNativeCallEnvironment.ResolveTermOwner(values[index], env);
                NifNativeMapMutation504Callbacks.RequireRuntime(owner, keyOwner);
                NifNativeMapMutation504Callbacks.RequireRuntime(owner, valueOwner);
                NifNativeTermValue key = keyOwner.GetValue(keys[index]);
                entries[index] = new(key, valueOwner.GetValue(values[index]));
                duplicate |= !seen.Add(NifNativeTermGraph.Resolve(key));
            }
            if (duplicate)
            {
                *output = NifNativeMapFamily504Names.NonValue;

                return NifNativeMapFamily504Names.Missing;
            }
            nuint result = owner.AddValue(NifNativeMapGraph504.Create(entries, owner.Runtime));
            *output = result;

            return NifNativeMapFamily504Names.Found;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return NifNativeMapFamily504Names.Missing;
        }
    }
}
