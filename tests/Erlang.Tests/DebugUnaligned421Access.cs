using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class DebugUnaligned421Access
{
    internal static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(DebugUnaligned421Names.Failure);
    }

    internal static async Task Initialize(ProcessRuntime runtime)
    {
        byte[] Read(string file)
        {
            using var input = typeof(DebugUnaligned421Access).Assembly.GetManifestResourceStream(DebugUnaligned421Names.Prefix + file)!;
            using var result = new MemoryStream();
            input.CopyTo(result);

            return result.ToArray();
        }
        byte[] beam = Read(DebugUnaligned421Names.Beam);
        var hashes = JsonSerializer.Deserialize<Dictionary<string, string>>(Read(DebugUnaligned421Names.Manifest))!;
        Require(hashes.Count == 1 && hashes[DebugUnaligned421Names.Beam] == Convert.ToHexString(SHA256.HashData(beam)));
        await runtime.InitializeModulesAsync(registry =>
        {
            Require(!registry.HasExport(DebugUnaligned421Names.Module, DebugUnaligned421Names.Function, 2));
            var image = registry.PrepareBeam(beam, new BeamDependencySelection([DebugUnaligned421Names.Runtime]));
            Require(image.NativePrimitiveExports.Count == 1 && image.NativePrimitiveExports.Contains(new BeamFunctionSignature(DebugUnaligned421Names.Function, 2)));
            Require(registry.PublishPreparedBeam(image).Status == BeamPublicationStatus.Published);
            var exports = registry.Exports.Where(e => e.Module == DebugUnaligned421Names.Module).ToArray();
            Require(exports.Length == 3);
            foreach (var export in exports)
                Require(ReferenceEquals(registry.SourceOwner(export.Module, export.Function, export.Arity), image));
        });
    }

    internal static void Representation(BitString input, BitString result, int offset)
    {
        Require(input.Equals(result) && !ReferenceEquals(input, result));
        var type = typeof(BitString);
        object backing = type.GetField(DebugUnaligned421Names.Backing, DebugUnaligned421Names.Members)!.GetValue(result)!;
        object source = type.GetField(DebugUnaligned421Names.Backing, DebugUnaligned421Names.Members)!.GetValue(input)!;
        Require(!ReferenceEquals(source, backing));
        Require((long)type.GetField(DebugUnaligned421Names.Offset, DebugUnaligned421Names.Members)!.GetValue(result)! == offset);
        Require((bool)backing.GetType().GetProperty(DebugUnaligned421Names.Reference, DebugUnaligned421Names.Members)!.GetValue(backing)!);
        byte[] buffer = (byte[])backing.GetType().GetProperty(DebugUnaligned421Names.Buffer, DebugUnaligned421Names.Members)!.GetValue(backing)!;
        Require(buffer.Length == Math.Max(65, (int)(((long)input.BitLength + offset + 7) / 8)));
    }

    internal static ErlangFunction Capture(ProcessRuntime runtime)
    {
        object?[] args = [runtime, DebugUnaligned421Names.Module, DebugUnaligned421Names.Function, 2, null];
        Require((bool)typeof(ProcessRuntime).Assembly.GetType(DebugUnaligned421Names.Native, true)!
            .GetMethod(DebugUnaligned421Names.Capture, DebugUnaligned421Names.Members)!.Invoke(null, args)!);

        return (ErlangFunction)args[4]!;
    }
}
