using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativeMapMutation504Callbacks
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Put(
        nint env,
        nuint map,
        nuint key,
        nuint value,
        nuint* output
    )
        => Change(
            env,
            map,
            key,
            value,
            output,
            NifNativeMapMutation504Kind.Put
        );

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Update(
        nint env,
        nuint map,
        nuint key,
        nuint value,
        nuint* output
    )
        => Change(
            env,
            map,
            key,
            value,
            output,
            NifNativeMapMutation504Kind.Update
        );

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    internal static int Remove(
        nint env,
        nuint map,
        nuint key,
        nuint* output
    )
        => Change(
            env,
            map,
            key,
            default,
            output,
            NifNativeMapMutation504Kind.Remove
        );

    private static int Change(
        nint env,
        nuint map,
        nuint key,
        nuint value,
        nuint* output,
        NifNativeMapMutation504Kind kind
    )
    {
        try
        {
            var source = NifNativeCallEnvironment.ResolveTermOwner(map, env);
            NifNativeTermValue original = source.GetValue(map);
            if (!NifNativeMapGraph504.TryNode(original, source.Runtime, out var node))
                return NifNativeMapFamily504Names.Missing;
            var destination = NifNativeCallEnvironment.Resolve(env);
            if (!ReferenceEquals(source, destination))
                throw new InvalidOperationException(NifNativeMapFamily504Names.DestinationOwner);
            var keyOwner = NifNativeCallEnvironment.ResolveTermOwner(key, env);
            RequireRuntime(destination, keyOwner);
            NifNativeTermValue keyValue = keyOwner.GetValue(key);
            NifNativeTermValue replacement = default;
            if (kind != NifNativeMapMutation504Kind.Remove)
            {
                var valueOwner = NifNativeCallEnvironment.ResolveTermOwner(value, env);
                RequireRuntime(destination, valueOwner);
                replacement = valueOwner.GetValue(value);
            }
            var changed = NifNativeMapGraph504.Change(
                node,
                keyValue,
                replacement,
                kind,
                destination.Runtime,
                out bool found
            );
            if (kind == NifNativeMapMutation504Kind.Update && !found)
            {
                if (node.Root is not null)
                {
                    RequireOutput(output);
                    *output = NifNativeMapFamily504Names.NonValue;
                }

                return NifNativeMapFamily504Names.Missing;
            }
            RequireOutput(output);
            if (ReferenceEquals(changed, node))
            {
                *output = map;

                return NifNativeMapFamily504Names.Found;
            }
            nuint handle = destination.AddValue(new(changed));
            *output = handle;

            return NifNativeMapFamily504Names.Found;
        }
        catch (Exception failure)
        {
            NifNativeCallbackScope.Fail(failure);

            return NifNativeMapFamily504Names.Missing;
        }
    }

    internal static void RequireRuntime(NifNativeCallEnvironment owner, NifNativeCallEnvironment argument)
    {
        if (!ReferenceEquals(owner.Runtime, argument.Runtime))
            throw new InvalidOperationException(NifNativeDiagnostics.ForeignRuntime);
    }

    internal static void RequireOutput(nuint* output)
    {
        if (output == null)
            throw new ArgumentException(NifNativeMapFamily504Names.NullOutput);
    }
}
