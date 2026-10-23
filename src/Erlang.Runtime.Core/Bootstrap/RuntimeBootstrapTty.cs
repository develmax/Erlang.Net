namespace Erlang;

// Explicit scoped native facet, not default registration or a pure OTP substitute.
public static class RuntimeBootstrapTty
{
    public static void Register(
        ProcessRuntime runtime,
        ModuleRegistry registry,
        PreparedBeamImage image,
        RuntimeBootstrapTtyProfile profile
    )
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(image);
        if (!ReferenceEquals(runtime.Modules, registry) || !ReferenceEquals(image.Owner, registry))
            throw new ArgumentException(PrimTtyNames.Foreign);
        if (!OperatingSystem.IsWindows() || profile != RuntimeBootstrapTtyProfile.WindowsRedirectedNoTermcapAnsi)
            throw new PlatformNotSupportedException();
        runtime.RequireNativePrimitiveRegistration();
        registry.InstallBootstrapTty(image);
    }
}
