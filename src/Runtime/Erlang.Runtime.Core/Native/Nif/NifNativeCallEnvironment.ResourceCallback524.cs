namespace Erlang;

internal sealed unsafe partial class NifNativeCallEnvironment
{
    internal static NifNativeCallEnvironment CreateResourceCallback524(
        ProcessRuntime runtime,
        ModuleCodeImage image,
        nint privateData,
        NifNativeFailureState failure
    ) => new(
        runtime,
        image,
        privateData,
        failure,
        true
    );
}
