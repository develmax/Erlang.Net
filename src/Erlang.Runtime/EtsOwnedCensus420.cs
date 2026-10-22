using System.Reflection;
using System.Numerics;
namespace Erlang;
// Only explicitly charged objects enter this census. No arbitrary reachable graph walk.
internal sealed class EtsOwnedCensus420(Func<object, nuint> allocatedExtent, CancellationToken cancellation)
{
    private readonly HashSet<object> seen = new(ReferenceEqualityComparer.Instance);
    internal ulong Bytes { get; private set; }
    internal long Objects => seen.Count;

    internal bool Add(object? value)
    {
        cancellation.ThrowIfCancellationRequested();
        if (value is null || !seen.Add(value)) return false;
        Bytes = checked(Bytes + (ulong)allocatedExtent(value));

        return true;
    }

    internal void Array<T>(T[]? value)
    {
        if (value is null || ReferenceEquals(value, System.Array.Empty<T>())) return;
        Add(value);
    }

    internal void Integer(BigInteger value)
    {
        var field = typeof(BigInteger).GetField(EtsOwnedCensusNames420.Bits, BindingFlags.NonPublic | BindingFlags.Instance)
         ?? throw new NotSupportedException(EtsOwnedCensusNames420.Unsupported);
        if (field.FieldType != typeof(uint[])) throw new NotSupportedException(EtsOwnedCensusNames420.Unsupported);
        Array((uint[]?)field.GetValue(value));
    }

    internal void Dictionary<TKey, TValue>(Dictionary<TKey, TValue> value) where TKey : notnull
    {
        if (!Add(value)) return;
        var type = typeof(Dictionary<TKey, TValue>);

        object? Field(string name, bool required)
        {
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field is null)
            {
                if (required) throw new NotSupportedException(EtsOwnedCensusNames420.Unsupported);

                return null;
            }

            return field.GetValue(value);
        }
        object? buckets = Field(EtsOwnedCensusNames420.Buckets, true), entries = Field(EtsOwnedCensusNames420.Entries, true);
        if (buckets is not null && buckets is not int[] || entries is not null && entries is not System.Array) throw new NotSupportedException(EtsOwnedCensusNames420.Unsupported);
        Add(buckets);
        Add(entries);
        Add(Field(EtsOwnedCensusNames420.Keys, false));
        Add(Field(EtsOwnedCensusNames420.Values, false));
        // Comparer instances/global default state are borrowed; entry keys/values are walked explicitly by the owning table.
    }

    internal void List<T>(LinkedList<T> value)
    {
        if (!Add(value)) return;
        for (var node = value.First; node is not null; node = node.Next) Add(node);
    }
}
