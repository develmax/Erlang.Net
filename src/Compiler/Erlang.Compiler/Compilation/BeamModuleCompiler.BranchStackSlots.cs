using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int Count, int[] Slots)? PlanBranchStackSlots(IReadOnlyList<BeamInstruction> body, int entry, int end)
    {
        if (entry + 1 >= end || body[entry].Definition.Opcode != Label
            || body[entry + 1].Definition.Opcode is not (Allocate or AllocateHeap))
        {
            return null;
        }

        int frame = (int)body[entry + 1].Operands[0].Value;
        if (frame < 2 || frame > BeamOperandParameters.MaximumRegister)
        {
            return null;
        }

        int start = entry + 2;
        var labels = new Dictionary<int, int>();
        var firstUse = new int[frame];
        Array.Fill(firstUse, int.MaxValue);
        for (int index = start; index < end; index++)
        {
            var instruction = body[index];
            if (instruction.Definition.Opcode == Label)
            {
                labels.Add((int)instruction.Operands[0].Value, index);
            }

            if (instruction.Definition.Opcode == InitYRegisters)
            {
                continue;
            }

            foreach (BeamOperand operand in instruction.Operands)
            {
                if (operand.Kind == BeamOperandParameters.Y && operand.Value < frame)
                {
                    int slot = (int)operand.Value;
                    firstUse[slot] = Math.Min(firstUse[slot], index);
                }
            }
        }

        int infoLabel = entry - 2;
        while (infoLabel >= 0 && body[infoLabel].Definition.Opcode == Line)
        {
            infoLabel--;
        }

        int clauseFailure = infoLabel >= 0 && body[infoLabel].Definition.Opcode == Label
            ? (int)body[infoLabel].Operands[0].Value : -1;
        BigInteger[] masks = Enumerable.Range(0, frame).Select(slot => BigInteger.One << slot).ToArray();
        var live = new BigInteger[end - start + 1];
        var conflicts = new BigInteger[frame];
        bool branched = false;
        bool released = false;
        for (int index = end - 1; index >= start; index--)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode is Allocate or AllocateHeap or TrimFrame or CatchEnter or TryEnter
                or RemoveMessage or ReceiveTimeout or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout
                || instruction.Definition.Name == "debug_line")
            {
                return null;
            }

            int frameOperand = LinearFrameSizeOperand(opcode);
            if (frameOperand >= 0)
            {
                if (instruction.Operands[frameOperand].Value != frame)
                {
                    return null;
                }

                released = true;
            }

            BigInteger next = EndsBeamBlock(opcode) || opcode == IfEnd ? BigInteger.Zero : live[index - start + 1];
            BigInteger onBranch = BigInteger.Zero;
            bool call = opcode is LocalCall or TailLocalCall or TailLocalCallWithFrame;
            bool initializes = opcode == InitYRegisters;
            int destination = StackWriteOperand(instruction);
            BigInteger reads = BigInteger.Zero;
            BigInteger writes = BigInteger.Zero;
            for (int position = 0; position < instruction.Operands.Count; position++)
            {
                BeamOperand operand = instruction.Operands[position];
                if (!call && operand.Kind == BeamOperandParameters.Label && operand.Value != clauseFailure)
                {
                    if (!labels.TryGetValue((int)operand.Value, out int target) || target <= index)
                    {
                        return null;
                    }

                    onBranch |= live[target - start];
                    branched = true;
                }

                if (operand.Kind != BeamOperandParameters.Y)
                {
                    continue;
                }

                if (operand.Value >= frame || frameOperand >= 0
                    || !initializes && destination == int.MinValue
                        && opcode is not (SelectValue or SelectTupleArity) && !IsEntryFramePredicate(opcode))
                {
                    return null;
                }

                int slot = (int)operand.Value;
                if (initializes && firstUse[slot] == int.MaxValue)
                {
                    continue;
                }

                if (initializes || position == destination || opcode == GetList && position == 2)
                {
                    writes |= masks[slot];
                }
                else
                {
                    reads |= masks[slot];
                }
            }

            // A failing instruction does not define its destination on the branch edge.
            BigInteger before = reads | (next & ~writes) | onBranch;
            live[index - start] = before;
            AddStackInterference(before, before, conflicts);
            if (!writes.IsZero)
            {
                BigInteger occupied = next | reads | (initializes ? BigInteger.Zero : writes);
                AddStackInterference(writes, occupied, conflicts);
                AddStackInterference(occupied, writes, conflicts);
            }
        }

        if (!branched || !released)
        {
            return null;
        }

        var groups = new List<BigInteger>();
        var slots = new int[frame];
        Array.Fill(slots, -1);
        foreach (int slot in Enumerable.Range(0, frame).Where(slot => firstUse[slot] != int.MaxValue).OrderBy(slot => firstUse[slot]))
        {
            int register = groups.FindIndex(group => (group & conflicts[slot]).IsZero);
            if (register >= 0)
            {
                groups[register] |= masks[slot];
            }
            else
            {
                register = groups.Count;
                groups.Add(masks[slot]);
            }

            slots[slot] = register;
        }

        if (groups.Count == 0 || groups.Count >= frame)
        {
            return null;
        }

        for (int slot = 0; slot < frame; slot++)
        {
            if (slots[slot] >= 0)
            {
                slots[slot] = groups.Count - slots[slot] - 1;
            }
        }

        return (groups.Count, slots);
    }

    private static void AddStackInterference(BigInteger slots, BigInteger other, BigInteger[] conflicts)
    {
        while (!slots.IsZero)
        {
            int slot = (int)BigInteger.TrailingZeroCount(slots);
            conflicts[slot] |= other;
            slots &= slots - BigInteger.One;
        }
    }
}
