using System.Buffers.Binary;

namespace Erlang.HaltProbe;

internal static class HaltWindowsDumpIdentity437
{
    internal static void Verify(string path, uint processId)
    {
        using var file = File.OpenRead(path);
        Span<byte> header = stackalloc byte[HaltWindowsDumpNames437.DumpHeaderBytes];
        file.ReadExactly(header);
        Require(BinaryPrimitives.ReadUInt32LittleEndian(header) == HaltWindowsDumpNames437.Signature);
        uint streams = BinaryPrimitives.ReadUInt32LittleEndian(header[8..]);
        uint directory = BinaryPrimitives.ReadUInt32LittleEndian(header[12..]);
        Require(streams > 0 && directory >= HaltWindowsDumpNames437.DumpHeaderBytes
            && (long)directory + (long)streams * HaltWindowsDumpNames437.DirectoryEntryBytes <= file.Length);
        Span<byte> entry = stackalloc byte[HaltWindowsDumpNames437.DirectoryEntryBytes];
        Span<byte> misc = stackalloc byte[HaltWindowsDumpNames437.MiscPrefixBytes];
        bool found = false;
        for (uint index = 0; index < streams; index++)
        {
            file.Position = (long)directory + (long)index * HaltWindowsDumpNames437.DirectoryEntryBytes;
            file.ReadExactly(entry);
            uint size = BinaryPrimitives.ReadUInt32LittleEndian(entry[4..]);
            uint rva = BinaryPrimitives.ReadUInt32LittleEndian(entry[8..]);
            Require((long)rva + size <= file.Length);
            if (BinaryPrimitives.ReadUInt32LittleEndian(entry) != HaltWindowsDumpNames437.MiscInfoStream) continue;
            Require(!found && size >= HaltWindowsDumpNames437.MiscPrefixBytes);
            file.Position = rva;
            file.ReadExactly(misc);
            uint declared = BinaryPrimitives.ReadUInt32LittleEndian(misc);
            Require(declared >= HaltWindowsDumpNames437.MiscPrefixBytes && declared <= size
                && (BinaryPrimitives.ReadUInt32LittleEndian(misc[4..]) & HaltWindowsDumpNames437.ProcessIdPresent) != 0
                && BinaryPrimitives.ReadUInt32LittleEndian(misc[8..]) == processId);
            found = true;
        }
        Require(found);
    }

    private static void Require(bool valid)
    {
        if (!valid) throw new InvalidDataException(HaltWindowsDumpNames437.InvalidDump);
    }
}
