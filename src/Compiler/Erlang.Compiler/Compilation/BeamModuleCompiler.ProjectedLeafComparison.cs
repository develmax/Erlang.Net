using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? RemoveProjectedLeafComparisonFrame(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlang))
        {
            return null;
        }

        int equal = FindImport(ErlangOperators.ExactEqual);
        int unequal = FindImport(ErlangOperators.ExactNotEqual);
        int element = FindImport(ErlangModuleNames.Element);
        if (equal < 0 && unequal < 0 || element < 0 && !hasTupleProjections
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        return RewriteProjectedLeafComparison(
            code,
            decoded.Body,
            element,
            equal,
            unequal
        );

        int FindImport(string name) => atoms.TryGetValue(name, out int atom)
            && imports.TryGetValue((erlang, atom, 2), out int index)
                ? emittedImports?[index] ?? index : -1;
    }

    private static byte[]? RewriteProjectedLeafComparison(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int element,
        int equal,
        int unequal
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int nextEntry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = nextEntry;
            nextEntry = end + 1;
            if (entry >= end || body[entry].Definition.Opcode != Label)
            {
                continue;
            }

            int frame = entry + 1;
            while (frame < end && IsEntryPrefix(body[frame]))
            {
                frame++;
            }

            if (frame >= end || body[frame] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [{ Value.IsOne: true }, _]
                })
            {
                continue;
            }

            int first = frame + 1;
            if (first < end && body[first] is
                {
                    Definition.Opcode: InitYRegisters,
                    Operands: [_, { Kind: BeamOperandParameters.Y, Value.IsZero: true }]
                })
            {
                first++;
            }

            bool hasProjection = false;
            int relay = first;
            for (; relay < end; relay++)
            {
                BeamInstruction instruction = body[relay];
                if (instruction.Definition.Opcode == Line)
                {
                    continue;
                }

                if (!IsTupleField(instruction) && !IsIntegralElement(instruction, element))
                {
                    break;
                }

                hasProjection = true;
            }

            if (!hasProjection || relay + 3 >= end || body[relay] is not
                {
                    Definition.Opcode: Move,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } projected,
                    { Kind: BeamOperandParameters.Y, Value.IsZero: true, TypeIndex: null } slot
                    ]
                }
                || body[relay + 1] is not
                {
                    Definition.Opcode: BifTwo,
                    Operands:
                    [
                    { Kind: BeamOperandParameters.Failure },
                    { Kind: BeamOperandParameters.Unsigned } import,
                        var operand,
                    {
                        Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                            or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue
                    },
                    { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }
                    ]
                }
                || operand != slot || import.Value != equal && import.Value != unequal
                || body[relay + 2] is not { Definition.Opcode: Deallocate, Operands: [{ Value.IsOne: true }] }
                || body[relay + 3].Definition.Opcode != Return
                || Enumerable.Range(relay + 4, end - relay - 4).Any(index => body[index] is not
                    ({ Definition.Opcode: Label or Line }
                    or { Definition.Opcode: BadRecord, Operands: [{ Kind: BeamOperandParameters.X }] })))
            {
                continue;
            }

            // There is no call or GC in the frame. The last move only saves
            // the projection for this comparison, which can consume its X value.
            output.Write(code.Span[copied..body[frame].Offset]);
            output.Write(code.Span[body[first].Offset..body[relay].Offset]);
            int comparison = body[relay + 1].Offset;
            var cursor = new BeamCursor(code[(comparison + 1)..]);
            _ = BeamCompactReader.Read(cursor);
            _ = BeamCompactReader.Read(cursor);
            int operandStart = comparison + 1 + cursor.Position;
            _ = BeamCompactReader.Read(cursor);
            output.Write(code.Span[comparison..operandStart]);
            WriteRegister(output, projected);
            output.Write(code.Span[(comparison + 1 + cursor.Position)..body[relay + 2].Offset]);
            copied = body[relay + 3].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static bool IsEntryPrefix(BeamInstruction instruction) =>
            instruction.Definition.Opcode == Line || IsTupleField(instruction)
            || IsEntryFramePredicate(instruction.Definition.Opcode)
                && instruction.Operands.All(operand => operand.Kind != BeamOperandParameters.Y);

        static bool IsTupleField(BeamInstruction instruction) => instruction is
        {
            Definition.Opcode: GetTupleElement,
            Operands: [{ Kind: BeamOperandParameters.X, TypeIndex: null }, _,
            { Kind: BeamOperandParameters.X, TypeIndex: null }]
        };

        static bool IsIntegralElement(BeamInstruction instruction, int target) => instruction is
        {
            Definition.Opcode: BifTwo,
            Operands:
            [
            { Kind: BeamOperandParameters.Failure },
            { Kind: BeamOperandParameters.Unsigned } import,
            { Kind: BeamOperandParameters.Integer, Value.Sign: > 0 },
            { Kind: BeamOperandParameters.X, TypeIndex: null },
            { Kind: BeamOperandParameters.X, TypeIndex: null }
            ]
        } && import.Value == target;
    }
}
