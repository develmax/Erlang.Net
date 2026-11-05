using System.Runtime.InteropServices;

namespace Erlang;

internal static unsafe class NifNativePortableLayout
{
    internal const string Export = "nif_bridge_platform_layout";
    private const uint Schema = 1;

    internal static void Verify(nint library)
    {
        nuint[] expected =
        [
            (nuint)IntPtr.Size, sizeof(long),
            (nuint)Marshal.SizeOf<NifNativeAbiEntry>(),
            (nuint)Marshal.SizeOf<NifNativeAbiFunction>(),
            (nuint)sizeof(NifNativeBinaryDescriptor),
            (nuint)sizeof(NifNativeBridgeTable),
            (nuint)Marshal.OffsetOf<NifNativeAbiEntry>(nameof(NifNativeAbiEntry.Name)),
            (nuint)Marshal.OffsetOf<NifNativeAbiEntry>(nameof(NifNativeAbiEntry.Functions)),
            (nuint)Marshal.OffsetOf<NifNativeAbiEntry>(nameof(NifNativeAbiEntry.Load)),
            (nuint)Marshal.OffsetOf<NifNativeAbiEntry>(nameof(NifNativeAbiEntry.Variant)),
            (nuint)Marshal.OffsetOf<NifNativeAbiEntry>(nameof(NifNativeAbiEntry.ResourceInitSize)),
            (nuint)Marshal.OffsetOf<NifNativeAbiEntry>(nameof(NifNativeAbiEntry.MinimumErts)),
            sizeof(int)
        ];
        nuint[] actual = new nuint[expected.Length];
        var layout = (delegate* unmanaged[Cdecl]<uint, nuint*, uint, int>)NativeLibrary.GetExport(library, Export);
        fixed (nuint* values = actual)
            if (layout(Schema, values, (uint)actual.Length) != 0 || !actual.AsSpan().SequenceEqual(expected))
                throw new PlatformNotSupportedException(NifNativeDiagnostics.ManagedLayout);
    }
}
