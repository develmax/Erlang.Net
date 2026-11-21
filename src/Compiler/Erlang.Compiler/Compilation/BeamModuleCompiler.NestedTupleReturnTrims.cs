using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? TrimNestedTupleCallReturns(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(InitYRegisters)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var sites = new HashSet<int>();
        for (int index = 0; index + 6 < body.Count; index++)
        {
            var initialization = body[index];
            if (initialization is not
                {
                    Definition.Opcode: InitYRegisters,
                    Operands: [_, { Kind: BeamOperandParameters.Y }]
                })
            {
                continue;
            }

            int call = index + 1;
            while (call < body.Count && body[call].Definition.Opcode is Move or Line)
            {
                call++;
            }

            if (call + 5 >= body.Count || body[call].Definition.Opcode is not (LocalCall or ExternalCall)
                || body[call + 1].Definition.Opcode != TestHeap
                || body[call + 2] is not
                {
                    Definition.Opcode: PutTuple,
                    Operands: [{ Kind: BeamOperandParameters.X } inner, _, ..]
                } constructor
                || body[call + 3].Definition.Opcode != PutTuple
                || body[call + 4].Definition.Opcode != Deallocate
                || body[call + 5].Definition.Opcode != Return
                || !constructor.Operands.Skip(2).Any(operand => operand.Kind == BeamOperandParameters.Y && operand.Value == 0)
                || !body[call + 3].Operands.Skip(2).Any(operand => SameProjectionRegister(operand, inner)))
            {
                continue;
            }

            sites.Add(initialization.Offset);
        }

        // The late call schedule can expose an existing kill between the last
        // outgoing load and the final ordinary call. Use the same suffix
        // liveness and trim cost comparison as the initial frame pass.
        return sites.Count == 0 ? null : TrimLinearStackFrames(code, labelCount, sites);
    }
}
