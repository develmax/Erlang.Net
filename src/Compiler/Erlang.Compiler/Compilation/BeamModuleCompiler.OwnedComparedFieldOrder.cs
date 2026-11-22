using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record ComparedCaptureOrder(int[] Slots, BeamOperand[] Sources);

    private static ComparedCaptureOrder? ReadComparedCaptureOrder(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int allocation,
        int name,
        int cold,
        int width,
        BeamProjectionFlow flow
    )
    {
        if (entry < 1 || allocation + width + 1 >= name - 1 || name + 6 >= cold
            || body[entry - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arguments] }
            || body[entry + 2].Definition.Opcode != SelectTupleArity
            || body[cold - 2].Definition.Opcode != Deallocate || body[cold - 1].Definition.Opcode != Return
            || body.Skip(name + 5).Take(cold - name - 7).Any(instruction => instruction.Definition.Opcode is not
                (Line or LocalCall or ExternalCall or TestHeap or GetTupleElement or PutTuple or PutList or UpdateRecord or Move)))
        {
            return null;
        }

        var pending = new Dictionary<BeamOperand, int>();
        var saved = new HashSet<int>();
        for (int index = allocation + 1; index < allocation + width; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var original, { Kind: BeamOperandParameters.Y, Value.Sign: > 0, TypeIndex: null } slot]
                } || !IsBareX(original) || slot.Value >= width
                || !saved.Add((int)slot.Value) || !pending.TryAdd(original, (int)slot.Value))
            {
                return null;
            }
        }

        var local = new BeamOperand(BeamOperandParameters.Y, 0);
        if (body[allocation + width] is not { Definition.Opcode: GetTupleElement, Operands: [_, _, var first] }
            || first != local || !IsMove(body[allocation + width + 1], local, new(BeamOperandParameters.X, 0))
            || SkipComparedFieldLines(body, allocation + width + 2, name) != name - 1)
        {
            return null;
        }

        int arm = allocation - 1;
        while (arm > entry && body[arm].Definition.Opcode != Label)
        {
            arm--;
        }

        if (arm <= entry + 2 || !flow.TryGetSingleForwardTarget(
            entry + 2,
            (int)body[arm].Operands[0].Value,
            out int target
        ) || target != arm)
        {
            return null;
        }

        var order = new List<(BeamOperand Source, int Slot)>();
        var cons = new List<(BeamOperand List, BeamOperand Head, BeamOperand Tail)>();
        (int Index, BeamOperand Tuple)? projectedField = null;
        int earlierCons = -1;
        int laterCons = -1;
        for (int index = allocation - 1; index > arm; index--)
        {
            var instruction = body[index];
            switch (instruction)
            {
                case { Definition.Opcode: GetList, Operands: [var list, var head, var tail] }
                    when IsBareX(list) && IsBareX(head) && tail == list && head != tail:
                    if (cons.Count == 0)
                    {
                        laterCons = index;
                    }
                    else
                    {
                        earlierCons = index;
                    }

                    cons.Add((list, head, tail));
                    Take(head);
                    Take(tail);
                    break;

                case { Definition.Opcode: GetTail, Operands: [var input, var rest] }
                    when IsBareX(input) && input == rest:
                    Take(rest);
                    break;

                case { Definition.Opcode: GetTupleElement, Operands: [var tuple, _, var result] }
                    when IsBareX(tuple) && IsBareX(result):
                    if (Take(result))
                    {
                        if (projectedField is not null)
                        {
                            return null;
                        }

                        projectedField = (index, tuple);
                    }

                    break;

                case { Definition.Opcode: IsTuple or TestArity or IsNonemptyList or IsTaggedTuple or ExactEqualTest }:
                    break;

                default:
                    return null;
            }
        }

        if (cons is not [var parent, var child] || parent.List != child.List
            || projectedField is not { } field || field.Tuple != child.Head
            || field.Index <= earlierCons || field.Index >= laterCons
            || pending.Count != 1)
        {
            return null;
        }

        BeamOperand receiver = pending.Single().Key;
        if (receiver.Value >= arguments.Value || !body.Skip(arm + 1).Take(allocation - arm - 1).Any(instruction => instruction is
            { Definition.Opcode: IsTaggedTuple, Operands: [_, var value, _, _] } && value == receiver))
        {
            return null;
        }

        Take(receiver);
        var slots = new int[width];
        slots[0] = 1;
        for (int index = 0; index < order.Count; index++)
        {
            slots[order[index].Slot] = index + 2;
        }

        // Follow Core's nested binder order from the matched heads and
        // tails. The late frame reverses their SSA variable allocation order.
        return new(slots, order.Select(capture => capture.Source).ToArray());

        bool Take(BeamOperand register)
        {
            if (!pending.Remove(register, out int slot))
            {
                return false;
            }

            order.Add((register, slot));

            return true;
        }
    }
}
