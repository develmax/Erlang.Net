using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const int FirstUnsqueezedClauseCount = 16;
        private const int MaxSqueezedByteCount = 3;
        private const int MaxEnsuredStringBits = 64;

        private sealed record UnaryBinaryLeaf(byte[] Bytes, Term Result);

        private sealed record UnaryBinaryNode(
            byte[] Prefix,
            int KeyWidth,
            Term? Result,
            (int Key, UnaryBinaryNode Node)[] Children
        )
        {
            internal int LabelCount => Children.Sum(child => 1 + child.Node.LabelCount);
        }

        private bool TryEmitUnaryBinaryLiteralDispatch(FunctionDefinition function)
        {
            if (function.Arity != 1 || function.Clauses.Count < 3)
            {
                return false;
            }

            Clause last = function.Clauses[^1];
            Term? fallback = null;
            if (last.Guard is null && HasUnconditionalParameters(last)
                && !AttributeLiteral.TryNormalizeConstant(last.Body, out fallback))
            {
                return false;
            }

            var alternatives = new List<UnaryBinaryLeaf>();
            foreach (Clause clause in function.Clauses.Take(function.Clauses.Count - (fallback is null ? 0 : 1)))
            {
                if (clause is not { Guard: null, Patterns: [var pattern] }
                    || ReadBinaryLiteral(pattern) is not { Length: > 0 } bytes
                    || !AttributeLiteral.TryNormalizeConstant(clause.Body, out Term result))
                {
                    return false;
                }

                alternatives.Add(new(bytes, result));
            }

            if (alternatives.Count < 3 || alternatives.Count >= FirstUnsqueezedClauseCount
                || ReadUnaryBinaryNode(alternatives.ToArray(), 0) is not { } node)
            {
                return false;
            }

            int context = fallback is null ? 1 : 0;
            int functionInfo = owner.nextLabel - 2;
            int miss = owner.nextLabel + node.LabelCount;
            owner.Op(StartBinaryMatch, LabelTag, fallback is null ? functionInfo : miss);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, context);
            if (fallback is null)
            {
                owner.Op(GetBinaryPosition, XRegisterTag, context);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, context + 1);
            }

            EmitUnaryBinaryNode(node, context, miss);
            Mark(Label());
            if (fallback is not null)
            {
                owner.EmitConstant(fallback, 0);
                owner.instructions.WriteByte(Return);
            }
            else
            {
                owner.Op(SetBinaryPosition, XRegisterTag, context);
                owner.Operand(XRegisterTag, 0);
                owner.Op(GetBinaryTail, XRegisterTag, context);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, context + 1);
                JumpTo(functionInfo);
            }

            return true;
        }

        private static UnaryBinaryNode? ReadUnaryBinaryNode(UnaryBinaryLeaf[] alternatives, int start)
        {
            byte[] first = alternatives[0].Bytes;
            if (alternatives.Length == 1)
            {
                return new(
                    first[start..],
                    0,
                    alternatives[0].Result,
                    []
                );
            }

            // beam_core_to_ssa:squeeze_clauses groups adjacent literal segments at each depth.
            int limit = alternatives.Min(alternative => alternative.Bytes.Length);
            int offset = start;
            while (offset < limit)
            {
                int remaining = limit - offset;
                int width = remaining > MaxSqueezedByteCount ? 2 : remaining;
                int key = ReadUnaryBinaryKey(first, offset, width);
                var groups = alternatives.GroupBy(alternative => ReadUnaryBinaryKey(alternative.Bytes, offset, width))
                    .OrderByDescending(group => group.Key)
                    .ToArray();
                if (groups.Length != 1)
                {
                    var children = new List<(int Key, UnaryBinaryNode Node)>();
                    foreach (var group in groups)
                    {
                        if (ReadUnaryBinaryNode(group.ToArray(), offset + width) is not { } child)
                        {
                            return null;
                        }

                        children.Add((group.Key, child));
                    }

                    return new(
                        first[start..offset],
                        width,
                        null,
                        children.ToArray()
                    );
                }

                offset += width;
            }

            // Equal patterns and ending-vs-continuing alternatives have their own clause order.
            return null;
        }

        private static int ReadUnaryBinaryKey(byte[] bytes, int offset, int width)
        {
            int key = 0;
            for (int index = offset; index < offset + width; index++)
            {
                key = (key << BitSyntaxDefaults.BinaryUnit) | bytes[index];
            }

            return key;
        }

        private void EmitUnaryBinaryNode(UnaryBinaryNode node, int context, int failure)
        {
            ReadOnlySpan<byte> prefix = node.Prefix;
            int prefixBits = prefix.Length * BitSyntaxDefaults.BinaryUnit;
            if (prefixBits > MaxEnsuredStringBits)
            {
                owner.EmitBinaryString(prefix, failure, context);
                prefix = [];
                prefixBits = 0;
            }

            int equalities = (prefixBits + MaximumEqualityCommandBits - 1) / MaximumEqualityCommandBits;
            int keyBits = node.KeyWidth * BitSyntaxDefaults.BinaryUnit;
            bool leaf = node.Result is not null;
            EmitBitMatchHeader(
                context,
                failure,
                equalities * 4 + (leaf ? 2 : 9),
                prefixBits + keyBits,
                leaf ? null : BitSyntaxDefaults.NumericUnit
            );
            if (prefixBits > 0)
            {
                EmitFixedBitEquality(prefixBits, new BigInteger(prefix, isUnsigned: true, isBigEndian: true));
            }
            if (node.Result is { } value)
            {
                owner.EmitConstant(value, 0);
                owner.instructions.WriteByte(Return);

                return;
            }

            int register = context + 1;
            owner.Operand(AtomTag, owner.Intern("integer"));
            owner.Operand(BeamWire.UnsignedTag, register);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(BeamWire.UnsignedTag, keyBits);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(XRegisterTag, register);
            EmitBinarySelect(
                node.Children.Select(child => (child.Key, 1 + child.Node.LabelCount)),
                node.KeyWidth,
                register,
                failure
            );
            foreach (var child in node.Children)
            {
                Mark(Label());
                EmitUnaryBinaryNode(child.Node, context, failure);
            }
        }

    }
}
