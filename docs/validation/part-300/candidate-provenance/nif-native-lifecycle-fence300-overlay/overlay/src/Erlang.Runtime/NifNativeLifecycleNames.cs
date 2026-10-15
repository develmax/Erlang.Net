namespace Erlang;

internal static class NifNativeLifecycleNames
{
    internal const int SingleOperation = 1;
    internal const string ReentrantOperation = "A native initializer or unload callback cannot reenter the native lifecycle fence.";
}
