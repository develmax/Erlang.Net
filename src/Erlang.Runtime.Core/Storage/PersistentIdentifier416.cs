namespace Erlang;

internal static class PersistentIdentifier416
{
    internal static byte[] Encode(Term value)
    {
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);
        switch (value)
        {
            case Pid pid:
                writer.Write(PersistentIdentifierNames416.Pid);
                WriteNode(writer, pid.Node, pid.Creation);
                writer.Write(pid.Id);
                break;
            case PortTerm port:
                writer.Write(PersistentIdentifierNames416.Port);
                WriteNode(writer, port.Node, port.Creation);
                writer.Write(port.Id);
                break;
            case ReferenceTerm reference when value is not ResourceReferenceTerm:
                writer.Write(PersistentIdentifierNames416.Reference);
                WriteNode(writer, reference.Node, reference.Creation);
                writer.Write(reference.Words.Count);
                foreach (uint word in reference.Words) writer.Write(word);
                break;
            default: throw new InvalidDataException(PersistentArenaNames416.Invalid);
        }

        return output.ToArray();
    }

    internal static Term Decode(byte[] data, LocalNodeIdentity? local)
    {
        using var input = new MemoryStream(data, false);
        using var reader = new BinaryReader(input);
        Require(input.Length >= sizeof(byte) + sizeof(int) + sizeof(uint));
        byte kind = reader.ReadByte();
        int length = reader.ReadInt32();
        Require(length >= 0 && length <= (input.Length - input.Position - sizeof(uint)) / sizeof(char));
        var chars = new char[length];
        for (int index = 0; index < length; index++) chars[index] = (char)reader.ReadUInt16();
        string node = new(chars);
        uint creation = reader.ReadUInt32();
        Term value;
        if (kind is PersistentIdentifierNames416.Pid or PersistentIdentifierNames416.Port)
        {
            Require(input.Length - input.Position == sizeof(ulong));
            ulong id = reader.ReadUInt64();
            value = kind == PersistentIdentifierNames416.Pid
                ? local is null ? new Pid(node, id, creation) : new Pid(local, id)
                : local is null ? new PortTerm(node, id, creation) : new PortTerm(local, id);
        }
        else
        {
            Require(kind == PersistentIdentifierNames416.Reference && input.Length - input.Position >= sizeof(int));
            int count = reader.ReadInt32();
            Require(count >= ReferenceIdentityLayout.MinimumWords && count <= ReferenceIdentityLayout.MaximumWords
                && input.Length - input.Position == (long)count * sizeof(uint));
            var words = new uint[count];
            for (int index = 0; index < count; index++) words[index] = reader.ReadUInt32();
            value = local is null ? new ReferenceTerm(node, words, creation) : new ReferenceTerm(local, words);
        }
        Require(input.Position == input.Length);

        return value;
    }

    private static void WriteNode(BinaryWriter writer, string node, uint creation)
    {
        writer.Write(node.Length);
        foreach (char character in node) writer.Write((ushort)character);
        writer.Write(creation);
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidDataException(PersistentArenaNames416.Invalid);
    }
}
