using System.Runtime.InteropServices;
using System.Text.Json;
using Erlang;

internal static class NifPlatformAbiChecks
{
    internal static void Run(string path)
    {
        var actual = JsonSerializer.Deserialize<Dictionary<string, int>>(File.ReadAllText(path))!;
        var expected = new Dictionary<string, int>
        {
            ["PointerBytes"] = IntPtr.Size,
            ["LongBytes"] = OperatingSystem.IsWindows() ? sizeof(int) : IntPtr.Size,
            ["ResourceInitBytes"] = Marshal.SizeOf<NativeResourceCallbacks527>(),
            ["MapIteratorBytes"] = Marshal.SizeOf<NifNativeMapIterator504>(),
            ["PidBytes"] = IntPtr.Size,
            ["PortBytes"] = IntPtr.Size,
            ["MonitorBytes"] = 4 * IntPtr.Size,
            ["TimeBytes"] = sizeof(long),
            ["EntryBytes"] = Marshal.SizeOf<NifNativeAbiEntry>(),
            ["FunctionBytes"] = Marshal.SizeOf<NifNativeAbiFunction>(),
            ["BinaryBytes"] = Marshal.SizeOf<NifNativeBinaryDescriptor>(),
            ["EventBytes"] = OperatingSystem.IsWindows() ? IntPtr.Size : sizeof(int),
            ["NameOffset"] = (int)Marshal.OffsetOf<NifNativeAbiEntry>(nameof(NifNativeAbiEntry.Name)),
            ["FunctionsOffset"] = (int)Marshal.OffsetOf<NifNativeAbiEntry>(nameof(NifNativeAbiEntry.Functions)),
            ["LoadOffset"] = (int)Marshal.OffsetOf<NifNativeAbiEntry>(nameof(NifNativeAbiEntry.Load)),
            ["VariantOffset"] = (int)Marshal.OffsetOf<NifNativeAbiEntry>(nameof(NifNativeAbiEntry.Variant)),
            ["ResourceInitOffset"] = (int)Marshal.OffsetOf<NifNativeAbiEntry>(nameof(NifNativeAbiEntry.ResourceInitSize)),
            ["MinimumErtsOffset"] = (int)Marshal.OffsetOf<NifNativeAbiEntry>(nameof(NifNativeAbiEntry.MinimumErts))
        };
        foreach (var entry in expected)
            if (!actual.TryGetValue(entry.Key, out int value) || value != entry.Value)
                throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
        if (actual["WindowsCallbacksBytes"] > (NifNativeWindowsCallbackLayout.FunctionCount + NifNativeWindowsCallbackLayout.TailPointers) * IntPtr.Size)
            throw new InvalidOperationException(NifNativeDiagnostics.ManagedLayout);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Passed = true,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            OperatingSystem = RuntimeInformation.OSDescription,
            Runtime = RuntimeInformation.FrameworkDescription,
            PointerBytes = IntPtr.Size,
            NativeAdmission = NifNativePlatformPolicy.IsSupported,
            AbiSupported = NifNativePlatformPolicy.Supports(
                OperatingSystem.IsWindows(),
                false,
                RuntimeInformation.ProcessArchitecture,
                IntPtr.Size
            ),
            PublicAbi = actual
        }));
    }
}
