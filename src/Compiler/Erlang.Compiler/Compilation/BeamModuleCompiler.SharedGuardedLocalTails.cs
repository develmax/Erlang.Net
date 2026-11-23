using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ShareGuardedLocalTails(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Label)> references)
    {
        if (!code.Span.Contains(ExactEqualTest) || !code.Span.Contains(TailLocalCall))
        {
            return null;
        }

        var uses = references.ToLookup(reference => reference.Label);
        var entries = exports.Concat(locals).Select(entry => MapEmittedLabel(entry.Entry))
            .Concat(closures.Select(closure => MapClosureEntryLabel(closure.Entry))).ToHashSet();
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        for (int index = 0; index + 4 < body.Count; index++)
        {
            if (body[index] is not { Definition.Opcode: ExactEqualTest, Operands: [var failure, _, _] } equality
                || body[index + 1] is not { Definition.Opcode: TailLocalCall } firstTail
                || body[index + 2] is not { Definition.Opcode: Label, Operands: [var alternate] }
                || alternate.Value != failure.Value || entries.Contains((int)alternate.Value))
            {
                continue;
            }

            if (uses[(int)alternate.Value].ToArray() is not [var reference] || reference.Start != equality.Offset + 1)
            {
                continue;
            }

            int firstGuard = index + 3;
            int secondTail = firstGuard;
            while (secondTail < body.Count && IsReadOnlyGuard(body[secondTail], failure))
            {
                secondTail++;
            }

            if (secondTail == firstGuard || secondTail >= body.Count
                || body[secondTail] is not { Definition.Opcode: TailLocalCall } tail
                || !tail.Operands.SequenceEqual(firstTail.Operands))
            {
                continue;
            }

            // Equality already supplies every argument. The alternate tests read
            // the same registers without changing them before the identical call.
            output.Write(code.Span[copied..equality.Offset]);
            output.WriteByte(ExactNotEqualTest);
            output.Write(code.Span[(equality.Offset + 1)..firstTail.Offset]);
            output.Write(code.Span[body[firstGuard].Offset..tail.Offset]);
            output.Write(code.Span[body[index + 2].Offset..body[firstGuard].Offset]);
            copied = tail.Offset;
            index = secondTail;
        }

        if (copied == 0)
        {
            return null;
        }

        highestOpcode = Math.Max(highestOpcode, ExactNotEqualTest);
        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool IsReadOnlyGuard(BeamInstruction instruction, BeamOperand alternate) =>
        instruction.Definition.Opcode is IsLess or IsGreaterOrEqual or NumericEqualTest or NumericNotEqualTest
            or ExactEqualTest or ExactNotEqualTest or IsInteger or IsFloat or IsNumber or IsAtom or IsPid
            or IsReference or IsPort or IsNil or IsBinary or IsList or IsNonemptyList or IsTuple or IsMap or TestArity
        && instruction.Operands is [var failure, ..] && !failure.Value.IsZero && failure.Value != alternate.Value;
}
