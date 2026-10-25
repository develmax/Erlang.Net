using System.Text;

namespace Erlang;

internal static class NifNativeElfCapsule
{
    internal static byte[] Create(params byte[][] dependencies)
    {
        if (dependencies.Length < 2)
            throw new ArgumentException(NifNativeCapsuleDiagnostics.MissingDependencies);
        int stringBytes = 1;
        foreach (byte[] dependency in dependencies)
        {
            if (dependency.Length == 0 || dependency.Contains((byte)0))
                throw new ArgumentException(NifNativeCapsuleDiagnostics.InvalidDependency);
            stringBytes = checked(stringBytes + dependency.Length + 1);
        }
        int strings = NifNativeCapsuleLayout.HeaderBytes + NifNativeCapsuleLayout.ProgramHeaders * NifNativeCapsuleLayout.ProgramHeaderBytes;
        int hash = Align(checked(strings + stringBytes));
        int symbol = hash + NifNativeCapsuleLayout.HashBytes;
        int dynamic = symbol + NifNativeCapsuleLayout.SymbolBytes;
        int dynamicBytes = checked((dependencies.Length + NifNativeCapsuleLayout.FixedDynamicEntries) * NifNativeCapsuleLayout.DynamicEntryBytes);
        byte[] bytes = new byte[checked(dynamic + dynamicBytes)];
        using var stream = new MemoryStream(bytes, writable: true);
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: false);
        writer.Write(NifNativeCapsuleLayout.Identification);
        writer.Write(NifNativeCapsuleLayout.SharedObject);
        writer.Write(NifNativeCapsuleLayout.X64Machine);
        writer.Write(NifNativeCapsuleLayout.Version);
        writer.Write(0UL);
        writer.Write((ulong)NifNativeCapsuleLayout.HeaderBytes);
        writer.Write(0UL);
        writer.Write(0U);
        writer.Write((ushort)NifNativeCapsuleLayout.HeaderBytes);
        writer.Write((ushort)NifNativeCapsuleLayout.ProgramHeaderBytes);
        writer.Write((ushort)NifNativeCapsuleLayout.ProgramHeaders);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        Segment(
            writer,
            NifNativeCapsuleLayout.LoadSegment,
            0,
            bytes.Length,
            NifNativeCapsuleLayout.PageAlignment
        );
        Segment(
            writer,
            NifNativeCapsuleLayout.DynamicSegment,
            dynamic,
            dynamicBytes,
            NifNativeCapsuleLayout.Alignment
        );
        Segment(
            writer,
            NifNativeCapsuleLayout.StackSegment,
            0,
            0,
            NifNativeCapsuleLayout.StackAlignment
        );
        stream.Position = strings + 1;
        var offsets = new List<int>();
        foreach (byte[] dependency in dependencies)
        {
            offsets.Add(checked((int)stream.Position - strings));
            writer.Write(dependency);
            writer.Write((byte)0);
        }
        stream.Position = hash;
        writer.Write(1U);
        writer.Write(1U);
        writer.Write(0U);
        writer.Write(0U);
        stream.Position = dynamic;
        foreach (int offset in offsets)
            Entry(writer, NifNativeCapsuleLayout.Needed, (ulong)offset);
        Entry(writer, NifNativeCapsuleLayout.Hash, (ulong)hash);
        Entry(writer, NifNativeCapsuleLayout.StringTable, (ulong)strings);
        Entry(writer, NifNativeCapsuleLayout.StringBytes, (ulong)stringBytes);
        Entry(writer, NifNativeCapsuleLayout.SymbolTable, (ulong)symbol);
        Entry(writer, NifNativeCapsuleLayout.SymbolEntryBytes, NifNativeCapsuleLayout.SymbolBytes);
        Entry(writer, 0, 0);

        return bytes;
    }

    private static int Align(int value)
        => checked((value + NifNativeCapsuleLayout.Alignment - 1) & ~(NifNativeCapsuleLayout.Alignment - 1));

    private static void Entry(BinaryWriter writer, ulong tag, ulong value)
    {
        writer.Write(tag);
        writer.Write(value);
    }

    private static void Segment(
        BinaryWriter writer,
        uint type,
        int offset,
        int size,
        ulong alignment
    )
    {
        writer.Write(type);
        writer.Write(NifNativeCapsuleLayout.ReadWrite);
        writer.Write((ulong)offset);
        writer.Write((ulong)offset);
        writer.Write(0UL);
        writer.Write((ulong)size);
        writer.Write((ulong)size);
        writer.Write(alignment);
    }
}
