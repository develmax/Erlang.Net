using System.Security.Cryptography;
using Erlang;

namespace Erlang.Tests;

internal static class FlatMapEtfHostAssets
{
    internal static byte[] Read()
    {
        using Stream source = typeof(FlatMapEtfHostAssets).Assembly.GetManifestResourceStream(FlatMapEtfHostNames.Resource)
            ?? throw new InvalidDataException(FlatMapEtfHostNames.Failure);
        using var destination = new MemoryStream();
        source.CopyTo(destination);
        byte[] bytes = destination.ToArray();
        FlatMapEtfHostWire.Require(Convert.ToHexString(SHA256.HashData(bytes)) == FlatMapEtfHostNames.OriginalSha256);

        return bytes;
    }

    internal static Term Import(ProcessRuntime runtime)
    {
        byte[] bytes = Read();
        EtfDecodedTerm decoded = runtime.ImportExternalTerm(bytes);
        FlatMapEtfHostWire.Require(decoded.Used == bytes.Length);

        return decoded.Term;
    }

    internal static MapTerm Pair(ProcessRuntime runtime)
    {
        var pending = new Stack<Term>();
        pending.Push(Import(runtime));
        while (pending.TryPop(out Term? current))
        {
            switch (current)
            {
                case MapTerm map when map.Entries.Count == 2
                    && map.Entries.All(row => row.Key is FunctionTerm { LocalIdentity: not null })
                    && map.Entries.Select(row => ((FunctionTerm)row.Key).LocalIdentity!.OldUnique).Order()
                        .SequenceEqual(new[] { 1, 2 }):
                    return map;
                case Cons list:
                    pending.Push(list.Tail);
                    pending.Push(list.Head);
                    break;
                case TupleTerm tuple:
                    foreach (Term item in tuple.Items.Reverse()) pending.Push(item);
                    break;
                case MapTerm map:
                    foreach (var row in map.Entries)
                    {
                        pending.Push(row.Key);
                        pending.Push(row.Value);
                    }
                    break;
            }
        }
        throw new InvalidDataException(FlatMapEtfHostNames.Failure);
    }

    internal static KeyValuePair<Term, Term>[] OldOrder(MapTerm map)
        => map.Entries.OrderBy(row => ((FunctionTerm)row.Key).LocalIdentity!.OldUnique).ToArray();
}
