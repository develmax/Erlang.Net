using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ScheduleDeclaredNativeFrameEntry(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(NifStart)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        var argument = new BeamOperand(BeamOperandParameters.X, 0);
        var saved = new BeamOperand(BeamOperandParameters.Y, 0);
        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 2; index + 3 < body.Count; index++)
        {
            if (body[index].Definition.Opcode != NifStart
                || body[index - 1].Definition.Opcode != Label
                || body[index - 2] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, { Value.IsOne: true }] }
                || body[index + 1] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [{ Value.IsOne: true }, { Value.IsOne: true }]
                }
                || body[index + 2] is not { Definition.Opcode: Move, Operands: [var source, var destination] }
                || source != argument || destination != saved)
            {
                continue;
            }

            // The pre-codegen frame allocation and entry copy precede the
            // first Core instruction. The native marker still owns this entry.
            int marker = body[index].Offset;
            int allocation = body[index + 1].Offset;
            int next = body[index + 3].Offset;
            output.Write(code.Span[copied..marker]);
            output.Write(code.Span[allocation..next]);
            output.Write(code.Span[marker..allocation]);
            copied = next;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
