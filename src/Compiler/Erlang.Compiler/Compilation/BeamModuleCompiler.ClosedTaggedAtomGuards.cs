using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? SelectClosedTaggedAtomGuards(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        Dictionary<int, string>? names = null;
        int copied = 0;
        int next = labelCount;
        for (int index = 1; index + 4 < body.Count; index++)
        {
            if (!ReadAtomGuard(
                body[index],
                out var selector,
                out var firstKey,
                out var retryLabel
            )
                || body[index + 1].Definition.Opcode is Label or FunctionInfo or EndCode
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(index, retryLabel, out int retry)
                || retry + 2 >= body.Count
                || !ReadAtomGuard(
                    body[retry + 1],
                    out var other,
                    out var secondKey,
                    out int failure
                )
                || !SameProjectionRegister(selector, other) || firstKey == secondKey || failure == retryLabel
                || !HasSharedTaggedGuardRoot(
                    body,
                    index,
                    retry,
                    failure
                ))
            {
                continue;
            }

            names ??= atoms.Where(entry => MapEmittedAtom(entry.Value) > 0)
                .ToDictionary(entry => MapEmittedAtom(entry.Value), entry => entry.Key);
            if (!names.TryGetValue(firstKey, out string? first) || !names.TryGetValue(secondKey, out string? second))
            {
                continue;
            }

            int success = next++;
            output.Write(code.Span[copied..body[index].Offset]);
            output.WriteByte(SelectValue);
            if (selector.TypeIndex is int type)
            {
                output.WriteByte(ExtendedTypedRegisterOperand);
                WriteRegister(output, selector);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, type);
            }
            else
            {
                WriteRegister(output, selector);
            }

            BeamCompactWriter.Write(output, LabelTag, failure);
            output.WriteByte(ExtendedListOperand);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 4);
            if (TermOrder.Compare(new Atom(first), new Atom(second), exact: true) < 0)
            {
                WriteArm(firstKey, success);
                WriteArm(secondKey, retryLabel);
            }
            else
            {
                WriteArm(secondKey, retryLabel);
                WriteArm(firstKey, success);
            }

            output.WriteByte(Label);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, success);
            output.Write(code.Span[body[index + 1].Offset..body[retry + 1].Offset]);
            copied = body[retry + 2].Offset;
            index = retry + 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return OrderFinalPhysicalLabelsCore(
            output.ToArray(),
            next,
            formMultiKeySelections: false,
            relocateOnly: true
        );

        void WriteArm(int key, int label)
        {
            BeamCompactWriter.Write(output, AtomTag, key);
            BeamCompactWriter.Write(output, LabelTag, label);
        }
    }

    private static bool HasSharedTaggedGuardRoot(
        IReadOnlyList<BeamInstruction> body,
        int guard,
        int retry,
        int failure
    )
    {
        int check = guard - 1;
        while (check >= 0 && body[check].Definition.Opcode is GetTupleElement or Line)
        {
            check--;
        }

        return check >= 0 && body[check] is
        { Definition.Opcode: IsTaggedTuple, Operands: [var target, ..] }
            && target.Value == failure && !Enumerable.Range(guard + 1, retry - guard)
                .Any(index => body[index].Definition.Opcode is FunctionInfo or EndCode);
    }

    private static bool ReadAtomGuard(
        BeamInstruction instruction,
        out BeamOperand selector,
        out int key,
        out int target
    )
    {
        if (instruction is { Definition.Opcode: ExactEqualTest, Operands: [var failure, var left, var right] }
            && failure.Kind == BeamOperandParameters.Label)
        {
            if (left.Kind == BeamOperandParameters.Atom)
            {
                (left, right) = (right, left);
            }

            if (left.Kind is BeamOperandParameters.X or BeamOperandParameters.Y
                && right.Kind == BeamOperandParameters.Atom)
            {
                selector = left;
                key = (int)right.Value;
                target = (int)failure.Value;

                return true;
            }
        }

        selector = default!;
        key = 0;
        target = 0;

        return false;
    }
}
