using System.Buffers.Binary;
using Erlang;

namespace Erlang.Tests;

internal static class FlatMapEtfHostWire
{
    internal static EtfEncodingOptions Options(ProcessRuntime runtime, bool deterministic = false)
        => new() { FunctionOwner = runtime.InitializationPid, Deterministic = deterministic };

    internal static byte[] Map(ProcessRuntime runtime, IEnumerable<KeyValuePair<Term, Term>> pairs, bool deterministic = false)
    {
        EtfEncodingOptions options = Options(runtime, deterministic);

        return RawMap(pairs.Select(pair => (
            ExternalTermFormat.Encode(pair.Key, options),
            ExternalTermFormat.Encode(pair.Value, options)
        )));
    }

    internal static byte[] RawMap(IEnumerable<(byte[] Key, byte[] Value)> pairs)
    {
        var entries = pairs.ToArray();
        using var output = new MemoryStream();
        byte[] header = new byte[FlatMapEtfHostNames.MapHeaderBytes];
        header[0] = FlatMapEtfHostNames.EtfVersion;
        header[1] = FlatMapEtfHostNames.MapTag;
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(FlatMapEtfHostNames.MapCountOffset), (uint)entries.Length);
        output.Write(header);
        foreach ((byte[] key, byte[] value) in entries)
        {
            Require(key.Length > 0 && value.Length > 0 && key[0] == FlatMapEtfHostNames.EtfVersion
                && value[0] == FlatMapEtfHostNames.EtfVersion);
            output.Write(key.AsSpan(FlatMapEtfHostNames.VersionBytes));
            output.Write(value.AsSpan(FlatMapEtfHostNames.VersionBytes));
        }

        return output.ToArray();
    }

    internal static void Equal(byte[] expected, byte[] actual) => Require(expected.AsSpan().SequenceEqual(actual));

    internal static void Bindings(ProcessRuntime runtime, MapTerm map)
    {
        byte[] bytes = ExternalTermFormat.Encode(map, Options(runtime));
        EtfDecodedTerm imported = runtime.ImportExternalTerm(bytes);
        Require(imported.Used == bytes.Length && imported.Term is MapTerm);
        var decoded = (MapTerm)imported.Term;
        Require(decoded.Entries.Count == map.Entries.Count);
        foreach (var pair in map.Entries)
            Require(decoded.TryGet(pair.Key, out Term? value) && value!.Equals(pair.Value));
    }

    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidDataException(FlatMapEtfHostNames.Failure);
    }
}
