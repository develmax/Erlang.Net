using System.Numerics;

namespace Erlang;

// Actual immutable interior node of the owning runtime's map traversal index.
// This is not a complete map and has no fabricated size or key dictionary.
internal sealed class HamtBitmapTerm429 : Term
{
    internal object Domain { get; }
    internal object Ownership { get; }
    internal ushort Bitmap { get; }
    internal IReadOnlyList<Term> Children { get; }

    internal HamtBitmapTerm429(
        object domain,
        object ownership,
        ushort bitmap,
        IEnumerable<Term> children
    )
    {
        Term[] entries = children.ToArray();
        if (entries.Length == 0 || entries.Length != BitOperations.PopCount((uint)bitmap))
            throw new ArgumentException(HamtTermNames429.InvalidBitmap);
        Domain = domain;
        Ownership = ownership;
        Bitmap = bitmap;
        Children = Array.AsReadOnly(entries);
    }
}
