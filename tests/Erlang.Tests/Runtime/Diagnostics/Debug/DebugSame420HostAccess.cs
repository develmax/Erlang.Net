using System.Security.Cryptography;
using System.Text.Json;
namespace Erlang.Tests;

internal static class DebugSame420HostAccess
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DebugSame420HostNames.Failure);
    }

    internal static byte[] Beam()
    {
        byte[] Read(string name)
        {
            using var input = typeof(DebugSame420HostAccess).Assembly.GetManifestResourceStream(DebugSame420HostNames.Prefix + name)!;
            using var output = new MemoryStream();
            input.CopyTo(output);

            return output.ToArray();
        }
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(DebugSame420HostNames.Manifest))!;
        byte[] bytes = Read(DebugSame420HostNames.Beam);
        Require(hashes.Count == 1 && hashes[DebugSame420HostNames.Beam] == Convert.ToHexString(SHA256.HashData(bytes)));

        return bytes;
    }

    internal static async Task Initialize(ProcessRuntime runtime)
    {
        await global::Erlang.Tests.PreparedBeamFixtureScope171.RunAsync(
            runtime,
            (registry, scope) =>
        {
            Require(!registry.HasExport(DebugSame420HostNames.Module, DebugSame420HostNames.Function, 2));
            var image = scope.Prepare(() => registry.PrepareBeam(Beam(), new BeamDependencySelection([DebugSame420HostNames.Runtime])));
            Require(image.NativePrimitiveExports.Count == 1 && image.NativePrimitiveExports.Contains(new BeamFunctionSignature(DebugSame420HostNames.Function, 2)));

            return ValueTask.FromResult<Action<ModuleRegistry>>(_ =>
            {
                Require(registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
                var exports = registry.Exports.Where(e => e.Module == DebugSame420HostNames.Module).ToArray();
                Require(exports.Length == 3);
                foreach (var export in exports) Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
            });
        }
        );
    }

    internal static async Task Same(
        ProcessContext context,
        Term a,
        Term b,
        bool expected
    )
    {
        Term value = await context.Runtime.Modules.Call(
            context,
            DebugSame420HostNames.Module,
            DebugSame420HostNames.Function,
            a,
            b
        );
        Require(value.Equals(Term.A(expected ? ErlangBooleanAtoms.True : ErlangBooleanAtoms.False)));
    }

    internal static ErlangFunction Capture(ProcessRuntime runtime)
    {
        object?[] args = [runtime, DebugSame420HostNames.Module, DebugSame420HostNames.Function, 2, null];
        Require((bool)typeof(ProcessRuntime).Assembly.GetType(DebugSame420HostNames.Native, true)!.GetMethod(DebugSame420HostNames.Capture, DebugSame420HostNames.Members)!.Invoke(null, args)!);

        return (ErlangFunction)args[4]!;
    }
}
