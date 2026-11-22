// SPDX-License-Identifier: Apache-2.0
// Comparison tests follow OTP-29.1.1 lib/compiler/src/beam_ssa_codegen.erl.
// Original Erlang implementation copyright Ericsson AB 1996-2026.

using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private ReadOnlyMemory<byte> FoldExhaustiveComparisonBranches(ReadOnlyMemory<byte> code, ref int labelCount)
    {
        if (!code.Span.Contains(BifTwo) || !code.Span.Contains(SelectValue)
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlang)
            || !atoms.TryGetValue(ErlangBooleanAtoms.False, out int falseAtom)
            || !atoms.TryGetValue(ErlangBooleanAtoms.True, out int trueAtom)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body }
            || body.Any(instruction => instruction.Definition.Opcode is DebugLine or ExecutableLine))
        {
            return code;
        }

        var comparisonImports = new Dictionary<int, (byte Test, bool Reverse)>();
        AddComparison(ErlangOperators.Less, IsLess);
        AddComparison(ErlangOperators.Greater, IsLess, reverse: true);
        AddComparison(ErlangOperators.GreaterOrEqual, IsGreaterOrEqual);
        AddComparison(ErlangOperators.LessOrEqual, IsGreaterOrEqual, reverse: true);
        AddComparison(ErlangOperators.NumericEqual, NumericEqualTest);
        AddComparison(ErlangOperators.NumericNotEqual, NumericNotEqualTest);
        AddComparison(ErlangOperators.ExactEqual, ExactEqualTest);
        AddComparison(ErlangOperators.ExactNotEqual, ExactNotEqualTest);
        if (comparisonImports.Count == 0 || RewriteExhaustiveComparisonBranches(
            code,
            body,
            comparisonImports,
            MapEmittedAtom(falseAtom),
            MapEmittedAtom(trueAtom)
        ) is not { } rewritten)
        {
            return code;
        }

        var (bytes, removedLabels) = rewritten;
        int physicalCount = labelCount - removedLabels;
        if (RenumberOutlinedMapCaseLabels(
            bytes,
            labelCount,
            physicalLabelCount: physicalCount
        ) is not { } renumbered)
        {
            return code;
        }

        labelCount = physicalCount;

        return RebuildLineTable(renumbered, labelCount);

        void AddComparison(string name, byte opcode, bool reverse = false)
        {
            if (atoms.TryGetValue(name, out int atom) && imports.TryGetValue((erlang, atom, 2), out int index))
            {
                comparisonImports.Add(emittedImports?[index] ?? index, (opcode, reverse));
            }
        }
    }

    private static (byte[] Code, int RemovedLabels)? RewriteExhaustiveComparisonBranches(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyDictionary<int, (byte Test, bool Reverse)> imports,
        int falseAtom,
        int trueAtom
    )
    {
        BeamProjectionFlow? flow = null;
        var tests = new Dictionary<int, (byte Opcode, bool Reverse, int Failure)>();
        var removed = new HashSet<int>();
        var branchValues = new Dictionary<int, (BeamOperand Register, int Atom)>();
        int removedLabels = 0;
        for (int index = 0; index + 2 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: BifTwo,
                    Operands: [{ Kind: BeamOperandParameters.Failure }, var import, var left, var right, var result]
                }
                || !imports.TryGetValue((int)import.Value, out var comparison)
                || body[index + 1] is not
                {
                    Definition.Opcode: SelectValue,
                    Operands: [var value, var impossible, _, var first, var firstLabel, var second, var secondLabel]
                }
                || !SameProjectionRegister(value, result) || first.Kind != BeamOperandParameters.Atom
                || second.Kind != BeamOperandParameters.Atom || firstLabel.Value == secondLabel.Value
                || body[index + 2] is not { Definition.Opcode: Label, Operands: [var next] })
            {
                continue;
            }

            int yes;
            int no;
            if (first.Value == trueAtom && second.Value == falseAtom)
            {
                yes = (int)firstLabel.Value;
                no = (int)secondLabel.Value;
            }
            else if (first.Value == falseAtom && second.Value == trueAtom)
            {
                yes = (int)secondLabel.Value;
                no = (int)firstLabel.Value;
            }
            else
            {
                continue;
            }

            int select = index + 1;
            if (next.Value != yes && next.Value != no || impossible.Value == yes || impossible.Value == no
                || !(flow ??= new BeamProjectionFlow(body)).TryGetForwardLabel(select, yes, out int yesEntry)
                || !flow.TryGetForwardLabel(select, no, out int noEntry)
                || !flow.TryGetForwardLabel(select, (int)impossible.Value, out int cold))
            {
                continue;
            }

            bool keepsTrue = !flow.IsEntryTemporaryDead(yesEntry, result);
            bool keepsFalse = !flow.IsEntryTemporaryDead(noEntry, result);
            if (keepsTrue && !flow.TryGetSingleForwardTarget(select, yes, out _)
                || keepsFalse && !flow.TryGetSingleForwardTarget(select, no, out _))
            {
                continue;
            }

            bool ownsCold = flow.TryGetSingleForwardTarget(select, (int)impossible.Value, out _);
            int exit = cold;
            if (ownsCold && !TryReadUnusedComparisonFailure(
                body,
                cold,
                result,
                out exit
            ))
            {
                continue;
            }

            // The edge carries a known boolean whenever the original result
            // is still read. Only the original select can reach such an entry.
            if (keepsTrue)
            {
                branchValues.Add(yesEntry + 1, (result, trueAtom));
            }

            if (keepsFalse)
            {
                branchValues.Add(noEntry + 1, (result, falseAtom));
            }

            byte opcode = next.Value == yes ? comparison.Test : InvertComparisonTest(comparison.Test);
            bool reverse = comparison.Reverse;
            if (opcode is NumericEqualTest or NumericNotEqualTest or ExactEqualTest or ExactNotEqualTest)
            {
                // beam_ssa_codegen places an immediate on the right.
                reverse = left.Kind is not (BeamOperandParameters.X or BeamOperandParameters.Y)
                    && right.Kind is BeamOperandParameters.X or BeamOperandParameters.Y;
            }

            tests.Add(index, (opcode, reverse, next.Value == yes ? no : yes));
            removed.Add(select);
            if (ownsCold)
            {
                for (int dead = cold; dead <= exit; dead++)
                {
                    removed.Add(dead);
                }

                removedLabels++;
            }

            if (flow.TryGetSingleForwardTarget(select, (int)next.Value, out _))
            {
                removed.Add(select + 1);
                removedLabels++;
            }

            index = select;
        }
        if (tests.Count == 0)
        {
            return null;
        }

        using var output = new MemoryStream(code.Length);
        for (int index = 0; index < body.Count; index++)
        {
            if (branchValues.TryGetValue(index, out var known))
            {
                output.WriteByte(Move);
                BeamCompactWriter.Write(output, AtomTag, known.Atom);
                WriteRegister(output, known.Register);
            }

            int from = body[index].Offset;
            int end = index + 1 < body.Count ? body[index + 1].Offset : code.Length;
            if (tests.TryGetValue(index, out var test))
            {
                var cursor = new BeamCursor(code[(from + 1)..end]);
                _ = BeamCompactReader.Read(cursor); // Failure.
                _ = BeamCompactReader.Read(cursor); // Import.
                int first = cursor.Position;
                SkipComparisonSource(cursor, body[index].Operands[2]);
                int second = cursor.Position;
                SkipComparisonSource(cursor, body[index].Operands[3]);
                ReadOnlySpan<byte> left = code.Span.Slice(from + 1 + first, second - first);
                ReadOnlySpan<byte> right = code.Span.Slice(from + 1 + second, cursor.Position - second);
                output.WriteByte(test.Opcode);
                BeamCompactWriter.Write(output, LabelTag, test.Failure);
                output.Write(test.Reverse ? right : left);
                output.Write(test.Reverse ? left : right);
            }
            else if (!removed.Contains(index))
            {
                output.Write(code.Span[from..end]);
            }
        }

        return (output.ToArray(), removedLabels);
    }

    private static bool TryReadUnusedComparisonFailure(
        IReadOnlyList<BeamInstruction> body,
        int cold,
        BeamOperand result,
        out int exit
    )
    {
        exit = cold + 1;
        if (exit >= body.Count)
        {
            return false;
        }

        bool hasLine = body[exit].Definition.Opcode == Line;
        if (hasLine)
        {
            exit++;
        }

        if (exit + 1 >= body.Count || body[exit] is not { Definition.Opcode: CaseEnd, Operands: [var operand] }
            || !SameProjectionRegister(result, operand))
        {
            return false;
        }

        // A removed line cannot supply the location of a later failure. The
        // next block either starts a fresh line or belongs to the next MFA.
        return body[exit + 1].Definition.Opcode == Label
            && (!hasLine || exit + 2 < body.Count && body[exit + 2].Definition.Opcode is Line or FunctionInfo)
            || body[exit + 1].Definition.Opcode == EndCode;
    }

    private static void SkipComparisonSource(BeamCursor cursor, BeamOperand value)
    {
        _ = BeamCompactReader.Read(cursor);
        if (value.TypeIndex is not null || value.Kind == BeamOperandParameters.LiteralValue)
        {
            _ = BeamCompactReader.Read(cursor);
        }

        if (value.TypeIndex is not null)
        {
            _ = BeamCompactReader.Read(cursor);
        }
    }

    private static byte InvertComparisonTest(byte opcode) => opcode switch
    {
        IsLess => IsGreaterOrEqual,
        IsGreaterOrEqual => IsLess,
        NumericEqualTest => NumericNotEqualTest,
        NumericNotEqualTest => NumericEqualTest,
        ExactEqualTest => ExactNotEqualTest,
        ExactNotEqualTest => ExactEqualTest,
        _ => throw new ArgumentOutOfRangeException(nameof(opcode))
    };
}
