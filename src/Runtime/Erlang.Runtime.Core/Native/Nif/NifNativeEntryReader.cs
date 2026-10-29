using System.Runtime.InteropServices;
using System.Text;

namespace Erlang;

internal static unsafe class NifNativeEntryReader
{
    internal static NifNativeAbiEntry Read(nint pointer, string module)
    {
        if (pointer == 0)
            throw new InvalidOperationException(NifNativeDiagnostics.NullNativeEntry);
        int major = Marshal.ReadInt32(pointer);
        int minor = Marshal.ReadInt32(pointer, sizeof(int));
        // The pinned loader rejects versions before reading version-specific tails.
        if (major > NifNativeAbi.MajorVersion || major == NifNativeAbi.MajorVersion && minor > NifNativeAbi.MinorVersion)
            throw Admission(
                NifNativeLoadReasonKind.FutureVersion651,
                module,
                major,
                minor,
                ReadNativeErrorText651(Marshal.ReadIntPtr(pointer, NifNativeVersion651.MinimumErtsOffset))
            );
        if (major < NifNativeVersion651.MinimumMajor || major == NifNativeAbi.MajorVersion && minor == NifNativeVersion651.RejectedMapsMinor)
            throw Admission(
                NifNativeLoadReasonKind.OldVersion651,
                module,
                major,
                minor,
                string.Empty
            );
        if (Marshal.SizeOf<NifNativeAbiEntry>() != NifNativeAbi.EntryBytes ||
            Marshal.SizeOf<NifNativeAbiFunction>() != NifNativeAbi.FunctionBytes ||
            Marshal.SizeOf<NifNativeAbiEntryPrefix651>() != NifNativeVersion651.PrefixBytes ||
            Marshal.SizeOf<NifNativeAbiFunctionLegacy651>() != NifNativeVersion651.LegacyFunctionBytes)
            throw new PlatformNotSupportedException(NifNativeDiagnostics.ManagedLayout);
        var prefix = Marshal.PtrToStructure<NifNativeAbiEntryPrefix651>(pointer);
        var entry = new NifNativeAbiEntry
        {
            Major = prefix.Major,
            Minor = prefix.Minor,
            Name = prefix.Name,
            FunctionCount = prefix.FunctionCount,
            Functions = prefix.Functions,
            Load = prefix.Load,
            Reload = prefix.Reload,
            Upgrade = prefix.Upgrade,
            Unload = prefix.Unload
        };
        if (minor >= NifNativeVersion651.VariantMinor)
        {
            entry.Variant = Marshal.ReadIntPtr(pointer, NifNativeVersion651.VariantOffset);
            string variant = ReadNativeErrorText651(entry.Variant);
            if (variant != NifNativeNames.Variant)
                throw Admission(
                    NifNativeLoadReasonKind.Variant651,
                    module,
                    major,
                    minor,
                    variant
                );
        }
        if (minor >= NifNativeVersion651.FlagsMinor)
            entry.Options = unchecked((uint)Marshal.ReadInt32(pointer, NifNativeVersion651.OptionsOffset));
        if (minor >= NifNativeVersion651.ResourceInitMinor)
            entry.ResourceInitSize = unchecked((nuint)Marshal.ReadIntPtr(pointer, NifNativeVersion651.ResourceInitOffset));
        if (minor >= NifNativeVersion651.MinimumErtsMinor)
            entry.MinimumErts = Marshal.ReadIntPtr(pointer, NifNativeVersion651.MinimumErtsOffset);

        return entry;
    }

    private static string ReadNativeErrorText651(nint pointer)
    {
        if (pointer == 0) return string.Empty;
        byte* text = (byte*)pointer;
        int length = 0;
        while (text[length] != 0)
            length++;

        return Encoding.Latin1.GetString(new ReadOnlySpan<byte>(text, length));
    }

    private static NifNativeEntryAdmissionException Admission(
        NifNativeLoadReasonKind kind,
        string module,
        int major,
        int minor,
        string text
    ) => new(
        NifNativeDiagnostics.UnsupportedNativeEntry,
        new NifNativeLoadReason(
            kind,
            module,
            string.Empty,
            string.Empty,
            0,
            0
        )
        {
            Major651 = major,
            Minor651 = minor,
            VersionText651 = text
        }
    );
}
