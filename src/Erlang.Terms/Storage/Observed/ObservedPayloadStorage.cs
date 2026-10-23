using System.Numerics;
using System.Runtime.CompilerServices;

namespace Erlang;

internal static class ObservedPayloadStorage
{
    private static readonly object gate = new();
    private static readonly ConditionalWeakTable<object, ObservedPayloadEntry> entries = new();

    internal static void Atom(string name) => Observe(name, (BigInteger)name.Length * sizeof(char), ObservedPayloadKind.Atom);

    internal static void Binary(byte[] bytes) => Observe(bytes, bytes.LongLength, ObservedPayloadKind.Binary);

    internal static T[] Code<T>(T[] array)
    {
        Observe(array, (BigInteger)array.LongLength * Unsafe.SizeOf<T>(), ObservedPayloadKind.Code);

        return array;
    }

    private static void Observe(object backing, BigInteger bytes, ObservedPayloadKind kind)
    {
        lock (gate)
        {
            if (entries.TryGetValue(backing, out ObservedPayloadEntry? existing))
            {
                if ((int)kind > (int)existing.Kind)
                    existing.Kind = kind;
            }
            else
                entries.Add(backing, new(bytes, kind));
        }
    }

    internal static ObservedPayloadSnapshot Capture(CancellationToken cancellation)
    {
        BigInteger atom = 0, binary = 0, code = 0;
        lock (gate)
        {
            foreach (var entry in entries)
            {
                cancellation.ThrowIfCancellationRequested();
                switch (entry.Value.Kind)
                {
                    case ObservedPayloadKind.Atom:
                        atom += entry.Value.Bytes;
                        break;

                    case ObservedPayloadKind.Binary:
                        binary += entry.Value.Bytes;
                        break;

                    case ObservedPayloadKind.Code:
                        code += entry.Value.Bytes;
                        break;
                }
            }
        }

        return new(atom, binary, code);
    }
}
