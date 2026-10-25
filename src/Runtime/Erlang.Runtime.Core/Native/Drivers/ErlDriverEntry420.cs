using System.Runtime.InteropServices;

namespace Erlang;

internal sealed class ErlDriverEntry420
{
    private readonly nint entry;
    private readonly ErlDriverAbiLayout420 layout;
    private readonly Dictionary<string, nint> addresses = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Delegate?> callbacks = new(StringComparer.Ordinal);

    internal ErlDriverEntry420(nint entry, ErlDriverAbiLayout420 layout)
    {
        this.entry = entry;
        this.layout = layout;
        // Source load_driver_entry rejects an old marker before reading the
        // version tail. Do not read beyond an obsolete entry merely to reject it.
        Marker = Integer(ErlDriverEntryNames420.Marker);
        if (Marker != ErlDriverEntryNames420.ExtendedMarker) return;
        Major = Integer(ErlDriverEntryNames420.Major);
        Minor = Integer(ErlDriverEntryNames420.Minor);
        if (!HasValidVersion) return;
        Flags = Integer(ErlDriverEntryNames420.Flags);
        Name = Pointer(ErlDriverEntryNames420.DriverName);
        // init_driver copies actual operation pointers before entry.init runs.
        // Subsequent guest calls use this immutable admitted function snapshot.
        Capture<ErlDriverEntryTypes420.Initialize>(ErlDriverEntryNames420.Init);
        Capture<ErlDriverEntryTypes420.Start>(ErlDriverEntryNames420.Start);
        Capture<ErlDriverEntryTypes420.Stop>(ErlDriverEntryNames420.Stop);
        Capture<ErlDriverEntryTypes420.Output>(ErlDriverEntryNames420.Output);
        Capture<ErlDriverEntryTypes420.Ready>(ErlDriverEntryNames420.ReadyInput);
        Capture<ErlDriverEntryTypes420.Ready>(ErlDriverEntryNames420.ReadyOutput);
        Capture<ErlDriverEntryTypes420.Finish>(ErlDriverEntryNames420.Finish);
        Capture<ErlDriverEntryTypes420.Control>(ErlDriverEntryNames420.Control);
        Capture<ErlDriverEntryTypes420.Unary>(ErlDriverEntryNames420.Timeout);
        Capture<ErlDriverEntryTypes420.Binary>(ErlDriverEntryNames420.OutputVector);
        Capture<ErlDriverEntryTypes420.Binary>(ErlDriverEntryNames420.ReadyAsync);
        Capture<ErlDriverEntryTypes420.Unary>(ErlDriverEntryNames420.Flush);
        Capture<ErlDriverEntryTypes420.Call>(ErlDriverEntryNames420.Call);
        Capture<ErlDriverEntryTypes420.Binary>(ErlDriverEntryNames420.ProcessExit);
        Capture<ErlDriverEntryTypes420.Binary>(ErlDriverEntryNames420.StopSelect);
        if (Major > ErlDriverEntryNames420.EmergencyMajor
            || Major == ErlDriverEntryNames420.EmergencyMajor && Minor >= ErlDriverEntryNames420.EmergencyMinor)
            Capture<ErlDriverEntryTypes420.Unary>(ErlDriverEntryNames420.EmergencyClose);
    }

    internal nint EntryPointer => entry;
    internal int Major { get; }
    internal int Minor { get; }
    internal int Marker { get; }
    internal int Flags { get; }
    internal nint Name { get; }
    internal bool HasValidVersion => Marker == ErlDriverEntryNames420.ExtendedMarker
        && Major >= ErlDriverEntryNames420.MinimumMajor
        && (Major < ErlDriverEntryNames420.CurrentMajor
            || Major == ErlDriverEntryNames420.CurrentMajor && Minor <= ErlDriverEntryNames420.CurrentMinor);

    private int Integer(string field) => Marshal.ReadInt32(entry, layout.EntryOffset(field, sizeof(int)));

    private nint Pointer(string field) => Marshal.ReadIntPtr(entry, layout.EntryOffset(field, IntPtr.Size));

    private void Capture<T>(string field) where T : Delegate
    {
        nint function = Pointer(field);
        addresses.Add(field, function);
        callbacks.Add(field, function == nint.Zero ? null : Marshal.GetDelegateForFunctionPointer<T>(function));
    }

    internal nint Address(string field) => addresses.TryGetValue(field, out var pointer) ? pointer : nint.Zero;

    internal T? Callback<T>(string field) where T : Delegate
        => callbacks.TryGetValue(field, out var value) ? (T?)value : null;

    internal unsafe bool NameEquals(ReadOnlySpan<byte> expected)
    {
        byte* name = (byte*)Name;
        if (name == null) return false;
        for (int index = 0; index < expected.Length; index++)
            if (name[index] == 0 || name[index] != expected[index]) return false;

        return name[expected.Length] == 0;
    }
}
