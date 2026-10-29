namespace Erlang;

internal sealed class BinarySearchPattern(BinaryProgramBacking598 backing)
{
    internal BinaryProgramBacking598 Backing598 { get; } = backing;
    internal string Tag => Backing598.Arena598.Kind598 == BinaryArenaKind598.BoyerMoore
        ? BinarySearchNames.SinglePattern : BinarySearchNames.MultiplePatterns;
}
