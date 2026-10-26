using System.Numerics;

namespace Erlang;

// Shape is immutable. Leaf payloads stay live while native HAMT slots stay fixed.
internal sealed class NifNativeMapBranch504
{
    internal KeyValuePair<NifNativeTermValue, NifNativeTermValue>? Entry { get; }
    internal IReadOnlyList<NifNativeMapBranch504> Children { get; }
    internal ushort Bitmap { get; }
    internal bool Collision { get; }

    internal NifNativeMapBranch504(KeyValuePair<NifNativeTermValue, NifNativeTermValue> entry)
    {
        Entry = entry;
        Children = Array.Empty<NifNativeMapBranch504>();
    }

    internal NifNativeMapBranch504(ushort bitmap, IEnumerable<NifNativeMapBranch504> children, bool collision = false)
    {
        Bitmap = bitmap;
        Collision = collision;
        Children = Array.AsReadOnly(children.ToArray());
    }

    internal int Slot(int digit) => BitOperations.PopCount((uint)Bitmap & ((1U << digit) - 1));

    internal IEnumerable<KeyValuePair<NifNativeTermValue, NifNativeTermValue>> Entries()
    {
        if (Entry is { } entry)
        {
            yield return entry;
            yield break;
        }
        foreach (var child in Children)
            foreach (var pair in child.Entries())
                yield return pair;
    }

    internal NifNativeMapBranch504 Copy(Func<NifNativeTermValue, NifNativeTermValue> read)
        => Entry is { } pair ? new(new(read(pair.Key), read(pair.Value)))
            : new(Bitmap, Children.Select(child => child.Copy(read)), Collision);
}
