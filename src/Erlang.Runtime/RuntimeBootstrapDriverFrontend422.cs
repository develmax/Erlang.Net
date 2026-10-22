namespace Erlang;

// Internal explicit configured-service entry while package/native child
// admission is still a separately reviewed bootstrap prerequisite. It never
// publishes a fabricated partial erl_ddll or changes default native catalogs.
internal static class RuntimeBootstrapDriverFrontend422
{
    internal static ValueTask Register(
        ProcessRuntime runtime,
        ModuleRegistry registry,
        DriverFrontendConfiguration422 configuration,
        CancellationToken cancellation
    )
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(configuration);
        if (!ReferenceEquals(runtime.Modules, registry))
            throw new ArgumentException(DriverParentWireNames422.Foreign, nameof(registry));
        registry.RequireDriverFrontendCaller422();

        return runtime.InstallDriverFrontend422(configuration, cancellation);
    }
}
