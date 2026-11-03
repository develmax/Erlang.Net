using System.Buffers.Binary;
using System.Text;

namespace Erlang.Tests;

// Finite comparator controls using copies of a fresh admitted original stream.
// They do not manufacture an original API result or rewrite retained evidence.
internal static class OriginalCounters610NegativeControls
{
    internal static void Check(byte[] original)
    {
        var observation = OriginalCounters610Protocol.Read(ExternalTermFormat.Decode(original));
        OriginalCounters610Protocol.Require(OriginalCounters610Compare.Compare(original, original).Passed);
        foreach (string literal in new[] { OriginalCounters610Fields.Memory, OriginalCounters610Fields.Badarg })
        {
            byte[] changed = (byte[])original.Clone();
            int offset = changed.AsSpan().IndexOf(Encoding.UTF8.GetBytes(literal));
            OriginalCounters610Protocol.Require(offset >= 0);
            changed[offset + literal.Length - 1] ^= 1;
            OriginalCounters610Protocol.Require(!OriginalCounters610Compare.Compare(original, changed).Passed);
        }
        var occurrences = new OriginalCounters610Wire(original, observation.Roles).Read();
        string role = OriginalCounters610Names.RoleNames[0];
        var chosen = occurrences.Where(item => item.Role == role).ToArray();
        OriginalCounters610Protocol.Require(chosen.Length > 1);
        ReferenceTerm descriptor = observation.Roles[role];
        uint replacement = 0;
        bool found = false;
        for (int bit = 0; bit < 32; bit++)
        {
            replacement = descriptor.Words[2] ^ (1u << bit);
            if (observation.Roles.Values.All(other => other.Node != descriptor.Node || other.Creation != descriptor.Creation
                || other.Words[0] != descriptor.Words[0] || other.Words[1] != descriptor.Words[1] || other.Words[2] != replacement))
            {
                found = true;
                break;
            }
        }
        OriginalCounters610Protocol.Require(found);
        byte[] inconsistent = (byte[])original.Clone();
        BinaryPrimitives.WriteUInt32BigEndian(inconsistent.AsSpan(chosen[0].Offset + 8, 4), replacement);
        OriginalCounters610Protocol.Require(!OriginalCounters610Compare.Compare(original, inconsistent).Passed);
        byte[] consistent = (byte[])original.Clone();
        foreach (var item in chosen) BinaryPrimitives.WriteUInt32BigEndian(consistent.AsSpan(item.Offset + 8, 4), replacement);
        OriginalCounters610Protocol.Require(OriginalCounters610Compare.Compare(original, consistent).Passed);
    }
}
