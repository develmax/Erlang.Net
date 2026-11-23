using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private (ReadOnlyMemory<byte> Code, int LabelCount)? PlaceColdProtectedProjectionFailures(
        ReadOnlyMemory<byte> code,
        int labelCount
    )
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TryEnter)
            || !code.Span.Contains(BadMatch) || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body }
            || RewriteColdProtectedProjectionFailures(code, body) is not { } rewritten
            || OrderFinalPhysicalLabelsCore(
                rewritten,
                labelCount,
                formMultiKeySelections: false,
                relocateOnly: true
            ) is not { } ordered)
        {
            return null;
        }

        return (RebuildLineTable(ordered.Code.ToArray(), ordered.LabelCount), ordered.LabelCount);
    }

    private static byte[]? RewriteColdProtectedProjectionFailures(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        if (body.Any(instruction => instruction.Definition.Opcode is DebugLine or ExecutableLine))
        {
            return null;
        }

        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        int copied = 0;
        int entry = 0;
        for (int boundary = 0; boundary < body.Count; boundary++)
        {
            if (body[boundary].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = boundary + 1;
            int end = boundary;
            if (body[boundary].Definition.Opcode == FunctionInfo)
            {
                while (end > start && body[end - 1].Definition.Opcode == Line)
                {
                    end--;
                }

                if (end <= start || body[--end].Definition.Opcode != Label)
                {
                    continue;
                }
            }

            if (end <= start + 12 || body[end - 1].Definition.Opcode != Return)
            {
                continue;
            }

            var failures = new List<int>();
            for (int installed = start + 1; installed + 10 < end; installed++)
            {
                if (body[installed] is not { Definition.Opcode: TryEnter, Operands: [_, _] })
                {
                    continue;
                }

                failures.Clear();
                int cursor = installed + 1;
                while (cursor + 8 < end)
                {
                    byte opcode = body[cursor].Definition.Opcode;
                    if (opcode is Line or Move or LocalCall or ExternalCall)
                    {
                        cursor++;
                        continue;
                    }

                    int jump = cursor + 3;
                    if (body[cursor - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                        || body[jump] is not { Definition.Opcode: Jump, Operands: [var resumed] }
                        || body[jump + 1] is not { Definition.Opcode: Label, Operands: [var cold] }
                        || body[jump + 2] is not { Definition.Opcode: Line, Operands: [{ Value.Sign: > 0 }] }
                        || body[jump + 3] is not { Definition.Opcode: BadMatch, Operands: [var value] } || value != x0
                        || body[jump + 4] is not { Definition.Opcode: Label, Operands: [var continuation] }
                        || continuation.Value != resumed.Value
                        || ReadColdProjectionSecondTest(
                            body,
                            cursor,
                            cold,
                            x0
                        ) is not int second
                        || !(flow ??= new BeamProjectionFlow(body)).TryGetExclusiveForwardTarget(
                            cursor,
                            second,
                            (int)cold.Value,
                            out int target
                        ) || target != jump + 1
                        || !flow.TryGetSingleForwardTarget(jump, (int)resumed.Value, out int selected)
                        || selected != jump + 4)
                    {
                        break;
                    }

                    failures.Add(jump);
                    cursor = jump + 5;
                }

                if (failures.Count >= 2)
                {
                    break;
                }
            }

            if (failures.Count < 2)
            {
                continue;
            }

            foreach (int jump in failures)
            {
                output.Write(code.Span[copied..body[jump].Offset]);
                copied = body[jump + 5].Offset;
            }

            output.Write(code.Span[copied..body[end].Offset]);
            // OTP visits failure before success and emits reverse postorder.
            // The exits of this uninterrupted success chain appear last-first.
            for (int index = failures.Count - 1; index >= 0; index--)
            {
                int jump = failures[index];
                output.Write(code.Span[body[jump + 1].Offset..body[jump + 4].Offset]);
            }

            copied = body[end].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int? ReadColdProjectionSecondTest(
        IReadOnlyList<BeamInstruction> body,
        int index,
        BeamOperand cold,
        BeamOperand value
    )
    {
        if (body[index] is { Definition.Opcode: IsTuple or IsNonemptyList, Operands: [var rejected, var source] }
            && rejected.Value == cold.Value && source == value)
        {
            if (body[index].Definition.Opcode == IsTuple && body[index + 1] is
                { Definition.Opcode: TestArity, Operands: [var wrongArity, var testValue, _] }
                && wrongArity.Value == cold.Value && testValue == value
                && body[index + 2] is { Definition.Opcode: GetTupleElement, Operands: [var tuple, _, _] }
                && tuple == value)
            {
                return index + 1;
            }

            if (body[index].Definition.Opcode == IsNonemptyList
                && body[index + 1] is { Definition.Opcode: GetList, Operands: [var list, var head, var tail] }
                && list == value && head != value && tail != value
                && body[index + 2] is { Definition.Opcode: IsNil, Operands: [var wrongTail, var testedTail] }
                && wrongTail.Value == cold.Value && testedTail == tail)
            {
                return index + 2;
            }
        }

        return null;
    }
}
