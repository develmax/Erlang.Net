using System.Numerics;

namespace Erlang;

internal sealed partial class HamtBitmapTerm429
{
    internal IOwnedTermSequence506? OwnedStorage517 => Children as IOwnedTermSequence506;

    internal HamtBitmapTerm429(
        object domain,
        object ownership,
        ushort bitmap,
        IOwnedTermSequence506 children
    )
    {
        if (children.Count == 0 || children.Count != BitOperations.PopCount((uint)bitmap))
            throw new ArgumentException(HamtTermNames429.InvalidBitmap);
        Domain = domain;
        Ownership = ownership;
        Bitmap = bitmap;
        Children = children;
    }
}
