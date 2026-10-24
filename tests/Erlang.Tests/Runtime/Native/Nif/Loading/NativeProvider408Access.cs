using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class NativeProvider408Access
{
    internal static RuntimeLibrarySelection Empty() => new(Array.Empty<RuntimeModuleSelection>());

    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(NativeProvider408Names.Failure);
    }

    internal static Task Enable(ProcessRuntime runtime)
        => runtime.InitializeModulesAsync(registry => RuntimeNativePrimitiveProviders.Register(
            runtime,
            registry,
            NativeProvider408Names.Providers
        )).AsTask();

    internal static bool Has(ProcessRuntime runtime, string function, int arity)
        => runtime.Modules.Exports.Contains((NativeProvider408Names.Module, function, arity));

    internal static void Both(ProcessRuntime runtime)
    {
        RuntimeNativePrimitiveSnapshot snapshot = runtime.NativePrimitives;
        Require(snapshot.Selection == NativeProvider408Names.Providers
            && snapshot.Bindings.Count == NativeProvider408Names.NativeCount
            && snapshot.Bindings.All(binding => (binding.Module == NativeProvider408Names.Module && binding.Provider == RuntimeNativePrimitives.PersistentTermCore
                    || binding.Module == NativeProvider408Names.Ets && binding.Provider == RuntimeNativePrimitives.EtsStorageCore)
                && binding.Assembly.ModuleVersionId == typeof(ProcessRuntime).Module.ModuleVersionId
                && runtime.Modules.SourceOwner(binding.Module, binding.Function, binding.Arity) is null)
            && Has(runtime, NativeProvider408Names.Info, 0));
    }

    internal static byte[] Resource(string name)
    {
        byte[] Read(string file)
        {
            using var input = typeof(NativeProvider408Access).Assembly.GetManifestResourceStream(NativeProvider408Names.ResourcePrefix + file)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(NativeProvider408Names.ResourceManifest))!;
        byte[] bytes = Read(name);
        Require(manifest.TryGetValue(name, out string? hash) && hash == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }

    internal static async Task<BitString> Encode(ProcessRuntime runtime, RuntimeNodeFoundationHostActor actor, Term value)
        => (BitString)await actor.InvokeAsync(async context => await runtime.Modules.Call(
            context,
            NativeProvider408Names.RuntimeModule,
            NativeProvider408Names.Encode,
            value
        ));

    internal static bool NativeError(ErlangException error, string function)
    {
        if (!error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument))
            || error.StackTraceTerm is not Cons { Head: TupleTerm { Items.Count: 4 } frame }
            || !frame.Items[0].Equals(Term.A(NativeProvider408Names.Module))
            || !frame.Items[1].Equals(Term.A(function)))
            return false;

        return IoPrintableOriginal404Terms.List(frame.Items[3], out bool proper).OfType<TupleTerm>().Any(item =>
            proper && item.Items.Count == 2 && item.Items[0].Equals(Term.A(NativeProvider408Names.ErrorInfo))
            && item.Items[1] is MapTerm map && map.Entries.Any(pair => pair.Key.Equals(Term.A(NativeProvider408Names.MetadataModule))
                && pair.Value.Equals(Term.A(NativeProvider408Names.ErrorFormatter))));
    }
}
