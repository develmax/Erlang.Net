namespace Erlang;

internal sealed unsafe partial class NifNativeLibraryOwner
{
    internal bool MatchesPreviousOwner544(
        ProcessRuntime runtime,
        ModuleCodeImage previousImage,
        ModuleCodeImage nextImage
    ) => ReferenceEquals(initializationContext.Runtime, runtime)
        && ReferenceEquals(image, previousImage)
        && !ReferenceEquals(image, nextImage)
        && string.Equals(image.Module, nextImage.Module, StringComparison.Ordinal)
        && Volatile.Read(ref initialized) && !Volatile.Read(ref disposed);

    internal bool MatchesNextOwner544(ProcessRuntime runtime, ModuleCodeImage nextImage)
        => ReferenceEquals(initializationContext.Runtime, runtime)
            && ReferenceEquals(image, nextImage)
            && Volatile.Read(ref initializationStarted)
            && !Volatile.Read(ref initialized) && !Volatile.Read(ref disposed);

    // ROOT must first admit the complete transition/shutdown barrier and prepare
    // publication. This helper supplies callback ABI/private-cell semantics only.
    private int InvokeUpgradeCallback544(
        NifNativeLibraryOwner previous,
        NifNativeCallEnvironment environment,
        nuint information
    )
    {
        ArgumentNullException.ThrowIfNull(previous);
        ArgumentNullException.ThrowIfNull(environment);
        if (!ReferenceEquals(environment.Runtime, initializationContext.Runtime)
            || !ReferenceEquals(environment.Image, image)
            || environment.IsIndependent || !environment.AccessibleOnCurrentThread)
            throw new InvalidOperationException(NifNativeUpgrade544Names.ForeignEnvironment);
        if (entry.Upgrade == 0)
            throw new NifImageAdmissionException(NifImageAdmissionFailure.UpgradeUnsupported);
        environment.BindInitializingPrivateData557(this);
        // Never hold the previous owner's gate across native code: its admitted
        // dirty callbacks may need that gate to read their own private data.
        fixed (nint* nextData = &privateData)
        fixed (nint* oldData = &previous.privateData)
        {
            int result = ((delegate* unmanaged[Cdecl]<nint, nint*, nint*, nuint, int>)entry.Upgrade)(
                environment.Pointer,
                nextData,
                oldData,
                information
            );

            return result;
        }
    }
}
