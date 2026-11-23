using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static EntryFramePlacement? PlanRepeatedEntryListTails(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int allocation,
        int end
    )
    {
        if (start < 1 || allocation != start + 1
            || body[start - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, var arity] }
            || body[allocation] is not { Definition.Opcode: Allocate, Operands: [{ Value.Sign: > 0 } width, var live] }
            || live.Value != arity.Value || live.Value >= BeamOperandParameters.MaximumRegister - 1)
        {
            return null;
        }

        var saved = new HashSet<int>();
        int first = allocation + 1;
        for (; first < end; first++)
        {
            if (body[first] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.X } input, { Kind: BeamOperandParameters.Y } destination]
                }
                && input.Value < arity.Value && destination.Value < width.Value)
            {
                saved.Add((int)destination.Value);
            }
            else if (body[first] is { Definition.Opcode: InitYRegisters } initialize)
            {
                foreach (BeamOperand initialized in initialize.Operands.Skip(1))
                {
                    saved.Remove((int)initialized.Value);
                }
            }
            else
            {
                break;
            }
        }

        if (first + 1 >= end || body[first] is not
            {
                Definition.Opcode: IsNonemptyList,
                Operands: [{ Kind: BeamOperandParameters.Label } failure,
                { Kind: BeamOperandParameters.X, TypeIndex: null } original]
            }
            || original.Value >= arity.Value)
        {
            return null;
        }

        if (body[first + 1].Definition.Opcode == GetList)
        {
            return PlanWholeEntryConsMatch(
                body,
                start,
                end,
                first,
                (int)width.Value,
                (int)live.Value,
                saved,
                failure,
                original
            );
        }

        if (body[first + 1] is not
            {
                Definition.Opcode: GetTail,
                Operands: [_, { Kind: BeamOperandParameters.Y, TypeIndex: null } slot]
            }
            || slot.Value >= width.Value || saved.Contains((int)slot.Value))
        {
            return null;
        }

        var delayed = new HashSet<int>();
        BeamOperand source = original;
        int next = first;
        while (next + 1 < end && body[next] is
            { Definition.Opcode: IsNonemptyList, Operands: [var rejected, var checkedList] }
            && rejected == failure && checkedList == source && body[next + 1] is
            { Definition.Opcode: GetTail, Operands: [var extracted, var target] }
            && extracted == source && target == slot)
        {
            delayed.Add(next++);
            delayed.Add(next++);
            source = slot;
        }

        if (delayed.Count < 4 || next + 1 >= end
            || body[next] is not { Definition.Opcode: Jump, Operands: [var success] }
            || success.Value == failure.Value
            || body[next + 1] is not { Definition.Opcode: Label, Operands: [var cold] }
            || cold.Value != failure.Value)
        {
            return null;
        }

        // The shared error reports the original list even after one or more successful extractions.
        int terminal = ReadEntryListBadMatch(
            body,
            next + 1,
            end,
            original
        );
        int frame = terminal + 2;
        if (terminal < 0 || frame >= end
            || body[terminal + 1] is not { Definition.Opcode: Label, Operands: [var resume] }
            || resume.Value != success.Value)
        {
            return null;
        }

        for (int index = start; index < end; index++)
        {
            if (body[index].Definition.Opcode == Label)
            {
                continue;
            }

            foreach (BeamOperand operand in body[index].Operands)
            {
                if (operand.Kind == BeamOperandParameters.Label
                    && (operand.Value == failure.Value && !delayed.Contains(index)
                        || operand.Value == success.Value && index != next))
                {
                    return null;
                }
            }
        }

        int temporary = (int)arity.Value;
        int destinationSlot = (int)slot.Value;

        return new(
            first,
            new Dictionary<int, int> { [destinationSlot] = temporary },
            delayed,
            new Dictionary<int, int> { [frame] = temporary + 1 },
            new Dictionary<int, int>(),
            new Dictionary<int, (int Slot, int Register)[]> { [frame] = [(destinationSlot, temporary)] }
        );
    }

    private static EntryFramePlacement? PlanWholeEntryConsMatch(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int first,
        int width,
        int live,
        HashSet<int> saved,
        BeamOperand failure,
        BeamOperand original
    )
    {
        var registers = new Dictionary<int, int>();
        var delayed = new HashSet<int>();
        int wholeCons = 0;
        int next = first;
        while (next < end && body[next] is { Operands: [var rejected, var tested] }
            && rejected == failure && ReadsListValue(tested))
        {
            if (body[next].Definition.Opcode == IsNil)
            {
                delayed.Add(next++);
                continue;
            }

            if (body[next].Definition.Opcode != IsNonemptyList || next + 1 >= end)
            {
                break;
            }

            BeamInstruction extract = body[next + 1];
            if (extract is { Definition.Opcode: GetList, Operands: [var list, var head, var tail] }
                && list == tested && head != tail && Store(head) && Store(tail))
            {
                wholeCons++;
            }
            else if (extract is not { Definition.Opcode: GetHead or GetTail, Operands: [var source, var value] }
                || source != tested || !Store(value))
            {
                return null;
            }

            delayed.Add(next++);
            delayed.Add(next++);
        }

        if (wholeCons < 2 || live + registers.Count >= BeamOperandParameters.MaximumRegister || next >= end)
        {
            return null;
        }

        int cold = next + 1;
        while (cold < end && !(body[cold] is { Definition.Opcode: Label, Operands: [var coldLabel] }
            && coldLabel.Value == failure.Value))
        {
            cold++;
        }

        if (cold >= end || !EndsBeamBlock(body[cold - 1].Definition.Opcode))
        {
            return null;
        }

        int terminal = ReadEntryListBadMatch(
            body,
            cold,
            end,
            original
        );
        if (terminal < 0)
        {
            return null;
        }

        int frame = next;
        BeamOperand? success = null;
        if (body[next] is { Definition.Opcode: Jump, Operands: [var resumed] })
        {
            if (cold != next + 1 || terminal + 2 >= end || resumed.Value == failure.Value
                || body[terminal + 1] is not { Definition.Opcode: Label, Operands: [var resumedLabel] }
                || resumed.Value != resumedLabel.Value)
            {
                return null;
            }

            frame = terminal + 2;
            success = resumed;
        }
        else if (body[next].Definition.Opcode == Label)
        {
            return null;
        }

        for (int index = start; index < end; index++)
        {
            if (body[index].Definition.Opcode == Label)
            {
                continue;
            }

            foreach (BeamOperand operand in body[index].Operands)
            {
                if (operand.Kind == BeamOperandParameters.Label
                    && (operand.Value == failure.Value && !delayed.Contains(index)
                        || success is not null && operand.Value == success.Value && index != next))
                {
                    return null;
                }
            }
        }

        return new(
            first,
            registers,
            delayed,
            new Dictionary<int, int> { [frame] = live + registers.Count },
            new Dictionary<int, int>(),
            new Dictionary<int, (int Slot, int Register)[]>
            {
                [frame] = registers.Select(binding => (binding.Key, binding.Value)).ToArray()
            }
        );

        bool ReadsListValue(BeamOperand value) => value == original
            || value is { Kind: BeamOperandParameters.Y, TypeIndex: null } && registers.ContainsKey((int)value.Value);

        bool Store(BeamOperand register)
        {
            if (register is not { Kind: BeamOperandParameters.Y, TypeIndex: null }
                || register.Value >= width || saved.Contains((int)register.Value))
            {
                return false;
            }

            registers.TryAdd((int)register.Value, live + registers.Count);

            return true;
        }
    }

    private static int ReadEntryListBadMatch(
        IReadOnlyList<BeamInstruction> body,
        int failure,
        int end,
        BeamOperand original
    )
    {
        int terminal = SkipLines(failure + 1);
        BeamOperand badValue = original;
        if (terminal < end && body[terminal] is
            {
                Definition.Opcode: Move,
                Operands: [var inputList,
                { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null } argument]
            } && inputList == original)
        {
            badValue = argument;
            terminal = SkipLines(terminal + 1);
        }

        return terminal < end && body[terminal] is { Definition.Opcode: BadMatch, Operands: [var actual] }
            && actual == badValue ? terminal : -1;

        int SkipLines(int index)
        {
            while (index < end && body[index].Definition.Opcode == Line)
            {
                index++;
            }

            return index;
        }
    }
}
