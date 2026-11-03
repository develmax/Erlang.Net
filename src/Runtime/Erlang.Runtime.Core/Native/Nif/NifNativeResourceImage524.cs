namespace Erlang;

internal sealed unsafe partial class NifNativeResourceImage524(
    ProcessRuntime runtime,
    ModuleCodeImage image,
    NifNativeAllocationOwner allocations,
    Func<nint> privateData,
    Func<bool> reallocation,
    Func<bool> disposed
)
{
    internal ProcessRuntime Runtime { get; } = runtime;
    internal ModuleCodeImage Image { get; } = image;
    internal bool IsDisposed524 => disposed();

    internal NifResourceImagePin524 Retain524() => Image.NifBindings.RetainResourceImage524();

    internal void InvokeDestructor524(nint function, nint payload)
    {
        if (function == 0)
            return;
        var failure = new NifNativeFailureState();
        using var scope = new NifNativeCallbackScope(allocations, failure, reallocation());
        using var environment = NifNativeCallEnvironment.CreateResourceCallback524(
            Runtime,
            Image,
            privateData(),
            failure
        );
        using var binding = scope.BindEnvironment(environment);
        ((delegate* unmanaged[Cdecl]<nint, nint, void>)function)(environment.Pointer, payload);
        failure.ThrowPending();
    }
}
