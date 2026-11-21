using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static int[]? PlanDispatchCaseFailures(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        bool standaloneOnly = false
    )
    {
        var labels = new Dictionary<int, int>();
        var exceptions = new List<(int Start, int End)>();
        int selections = 0;
        int lastSelection = -1;
        for (int index = start; index < end; index++)
        {
            BeamInstruction instruction = body[index];
            if (instruction.Definition.Opcode is BadMatch or BadRecord or TryCaseEnd or IfEnd)
            {
                // Distinct exception classes already have their own relative layout and source lines.
                return null;
            }

            if (instruction.Definition.Opcode == SelectValue)
            {
                if (!IsProjectedTagSelection(body, start, index))
                {
                    return null;
                }

                selections++;
                lastSelection = index;
            }

            if (instruction.Definition.Opcode != Label)
            {
                continue;
            }

            labels.Add((int)instruction.Operands[0].Value, index);
            if (index > start && EndsBeamBlock(body[index - 1].Definition.Opcode) && ReadCaseFailure(index) is int exit)
            {
                exceptions.Add((index, exit));
            }
        }

        if (standaloneOnly)
        {
            return selections == 1 && exceptions is [(var coldStart, var coldEnd)]
                ? PlanStandaloneCaseFailure(coldStart, coldEnd)
                : null;
        }

        if (selections < 2 || exceptions.Count < 2
            || ReadForwardCaseReversePostorder(
                body,
                start,
                end,
                labels
            ) is not { } reversePostorder)
        {
            return null;
        }

        var cold = exceptions.SelectMany(region => Enumerable.Range(region.Start, region.End - region.Start + 1)).ToHashSet();
        int[] order = reversePostorder.Where(index => index != start && !cold.Contains(index))
            .Concat(reversePostorder.Where(cold.Contains)).ToArray();
        if (order.Length != end - start - 1 || !PreservesFallthrough(order))
        {
            return null;
        }

        return order;

        int[]? PlanStandaloneCaseFailure(int coldStart, int coldEnd)
        {
            if (coldStart <= lastSelection || coldStart <= start + 1
                || body[coldStart - 1].Definition.Opcode != Return
                || coldEnd != coldStart + 1
                || coldEnd + 4 != end
                || body[coldEnd + 1] is not { Definition.Opcode: Label, Operands: [var passthrough] }
                || body[coldEnd + 2].Definition.Opcode != Deallocate
                || body[coldEnd + 3].Definition.Opcode != Return)
            {
                return null;
            }

            var selection = body[lastSelection].Operands;
            if (selection[1].Value != body[coldStart].Operands[0].Value
                || !selection.Skip(4).Where((_, index) => index % 2 == 0).Any(label => label.Value == passthrough.Value))
            {
                return null;
            }

            // beam_jump moves isolated exits while retaining the order of the ordinary blocks.
            return Enumerable.Range(start + 1, coldStart - start - 1)
                .Concat(Enumerable.Range(coldEnd + 1, end - coldEnd - 1))
                .Concat(Enumerable.Range(coldStart, coldEnd - coldStart + 1)).ToArray();
        }

        int? ReadCaseFailure(int label)
        {
            bool restored = false;
            for (int index = label + 1; index < end; index++)
            {
                BeamInstruction instruction = body[index];
                if (instruction.Definition.Opcode == Line)
                {
                    continue;
                }

                if (!restored && instruction is
                    {
                        Definition.Opcode: Move,
                        Operands: [{ Kind: BeamOperandParameters.X or BeamOperandParameters.Y },
                        { Kind: BeamOperandParameters.X, Value.IsZero: true }]
                    })
                {
                    restored = true;
                    continue;
                }

                return instruction is
                { Definition.Opcode: CaseEnd, Operands: [{ Kind: BeamOperandParameters.X, Value.IsZero: true }] }
                    && (index + 1 == end || body[index + 1].Definition.Opcode == Label) ? index : null;
            }

            return null;
        }

        bool PreservesFallthrough(int[] layout)
        {
            int before = start;
            foreach (int next in layout)
            {
                if (!EndsBeamBlock(body[before].Definition.Opcode) && next != before + 1)
                {
                    return false;
                }

                before = next;
            }

            // A final block cannot accidentally fall through to the next function.
            return EndsBeamBlock(body[before].Definition.Opcode);
        }
    }

    private static bool IsProjectedTagSelection(IReadOnlyList<BeamInstruction> body, int start, int selection)
    {
        var operands = body[selection].Operands;
        if (operands[0].Kind != BeamOperandParameters.X)
        {
            return false;
        }

        for (int index = 3; index < operands.Count; index += 2)
        {
            if (operands[index].Kind != BeamOperandParameters.Atom)
            {
                return false;
            }
        }

        // A shared payload projection can sit between the tag projection and the select.
        for (int index = selection - 1; index > start && index >= selection - 2; index--)
        {
            if (body[index] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [_, { Kind: BeamOperandParameters.Unsigned } field, var result]
                })
            {
                return false;
            }

            if (field.Value.IsZero && SameProjectionRegister(result, operands[0]))
            {
                return true;
            }
        }

        return false;
    }
}
