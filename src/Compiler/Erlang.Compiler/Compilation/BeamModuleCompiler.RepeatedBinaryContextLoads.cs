using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveRepeatedBinaryContextLoads(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(Move)
            || !(code.Span.Contains(GetBinaryInteger) || code.Span.Contains(GetBinarySegment) || code.Span.Contains(GetBinaryFloat))
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        using var output = new MemoryStream();
        int copied = 0;
        int end = 0;
        bool protectedFunction = false;
        for (int index = 0; index + 3 < body.Count; index++)
        {
            if (body[index].Definition.Opcode == FunctionInfo)
            {
                protectedFunction = false;
                end = index + 1;
                while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
                {
                    protectedFunction |= body[end].Definition.Opcode is TryEnter or CatchEnter
                        or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout or DebugLine or ExecutableLine;
                    end++;
                }
            }

            if (protectedFunction || index + 2 >= end
                || body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } context
                    ]
                }
                || body[index + 1] is not
                {
                    Definition.Opcode: GetBinaryInteger or GetBinarySegment or GetBinaryFloat,
                    Operands: [_, var source, var live, _, _, _, var result]
                }
                || source != context || live.Value <= context.Value
                || SameProjectionRegister(result, context) || SameProjectionRegister(result, saved)
                || body[index + 2] is not
                { Definition.Opcode: Move, Operands: [var repeated, var destination] }
                || repeated != saved || destination != context)
            {
                continue;
            }

            // Extracting a field advances the shared match state. The X copy
            // remains rooted and neither copy is the extraction's destination.
            output.Write(code.Span[copied..body[index + 2].Offset]);
            copied = body[index + 3].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
