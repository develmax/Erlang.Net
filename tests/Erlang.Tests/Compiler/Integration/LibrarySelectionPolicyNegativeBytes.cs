using System.Buffers.Binary;
using System.Text;
using static Erlang.Tests.LibrarySelectionPolicyControlNames;

namespace Erlang.Tests;

internal static class LibrarySelectionPolicyNegativeBytes
{
    internal static byte[] RenameRunExport(byte[] image, string name)
    {
        // Fixed synthetic layout from the sibling's Identity factory; all references stay at their original indices.
        string[] atoms = [StringModule, name, BeamOpcodeEdgeNames.Middle, BeamOpcodeEdgeNames.Leaf,
            RuntimeModule, BeamOpcodeEdgeNames.Length, NativeReason, BeamOpcodeEdgeNames.Other];
        using var payload = new MemoryStream();
        Span<byte> count = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(count, atoms.Length);
        payload.Write(count);
        foreach (string atom in atoms)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(atom);
            payload.WriteByte(checked((byte)bytes.Length));
            payload.Write(bytes);
        }

        return BeamMetadataModeledBytes.WithChunk(image, AtomsChunk, payload.ToArray());
    }
}
