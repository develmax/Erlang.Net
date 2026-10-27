namespace Erlang;

internal static class DebugSharingSizePolicy528
{
    internal const ulong HamtHeadWords = 2;
    internal const ulong CleanupPasses = 2;

    internal static ulong SharedHamtWords(ulong ownWords)
        // Pinned copy.c:463 counts HAMT headers/pointers on the mark pass;
        // copy.c:607 adds those same words again during restoration. Preserve
        // the observable size_shared behavior without mutating guest headers.
        => unchecked(ownWords * CleanupPasses);
}
