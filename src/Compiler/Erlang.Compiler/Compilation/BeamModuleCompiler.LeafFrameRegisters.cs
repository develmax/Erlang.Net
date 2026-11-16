using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ReallocateLeafFrameRegisters(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        List<(int Start, int End, int Slot)> stackOperands
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int operand = 0;
        int start = 0;
        int arity = 0;
        for (int end = 0; end < body.Count; end++)
        {
            BeamInstruction boundary = body[end];
            if (boundary.Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int?[]? registers = PlanLeafFrameRegisters(
                body,
                start,
                end,
                arity
            );
            if (registers is not null)
            {
                for (int index = start; index < end; index++)
                {
                    BeamInstruction instruction = body[index];
                    int next = body[index + 1].Offset;
                    while (operand < stackOperands.Count && stackOperands[operand].Start < instruction.Offset)
                    {
                        operand++;
                    }

                    if (instruction.Definition.Opcode is Allocate or Deallocate or InitYRegisters)
                    {
                        output.Write(code.Span[copied..instruction.Offset]);
                        copied = next;
                        continue;
                    }

                    while (operand < stackOperands.Count && stackOperands[operand].Start < next)
                    {
                        var (from, to, slot) = stackOperands[operand++];
                        output.Write(code.Span[copied..from]);
                        BeamCompactWriter.Write(output, XRegisterTag, registers[slot]!.Value);
                        copied = to;
                    }
                }
            }

            start = end + 1;
            if (boundary.Definition.Opcode == FunctionInfo)
            {
                arity = (int)boundary.Operands[2].Value;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static int?[]? PlanLeafFrameRegisters(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int end,
        int arity
    )
    {
        const int xCount = BeamOperandParameters.MaximumRegister;
        if (end - start < 2 || body[start].Definition.Opcode != Label
            || body[start + 1].Definition.Opcode != Allocate)
        {
            return null;
        }

        int slots = (int)body[start + 1].Operands[0].Value;
        if (slots <= 0 || slots >= xCount)
        {
            return null;
        }

        int count = end - start;
        var labels = new Dictionary<int, int>();
        var uses = new BigInteger[count];
        var writes = new int[count][];
        var movedFrom = new int[count];
        var slotsUsed = new HashSet<int>();
        for (int index = 0; index < count; index++)
        {
            BeamInstruction instruction = body[start + index];
            byte opcode = instruction.Definition.Opcode;
            if (!IsFramelessInstruction(instruction) || (opcode is Allocate && index != 1)
                || opcode is AllocateHeap or TestHeap or PutTuple or PutList or GetMapElements
                    or BifOne or BifTwo or BifThree or GcBifOne or GcBifTwo or GcBifThree
                || (opcode == Deallocate && instruction.Operands[0].Value != slots))
            {
                return null;
            }

            if (instruction is { Definition.Opcode: Label, Operands: [var label] })
            {
                labels.Add((int)label.Value, index);
            }

            var destinations = new List<int>(2);
            uses[index] = opcode == Return ? BigInteger.One : BigInteger.Zero;
            movedFrom[index] = -1;
            for (int position = 0; position < instruction.Operands.Count && opcode != InitYRegisters; position++)
            {
                BeamOperand operand = instruction.Operands[position];
                int register = LeafRegisterIndex(operand);
                if (register < 0)
                {
                    continue;
                }

                if (register >= xCount)
                {
                    int slot = register - xCount;
                    if (slot >= slots)
                    {
                        return null;
                    }

                    slotsUsed.Add(slot);
                }

                if (opcode == Swap || opcode == GetList && position > 0
                    || opcode is (Move or GetHead or GetTail or GetTupleElement) && position == instruction.Operands.Count - 1)
                {
                    destinations.Add(register);
                    if (opcode != Swap)
                    {
                        continue;
                    }
                }

                uses[index] |= BigInteger.One << register;
                if (opcode == Move)
                {
                    movedFrom[index] = register;
                }
            }

            writes[index] = destinations.ToArray();
        }

        var successors = new int[count][];
        for (int index = 0; index < count; index++)
        {
            BeamInstruction instruction = body[start + index];
            var targets = new List<int>();
            if (index + 1 < count && !EndsBeamBlock(instruction.Definition.Opcode))
            {
                targets.Add(index + 1);
            }

            foreach (var operand in instruction.Operands)
            {
                if (operand.Kind == BeamOperandParameters.Label)
                {
                    targets.Add(labels.GetValueOrDefault((int)operand.Value, -1));
                }
            }

            successors[index] = targets.ToArray();
        }

        var liveIn = new BigInteger[count];
        var liveOut = new BigInteger[count];
        BigInteger arguments = (BigInteger.One << arity) - 1;
        bool changed;
        do
        {
            changed = false;
            for (int index = count - 1; index >= 0; index--)
            {
                BigInteger after = BigInteger.Zero;
                foreach (int next in successors[index])
                {
                    // A branch back to func_info uses the original arguments for function_clause.
                    after |= next < 0 ? arguments : liveIn[next];
                }

                liveOut[index] = after;
                foreach (int destination in writes[index])
                {
                    after &= ~(BigInteger.One << destination);
                }

                BigInteger before = uses[index] | after;
                changed |= before != liveIn[index];
                liveIn[index] = before;
            }
        }
        while (changed);

        var conflicts = new BigInteger[xCount + slots];
        var preferred = new List<int>[slots];
        for (int index = 0; index < count; index++)
        {
            if (body[start + index].Definition.Opcode == InitYRegisters
                && body[start + index].Operands.Skip(1).Any(slot => LeafRegisterIsLive(liveOut[index], LeafRegisterIndex(slot))))
            {
                return null;
            }

            int source = movedFrom[index];
            foreach (int destination in writes[index])
            {
                BigInteger neighbors = liveOut[index];
                if (source >= 0)
                {
                    // A copy can share its source register when no other definition interferes.
                    neighbors &= ~(BigInteger.One << source);
                    if (source < xCount && destination >= xCount)
                    {
                        (preferred[destination - xCount] ??= []).Add(source);
                    }
                    else if (source >= xCount && destination < xCount)
                    {
                        (preferred[source - xCount] ??= []).Add(destination);
                    }
                }

                foreach (int other in writes[index])
                {
                    neighbors |= BigInteger.One << other;
                }

                conflicts[destination] |= neighbors;
            }
        }

        var registers = new int?[slots];
        foreach (int slot in slotsUsed.Order())
        {
            int reg = xCount + slot;
            int candidate = (preferred[slot] ?? []).Concat(Enumerable.Range(0, xCount))
                .FirstOrDefault(CanUse, -1);
            if (candidate < 0)
            {
                return null;
            }

            registers[slot] = candidate;

            bool CanUse(int x) => !Interferes(x) && !slotsUsed.Any(other =>
                registers[other] == x && Interferes(xCount + other));

            bool Interferes(int other) => LeafRegisterIsLive(conflicts[reg], other)
                || LeafRegisterIsLive(conflicts[other], reg);
        }

        return registers;
    }

    private static int LeafRegisterIndex(BeamOperand operand) => operand.Kind switch
    {
        BeamOperandParameters.X => (int)operand.Value,
        BeamOperandParameters.Y => BeamOperandParameters.MaximumRegister + (int)operand.Value,
        _ => -1
    };

    private static bool LeafRegisterIsLive(BigInteger live, int register) =>
        !(live & (BigInteger.One << register)).IsZero;
}
