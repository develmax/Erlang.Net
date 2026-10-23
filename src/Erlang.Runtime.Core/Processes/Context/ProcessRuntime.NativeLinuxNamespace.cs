namespace Erlang;

public sealed partial class ProcessRuntime
{
    private readonly object nativeLinuxNamespaceGate = new();
    private NifNativeLinuxNamespace? nativeLinuxNamespace;
    internal nint NativeLinuxVariadicReader => nativeLinuxNamespace?.Reader ?? 0;

    internal NifNativeLinuxCapsuleLease OpenNativeLinuxCapsule(
        NifNativeCapabilityProfile profile,
        ModuleCodeImage image,
        FileStream? verifiedTarget
    )
    {
        lock (nativeLinuxNamespaceGate)
        {
            nativeLinuxNamespace ??= new NifNativeLinuxNamespace(this, image, profile);

            return nativeLinuxNamespace.Open(profile, verifiedTarget);
        }
    }

    private void CloseNativeLinuxNamespace()
    {
        lock (nativeLinuxNamespaceGate)
        {
            nativeLinuxNamespace?.Dispose();
            nativeLinuxNamespace = null;
        }
    }
}
