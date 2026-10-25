namespace Erlang;

internal static class NifNativeImmediateOwner
{
    internal static NifNativeCallEnvironment Resolve(nint environmentHint)
    {
        // Native invocation ownership is explicit and nonbinding env arguments are ignored.
        var scope = NifNativeCallbackScope.Current;
        if (scope.InvocationEnvironment is not null)
            return NifNativeCallEnvironment.CurrentInvocation();

        // Preserve the existing explicit valid-env path for unbound managed HOST controls.
        // There is no global owner search and no fallback from an expired bound owner.

        return NifNativeCallEnvironment.Resolve(environmentHint);
    }
}
