using System.Buffers.Binary;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private readonly List<byte> stringTable = [];

    private int InternBinaryString(ReadOnlySpan<byte> bytes)
    {
        int offset = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(stringTable).IndexOf(bytes);
        if (offset < 0)
        {
            offset = stringTable.Count;
            stringTable.AddRange(bytes.ToArray());
        }

        return offset;
    }

    private void EmitBinaryString(ReadOnlySpan<byte> bytes, int failure)
    {
        int offset = InternBinaryString(bytes);
        Op(MatchBinaryString, LabelTag, failure);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, checked(bytes.Length * BitSyntaxDefaults.BinaryUnit));
        Operand(BeamWire.UnsignedTag, offset);
    }

    private sealed partial class GeneralEmitter
    {
        private sealed record BinaryPrefix(byte[] Bytes, (int Key, BinaryPrefix Node)[] Children)
        {
            internal int LabelCount => Children.Sum(branch => 1 + branch.Node.LabelCount);
        }

        private bool TryEmitBinaryLiteralDispatch(FunctionDefinition function)
        {
            if (function.Arity != 2 || function.Clauses.Count < 4
                || function.Clauses[^1] is not { Guard: null, Body: Expr.Literal fallback } last
                || !HasUnconditionalParameters(last))
            {
                return false;
            }

            var alternatives = new List<(int Generator, byte[] Prime)>();
            Term? result = null;
            foreach (Clause clause in function.Clauses.SkipLast(1))
            {
                if (clause is not { Guard: null, Patterns: [var first, var second], Body: Expr.Literal value }
                    || result is not null && !result.Equals(value.Value)
                    || ReadBinaryLiteral(first) is not [var generator]
                    || ReadBinaryLiteral(second) is not { Length: > 0 } prime)
                {
                    return false;
                }

                result = value.Value;
                alternatives.Add((generator, prime));
            }

            var groups = alternatives.GroupBy(item => item.Generator).OrderByDescending(group => group.Key).ToArray();
            if (groups.Length < 3 || groups.Count(group => group.Count() == 1) < 2)
            {
                return false;
            }

            var branches = new List<(int Key, byte[]? Literal, BinaryPrefix? Node)>();
            foreach (var group in groups)
            {
                byte[][] values = group.Select(item => item.Prime).ToArray();
                if (values.Length == 1)
                {
                    branches.Add((group.Key, values[0], null));
                }
                else if (ReadBinaryPrefix(values, 0) is { } prefix)
                {
                    branches.Add((group.Key, null, prefix));
                }
                else
                {
                    return false;
                }
            }

            if (branches.All(branch => branch.Node is null))
            {
                return false;
            }

            int success = owner.nextLabel + branches.Sum(branch => 1 + (branch.Node?.LabelCount ?? 0));
            int failure = success + 1;
            EmitBinaryStart(0, failure);
            EmitBinaryInteger(1, 2, 2, failure);
            EmitBinarySelect(
                branches.Select(branch => (branch.Key, 1 + (branch.Node?.LabelCount ?? 0))),
                1,
                2,
                failure
            );
            int lastLiteral = branches.FindLastIndex(branch => branch.Literal is not null);
            for (int index = 0; index < branches.Count; index++)
            {
                var branch = branches[index];
                Mark(Label());
                EmitBinaryEnd(failure);
                if (branch.Literal is { } literal)
                {
                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, 1);
                    owner.LiteralOperand(new BitString(literal));
                    if (index != lastLiteral || index != branches.Count - 1)
                    {
                        JumpTo(success);
                    }
                }
                else
                {
                    EmitBinaryStart(1, failure);
                    EmitBinaryPrefix(branch.Node!, result!, failure);
                }
            }

            Mark(Label());
            owner.EmitConstant(result!, 0);
            owner.instructions.WriteByte(Return);
            Mark(Label());
            owner.EmitConstant(fallback.Value, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private static byte[]? ReadBinaryLiteral(Pattern pattern)
        {
            if (pattern is Pattern.Literal { Value: BitString { IsBinary: true } literal })
            {
                return literal.ByteSpan.ToArray();
            }

            if (pattern is not BitPattern bits)
            {
                return null;
            }

            var bytes = new byte[bits.Segments.Count];
            for (int index = 0; index < bytes.Length; index++)
            {
                var segment = bits.Segments[index];
                if (!IsByte(segment.Specification)
                    || segment.Value is not Pattern.Literal { Value: Integer integer }
                    || integer.Value < byte.MinValue || integer.Value > byte.MaxValue)
                {
                    return null;
                }

                bytes[index] = (byte)integer.Value;
            }

            return bytes;
        }

        private static BinaryPrefix? ReadBinaryPrefix(byte[][] values, int start)
        {
            if (values.Length == 1)
            {
                return new(values[0][start..], []);
            }

            // OTP squeezes long runs of byte segments into two-byte alternatives.
            int limit = values.Min(bytes => bytes.Length);
            int offset = start;
            while (offset + 2 <= limit
                && values.All(bytes => bytes.AsSpan(offset, 2).SequenceEqual(values[0].AsSpan(offset, 2))))
            {
                offset += 2;
            }

            if (offset + 2 > limit)
            {
                return null;
            }

            var children = new List<(int Key, BinaryPrefix Node)>();
            foreach (var group in values.GroupBy(bytes => BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2)))
                .OrderByDescending(group => group.Key))
            {
                if (ReadBinaryPrefix(group.ToArray(), offset + 2) is not { } child)
                {
                    return null;
                }

                children.Add((group.Key, child));
            }

            return new(values[0][start..offset], children.ToArray());
        }

        private void EmitBinaryPrefix(BinaryPrefix node, Term result, int failure)
        {
            if (node.Bytes.Length > 0)
            {
                owner.EmitBinaryString(node.Bytes, failure);
            }

            if (node.Children.Length == 0)
            {
                EmitBinaryEnd(failure);
                owner.EmitConstant(result, 0);
                owner.instructions.WriteByte(Return);
                return;
            }

            EmitBinaryInteger(2, 1, 1, failure);
            EmitBinarySelect(node.Children.Select(branch => (branch.Key, 1 + branch.Node.LabelCount)), 2, 1, failure);
            foreach (var branch in node.Children)
            {
                Mark(Label());
                EmitBinaryPrefix(branch.Node, result, failure);
            }
        }

        private void EmitBinarySelect(IEnumerable<(int Key, int Labels)> branches, int bytes, int register, int failure)
        {
            var table = new List<(int Key, int Label)>();
            int label = owner.nextLabel;
            foreach (var branch in branches)
            {
                table.Add((branch.Key, label));
                label += branch.Labels;
            }

            owner.instructions.WriteByte(SelectValue);
            owner.EmitTypedRegister(register, IntegerTypeBits, 0, bytes == 1 ? byte.MaxValue : ushort.MaxValue);
            owner.Operand(LabelTag, failure);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, table.Count * 2);
            foreach (var entry in table.OrderBy(entry => entry.Key))
            {
                owner.Operand(BeamWire.IntegerTag, entry.Key);
                owner.Operand(LabelTag, entry.Label);
            }
        }

        private void EmitBinaryStart(int source, int failure)
        {
            owner.Op(StartBinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, source);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(XRegisterTag, 0);
        }

        private void EmitBinaryInteger(int bytes, int live, int destination, int failure)
        {
            int bits = bytes * BitSyntaxDefaults.BinaryUnit;
            EmitBinaryCommands(9, failure);
            owner.Operand(AtomTag, owner.Intern("ensure_at_least"));
            owner.Operand(BeamWire.UnsignedTag, bits);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(AtomTag, owner.Intern("integer"));
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(BeamWire.UnsignedTag, bits);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, destination);
        }

        private void EmitBinaryEnd(int failure)
        {
            EmitBinaryCommands(2, failure);
            owner.Operand(AtomTag, owner.Intern("ensure_exactly"));
            owner.Operand(BeamWire.UnsignedTag, 0);
        }

        private void EmitBinaryCommands(int operands, int failure)
        {
            owner.Op(BinaryMatch, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, operands);
        }
    }
}
