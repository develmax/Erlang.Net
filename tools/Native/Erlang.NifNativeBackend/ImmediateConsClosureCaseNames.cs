namespace Erlang.NifNativeBackend;

public static class ImmediateConsClosureCaseNames
{
    public const string Physical = "runtime/immediate-cons-closure-real-collector-live-stale-foreign-and-boxed-head";
    public const string NestedPhysical = "runtime/immutable-nested-closure-real-collector-native-stale-foreign-and-mutable-input";
    internal const int NestedLength = 256;
    internal const int BinaryBytes = 512;
    internal const int CellMutationValue = 7021;
    internal const int Length = 1024;
    internal const int BoxedShift = 100;
    internal const string Failure = "Immediate Cons closure ownership regression";
}
