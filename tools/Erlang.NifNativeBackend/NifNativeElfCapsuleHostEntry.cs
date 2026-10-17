namespace Erlang.NifNativeBackend;

public static class NifNativeElfCapsuleHostEntry
{
    public static Task Metadata()
    {
        Check([NifNativeElfCapsuleHostNames.Bridge, NifNativeElfCapsuleHostNames.RawTarget]);
        Check([NifNativeElfCapsuleHostNames.Bridge, NifNativeElfCapsuleHostNames.Reallocation, NifNativeElfCapsuleHostNames.RawTarget]);
        byte[][][] invalid =
        [
            [],
            [NifNativeElfCapsuleHostNames.Bridge],
            [NifNativeElfCapsuleHostNames.Bridge, []],
            [NifNativeElfCapsuleHostNames.Bridge, [(byte)0]]
        ];
        foreach (byte[][] dependencies in invalid)
        {
            try
            {
                _ = NifNativeElfCapsule.Create(dependencies);
                throw new InvalidOperationException(NifNativeElfCapsuleHostNames.Failure);
            }
            catch (ArgumentException)
            {
            }
        }

        return Task.CompletedTask;
    }

    private static void Check(byte[][] dependencies)
    {
        byte[] bytes = NifNativeElfCapsule.Create(dependencies);
        using var reader = new BinaryReader(new MemoryStream(bytes));
        Require(reader.ReadBytes(NifNativeCapsuleLayout.Identification.Length).SequenceEqual(NifNativeCapsuleLayout.Identification));
        reader.BaseStream.Position = NifNativeElfCapsuleHostNames.HeaderType;
        Require(reader.ReadUInt16() == NifNativeCapsuleLayout.SharedObject);
        Require(reader.ReadUInt16() == NifNativeCapsuleLayout.X64Machine);
        reader.BaseStream.Position = NifNativeElfCapsuleHostNames.ProgramOffset;
        Require(reader.ReadUInt64() == NifNativeCapsuleLayout.HeaderBytes);
        reader.BaseStream.Position = NifNativeElfCapsuleHostNames.ProgramShape;
        Require(reader.ReadUInt16() == NifNativeCapsuleLayout.ProgramHeaderBytes);
        Require(reader.ReadUInt16() == NifNativeCapsuleLayout.ProgramHeaders);
        for (int index = 0; index < NifNativeCapsuleLayout.ProgramHeaders; index++)
        {
            reader.BaseStream.Position = NifNativeCapsuleLayout.HeaderBytes + index * NifNativeCapsuleLayout.ProgramHeaderBytes;
            uint type = reader.ReadUInt32();
            uint flags = reader.ReadUInt32();
            Require((flags & NifNativeElfCapsuleHostNames.Execute) == 0);
            Require(type == NifNativeCapsuleLayout.LoadSegment || type == NifNativeCapsuleLayout.DynamicSegment || type == NifNativeCapsuleLayout.StackSegment);
        }
        reader.BaseStream.Position = NifNativeCapsuleLayout.HeaderBytes + NifNativeCapsuleLayout.ProgramHeaderBytes + NifNativeElfCapsuleHostNames.SegmentOffsetField;
        long dynamic = checked((long)reader.ReadUInt64());
        reader.BaseStream.Position = dynamic;
        var needed = new List<ulong>();
        ulong strings = 0;
        while (true)
        {
            ulong tag = reader.ReadUInt64();
            ulong value = reader.ReadUInt64();
            if (tag == 0)
                break;
            if (tag == NifNativeCapsuleLayout.Needed)
                needed.Add(value);
            if (tag == NifNativeCapsuleLayout.StringTable)
                strings = value;
        }
        Require(needed.Count == dependencies.Length && strings != 0);
        byte[][] expected = dependencies;
        for (int index = 0; index < expected.Length; index++)
        {
            reader.BaseStream.Position = checked((long)(strings + needed[index]));
            Require(reader.ReadBytes(expected[index].Length).SequenceEqual(expected[index]));
            Require(reader.ReadByte() == 0);
        }
    }

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeElfCapsuleHostNames.Failure);
    }
}
