using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static (int Start, int Insert, bool Initialize, int[] Existing, BeamOperand[] Reads)? ReadTupleListCallSlotUses(
        IReadOnlyList<BeamInstruction> body,
        int entry,
        int call
    )
    {
        if (body[call] is not { Definition.Opcode: LocalCall or ExternalCall, Operands: [var arity, _] }
            || arity.Value != 1)
        {
            return null;
        }

        int index = call - 1;
        while (index > entry && body[index].Definition.Opcode == Line)
        {
            index--;
        }

        bool initialize = body[index].Definition.Opcode == InitYRegisters;
        int insert = initialize ? index : index + 1;
        if (initialize)
        {
            index--;
        }

        if (body[index].Definition.Opcode != PutList)
        {
            return null;
        }

        var required = new HashSet<int> { 0 };
        var listTails = new HashSet<int> { 0 };
        var reads = new List<BeamOperand>();
        // Walk backwards through the definitions that reach the finished X0
        // argument. A reused register denotes its prior value in the inputs.
        // Only Y leaves reached through a tuple are candidates for clearing.
        while (index > entry)
        {
            BeamInstruction instruction = body[index];
            byte opcode = instruction.Definition.Opcode;
            if (opcode == TestHeap)
            {
                if (reads.Count == 0 || listTails.Count != 0
                    || required.Any(register => register >= instruction.Operands[1].Value))
                {
                    return null;
                }

                int[] existing = initialize
                    ? body[insert].Operands.Skip(1).Select(operand => (int)operand.Value).ToArray()
                    : [];

                return (index, insert, initialize, existing, reads.ToArray());
            }

            if (opcode is not (PutTuple or PutList or BifZero)
                || instruction.Operands[StackWriteOperand(instruction)] is not { Kind: BeamOperandParameters.X } result
                || !required.Remove((int)result.Value)
                || listTails.Remove((int)result.Value) && opcode != PutList)
            {
                return null;
            }

            if (opcode == PutList && instruction.Operands[1] is { Kind: not BeamOperandParameters.Nil } tail)
            {
                if (tail.Kind != BeamOperandParameters.X)
                {
                    return null;
                }

                listTails.Add((int)tail.Value);
            }

            IEnumerable<BeamOperand> inputs = opcode switch
            {
                PutTuple => instruction.Operands.Skip(2),
                PutList => instruction.Operands.Take(2),
                _ => []
            };
            foreach (BeamOperand input in inputs)
            {
                if (input.Kind == BeamOperandParameters.X)
                {
                    required.Add((int)input.Value);
                }
                else if (opcode == PutTuple && input.Kind == BeamOperandParameters.Y)
                {
                    reads.Add(input);
                }
            }

            index--;
        }

        return null;
    }
}
