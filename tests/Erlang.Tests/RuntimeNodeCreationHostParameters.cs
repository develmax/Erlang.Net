namespace Erlang.Tests;

internal static class RuntimeNodeCreationHostParameters
{
    internal const int FirstNonSmallShift = 59;
    internal const int WireCreationShift = 32;
    internal const long MaximumSmallCreation = (1L << FirstNonSmallShift) - 1;
    internal const uint MinimumCreation = 4;
    internal const uint CollisionSuccessor = 5;
    internal static readonly uint[] RetainedReferenceWords = [173, 29, 53];
    internal static readonly (long Requested, uint Expected)[] TruncationCases =
    [
        (1L << WireCreationShift, MinimumCreation),
        ((1L << WireCreationShift) + MinimumCreation, MinimumCreation),
        ((1L << (FirstNonSmallShift - 1)) + CollisionSuccessor, CollisionSuccessor),
        (MaximumSmallCreation, uint.MaxValue)
    ];
}
