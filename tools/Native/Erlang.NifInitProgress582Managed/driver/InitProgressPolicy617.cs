using Erlang;
using static InitProgressNames617;
using static InitProgressAdmission617;

internal static class InitProgressPolicy617
{
    internal static void Install(ProcessRuntime runtime, InitProgressAdmission617 admission)
    {
        string bridge = admission.File(Bridge), bridgeHash = Hash(bridge);
        if (admission.RouteValue == Ordinary)
        {
            Require(Hash(System.IO.Path.Combine(AppContext.BaseDirectory, System.IO.Path.GetFileName(bridge))) == bridgeHash);

            return;
        }
        Type capabilityType = typeof(ProcessRuntime).Assembly.GetType(CapabilityType, true)!;
        var capabilities = admission.Native.ToDictionary(
            item => item.GetProperty(JsonPath).GetString()!,
            item => Activator.CreateInstance(
                capabilityType,
                Members,
                null,
                new object?[] { item.GetProperty(JsonPath).GetString(), item.GetProperty(Sha).GetString(), item.GetProperty(BuildReceiptSha).GetString(), bridge, bridgeHash, null },
                null
            )!,
            StringComparer.Ordinal
        );
        typeof(InitProgressPolicy617).GetMethod(nameof(InstallTyped), Members)!.MakeGenericMethod(capabilityType)
            .Invoke(null, [runtime.Modules, capabilities]);
    }

    private static void InstallTyped<T>(ModuleRegistry registry, IReadOnlyDictionary<string, object> capabilities)
    {
        Func<string, T> actualPolicy = path =>
        {
            Require(capabilities.TryGetValue(path, out object? value));

            return (T)value!;
        };
        typeof(ModuleRegistry).GetMethod(PolicyMethod, Members)!.Invoke(registry, [actualPolicy]);
    }
}
