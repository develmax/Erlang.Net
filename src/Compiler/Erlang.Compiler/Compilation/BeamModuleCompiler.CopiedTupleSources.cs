using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? PreferCopiedTupleSourcesInX(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || (!code.Span.Contains(GetTupleElement) && !code.Span.Contains(PutList)))
        {
            return null;
        }

        var stackOperands = new List<(int Start, int End)>();
        IReadOnlyList<BeamInstruction> body;
        try
        {
            var header = new BeamCodeHeader(
                BeamWire.FormatVersion,
                highestOpcode,
                labelCount,
                exports.Count + locals.Count,
                code
            );
            body = BeamInstructionReader.Read(
                header,
                atoms.Count + 1,
                literalBytes.Count,
                recordStackOperand: (start, end, _) => stackOperands.Add((start, end))
            );
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        int position = 0;
        for (int index = 0; index + 1 < body.Count; index++)
        {
            if (body[index] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } live,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } saved
                    ]
                })
            {
                continue;
            }

            BeamInstruction instruction = body[index + 1];
            if (live.Value.IsZero && index > 0 && index + 3 < body.Count
                && body[index - 1].Definition.Opcode is (LocalCall or ExternalCall)
                && instruction is
                {
                    Definition.Opcode: IsTaggedTuple,
                    Operands: [
                    { Kind: BeamOperandParameters.Label } failure,
                        var tested,
                    { Kind: BeamOperandParameters.Unsigned } arity,
                    { Kind: BeamOperandParameters.Atom }
                    ]
                }
                && failure.Value.Sign > 0 && tested == saved
                && body[index + 2] is
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [
                        var projected,
                    { Kind: BeamOperandParameters.Unsigned } field,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } destination
                    ]
                }
                && projected == saved && field.Value < arity.Value
                && body[index + 3] is
                {
                    Definition.Opcode: ExactEqualTest or ExactNotEqualTest,
                    Operands: [
                    { Kind: BeamOperandParameters.Label } comparisonFailure,
                        var compared,
                    {
                        Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                                or BeamOperandParameters.LiteralValue
                    }
                    ]
                }
                && comparisonFailure.Value.Sign > 0 && compared == destination)
            {
                // The tag test preserves the call result on both edges.
                // The projection writes the same field before the next test reads it.
                RewriteCopiedInputs(
                    instruction,
                    2,
                    saved,
                    live
                );
                RewriteCopiedInputs(
                    body[index + 2],
                    1,
                    saved,
                    live
                );

                continue;
            }

            int inputs = instruction switch
            {
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [_, { Kind: BeamOperandParameters.Unsigned }, { Kind: BeamOperandParameters.X, TypeIndex: null }]
                } => 1,
                {
                    Definition.Opcode: PutList,
                    Operands: [_, _, { Kind: BeamOperandParameters.X, TypeIndex: null }]
                } when HasInitializedListCopySource(body, index, live) => 2,
                _ => 0
            };
            if (inputs == 0)
            {
                continue;
            }

            RewriteCopiedInputs(
                instruction,
                inputs,
                saved,
                live
            );
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void RewriteCopiedInputs(
            BeamInstruction instruction,
            int inputs,
            BeamOperand saved,
            BeamOperand live
        )
        {
            while (position < stackOperands.Count && stackOperands[position].Start < instruction.Offset)
            {
                position++;
            }

            // The decoder supplies exact offsets even when another operand is typed or literal.
            foreach (BeamOperand source in instruction.Operands.Take(inputs))
            {
                if (source.Kind != BeamOperandParameters.Y)
                {
                    continue;
                }

                var (start, end) = stackOperands[position++];
                if (source == saved)
                {
                    output.Write(code.Span[copied..start]);
                    BeamCompactWriter.Write(output, XRegisterTag, live.Value);
                    copied = end;
                }
            }
        }
    }

    private static bool HasInitializedListCopySource(
        IReadOnlyList<BeamInstruction> body,
        int copy,
        BeamOperand source
    )
    {
        int allocation = copy - 1;
        while (allocation >= 0 && body[allocation].Definition.Opcode == InitYRegisters)
        {
            allocation--;
        }

        return allocation >= 0 && body[allocation] is
        {
            Definition.Opcode: AllocateHeap,
            Operands: [_, _, var live]
        } && source.Value < live.Value;
    }
}
