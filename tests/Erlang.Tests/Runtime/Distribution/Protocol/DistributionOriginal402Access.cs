using Erlang;
using System.Security.Cryptography;
using System.Text.Json;

namespace Erlang.Tests;

internal static class DistributionOriginal402Access
{
    internal static void Require(bool value)
    {
        if (!value)
            throw new InvalidOperationException(DistributionOriginal402Names.Failure);
    }

    internal static Term[] Proper(Term value)
    {
        var result = new List<Term>();
        while (value is Cons cons)
        {
            result.Add(cons.Head);
            value = cons.Tail;
        }
        DistributionOriginal402Access.Require(value is Nil);

        return result.ToArray();
    }

    internal static TupleTerm Tuple(Term value, int arity)
    {
        DistributionOriginal402Access.Require(value is TupleTerm tuple && tuple.Items.Count == arity);

        return (TupleTerm)value;
    }

    internal static Term Read(string name) => ExternalTermFormat.Decode(LibraryDependencyOriginalHostChecks.ReadRetainedResource(name));

    internal static IReadOnlyDictionary<string, Term> Rows(Term value, int count)
    {
        Term[] rows = Proper(value);
        DistributionOriginal402Access.Require(rows.Length == count);
        var result = new Dictionary<string, Term>(StringComparer.Ordinal);
        foreach (Term item in rows)
        {
            TupleTerm row = Tuple(item, 2);
            DistributionOriginal402Access.Require(row.Items[0] is Atom);
            string name = ((Atom)row.Items[0]).Name;
            DistributionOriginal402Access.Require(result.TryAdd(name, row));
        }

        return result;
    }

    internal static void CompletePortableRows(
        Term original,
        Term actual,
        int total,
        IReadOnlyList<string> portable,
        IReadOnlyList<string> raw
    )
    {
        var expected = Rows(original, total);
        var observed = Rows(actual, total);
        DistributionOriginal402Access.Require(portable.Count + raw.Count == total);
        Term[] leftRows = Proper(original), rightRows = Proper(actual);
        for (int index = 0; index < total; index++)
            DistributionOriginal402Access.Require(Tuple(leftRows[index], 2).Items[0].Equals(Tuple(rightRows[index], 2).Items[0]));
        foreach (string name in portable)
            DistributionOriginal402Access.Require(expected.TryGetValue(name, out Term? left) && observed.TryGetValue(name, out Term? right) && left.Equals(right));
        foreach (string name in raw)
            DistributionOriginal402Access.Require(expected.ContainsKey(name) && observed.ContainsKey(name));
        // Every selected row is compared as a complete term, including the complete outcome/stack.
    }

    internal static void CompleteProfiles(
        Term original,
        Term actual,
        int total,
        IReadOnlyList<string> portable,
        IReadOnlyList<string> raw,
        Action<Term>? rawProtocol = null
    )
    {
        Term[] expected = Proper(original), observed = Proper(actual);
        DistributionOriginal402Access.Require(expected.Length == DistributionOriginal402Names.Profiles && observed.Length == expected.Length);
        for (int index = 0; index < expected.Length; index++)
        {
            TupleTerm left = Tuple(expected[index], 3), right = Tuple(observed[index], 3);
            DistributionOriginal402Access.Require(left.Items[0].Equals(right.Items[0]) && left.Items[1].Equals(right.Items[1]));
            CompletePortableRows(
                left.Items[2],
                right.Items[2],
                total,
                portable,
                raw
            );
            rawProtocol?.Invoke(right.Items[2]);
            rawProtocol?.Invoke(left.Items[2]);
        }
    }

    internal static void Retain(
        string mode,
        Term values,
        Term profiles,
        int portable,
        int raw
    )
    {
        string output = Path.Combine(DistributionOriginal402Names.ArtifactRoot, mode, Guid.NewGuid().ToString(DistributionOriginal402Names.OwnedRunFormat));
        DistributionOriginal402Access.Require(!Directory.Exists(output));
        Directory.CreateDirectory(output);
        byte[] completeValues = ExternalTermFormat.Encode(values), completeProfiles = ExternalTermFormat.Encode(profiles);
        File.WriteAllBytes(Path.Combine(output, DistributionOriginal402Names.ValuesFile), completeValues);
        File.WriteAllBytes(Path.Combine(output, DistributionOriginal402Names.StacksFile), completeProfiles);
        File.WriteAllText(
            Path.Combine(output, DistributionOriginal402Names.ReceiptFile),
            JsonSerializer.Serialize(new
            {
                ValuesSha256 = Convert.ToHexString(SHA256.HashData(completeValues)),
                StacksSha256 = Convert.ToHexString(SHA256.HashData(completeProfiles)),
                StrictPortableWholeRows = portable * (DistributionOriginal402Names.Profiles + 1),
                RawRowsRetainedWithoutCrossVmEqualityClaim = raw * (DistributionOriginal402Names.Profiles + 1),
                FullFilesProjectedOrRewritten = false,
                WholeFileOriginalEqualityClaim = false
            })
        );
    }

    internal static void RetainProbe(Term actual)
    {
        string output = Path.Combine(
            DistributionOriginal402Names.ArtifactRoot,
            DistributionOriginal402Names.ReuseProbe,
            Guid.NewGuid().ToString(DistributionOriginal402Names.OwnedRunFormat)
        );
        DistributionOriginal402Access.Require(!Directory.Exists(output));
        Directory.CreateDirectory(output);
        File.WriteAllBytes(Path.Combine(output, DistributionOriginal402Names.ProbeFile), ExternalTermFormat.Encode(actual));
    }
}
