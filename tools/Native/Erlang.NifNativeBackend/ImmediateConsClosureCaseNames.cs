namespace Erlang.NifNativeBackend;

public static class ImmediateConsClosureCaseNames
{
    public const string Physical = "runtime/immediate-cons-closure-real-collector-live-stale-foreign-and-boxed-head";
    internal const int Length = 1024;
    internal const int BoxedShift = 100;
    internal const string Failure = "Immediate Cons closure ownership regression";
}
