using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? PrioritizeCheckedConsResult(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsNonemptyList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteCheckedConsResultOrder(code, decoded.Body);
    }

    private static byte[]? RewriteCheckedConsResultOrder(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        for (int start = 0; start + 7 < body.Count; start++)
        {
            if (body[start] is not
                {
                    Definition.Opcode: IsNil,
                    Operands: [{ Kind: BeamOperandParameters.Label } next, { Kind: BeamOperandParameters.X, TypeIndex: null } list]
                })
            {
                continue;
            }

            var assigned = new HashSet<BeamOperand>();
            int jump = start + 1;
            while (body[jump] is
                {
                    Definition.Opcode: Move,
                    Operands: [var constant, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
                } && IsNonRegisterValue(constant) && assigned.Add(slot))
            {
                jump++;
            }

            int label = jump + 1;
            int guard = jump + 2;
            if (assigned.Count < 2 || guard + 3 >= body.Count
                || body[jump] is not { Definition.Opcode: Jump, Operands: [var join] }
                || body[label] is not { Definition.Opcode: Label, Operands: [var alternative] } || next.Value != alternative.Value
                || body[guard] is not
                {
                    Definition.Opcode: IsNonemptyList,
                    Operands: [{ Kind: BeamOperandParameters.Label } failed, var tested]
                } || tested != list || failed.Value == next.Value || failed.Value == join.Value
                || body[guard + 1] is not { Definition.Opcode: GetList, Operands: [var selected, _, _] }
                || selected != list)
            {
                continue;
            }

            int end = guard + 1;
            bool sawLine = false;
            bool sawArithmetic = false;
            for (; end < body.Count; end++)
            {
                BeamInstruction instruction = body[end];
                byte opcode = instruction.Definition.Opcode;
                if (opcode is GcBifOne or GcBifTwo or GcBifThree)
                {
                    if (!sawLine || !instruction.Operands[0].Value.IsZero)
                    {
                        break;
                    }

                    sawArithmetic = true;
                }
                else if (opcode is not (GetList or GetHead or GetTail or Line or TestHeap or PutList or PutTuple or Move))
                {
                    break;
                }

                sawLine |= opcode == Line;
            }

            if (!sawArithmetic || end >= body.Count || end <= guard + assigned.Count + 2
                || body[end] is not { Definition.Opcode: Label, Operands: [var merged] } || merged.Value != join.Value
                || !ReadConsOutputs().SetEquals(assigned)
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(start, (int)next.Value, out int only)
                || only != label)
            {
                continue;
            }

            // The two constructors are disjoint. The empty arm only
            // assigns the join values. Collecting operations keep their
            // explicit line and every original failure keeps its operand.
            output.Write(code.Span[copied..body[start].Offset]);
            WriteTest(IsNonemptyList, next);
            output.Write(code.Span[body[guard + 1].Offset..body[end].Offset]);
            output.Write(code.Span[body[jump].Offset..body[guard].Offset]);
            WriteTest(IsNil, failed);
            output.Write(code.Span[body[start + 1].Offset..body[jump].Offset]);
            copied = body[end].Offset;
            start = end - 1;

            HashSet<BeamOperand> ReadConsOutputs()
            {
                var slots = new HashSet<BeamOperand>();
                for (int index = end - 1; index > guard && body[index].Definition.Opcode == Move; index--)
                {
                    BeamOperand destination = body[index].Operands[1];
                    if (destination.Kind == BeamOperandParameters.Y)
                    {
                        slots.Add(destination);
                    }
                }

                return slots;
            }

            void WriteTest(byte opcode, BeamOperand failure)
            {
                output.WriteByte(opcode);
                BeamCompactWriter.Write(output, LabelTag, failure.Value);
                BeamCompactWriter.Write(output, XRegisterTag, list.Value);
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
