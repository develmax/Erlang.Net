using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ProjectAliasedTupleGuardElements(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(BifTwo)
            || !atoms.TryGetValue(ErlangModuleNames.Module, out int erlang)
            || !atoms.TryGetValue(AutoImportNames.Element, out int element)
            || !atoms.TryGetValue(ErlangOperators.BitwiseOr, out int union)
            || !imports.TryGetValue((erlang, element, 2), out int elementImport)
            || !imports.TryGetValue((erlang, union, 2), out int unionImport)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteAliasedTupleGuardElements(
            code,
            body,
            emittedImports?[elementImport] ?? elementImport,
            emittedImports?[unionImport] ?? unionImport
        );
    }

    private static byte[]? RewriteAliasedTupleGuardElements(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        int elementImport,
        int unionImport
    )
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        int copied = 0;
        for (int start = 0; start + 5 < body.Count; start++)
        {
            if (body[start] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } carrier, var duplicate]
                } || duplicate != x0
                || body[start + 1] is not { Definition.Opcode: IsTuple, Operands: [var rejected, var tested] }
                || tested != duplicate || rejected.Value.IsZero
                || body[start + 2] is not
                {
                    Definition.Opcode: TestArity,
                    Operands: [var wrongArity, var measured, { Value: var width }]
                } || wrongArity.Value != rejected.Value || measured != duplicate || width <= 1)
            {
                continue;
            }

            var reads = new List<(int Index, int Field)>();
            int index = start + 3;
            while (index + 1 < body.Count)
            {
                BeamInstruction instruction = body[index];
                if (instruction is
                    {
                        Definition.Opcode: BifTwo,
                        Operands: [var failed, var import, { Kind: BeamOperandParameters.Integer } field, var tuple, var target]
                    } && import.Value == elementImport && failed.Value == rejected.Value && tuple == carrier
                    && target == x0 && field.Value > 0 && field.Value <= width && field.Value <= int.MaxValue)
                {
                    reads.Add((index, (int)(field.Value - 1)));
                }
                else if (!PreservesCarrier(instruction, carrier, rejected))
                {
                    break;
                }

                index++;
            }

            if (reads.Count < 2)
            {
                continue;
            }

            // The checked X alias may be reassigned, but the immutable Y
            // carrier still holds that exact tuple throughout these reads.
            foreach (var (at, field) in reads)
            {
                output.Write(code.Span[copied..body[at].Offset]);
                output.WriteByte(GetTupleElement);
                BeamCompactWriter.Write(output, YRegisterTag, carrier.Value);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field);
                BeamCompactWriter.Write(output, XRegisterTag, 0);
                copied = body[at + 1].Offset;
            }

            start = index - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool PreservesCarrier(BeamInstruction instruction, BeamOperand carrier, BeamOperand rejected) =>
            instruction is { Definition.Opcode: Line }
            || instruction is
            {
                Definition.Opcode: Move,
                Operands: [var source, { Kind: BeamOperandParameters.Y, TypeIndex: null } target]
            } && source == x0 && target != carrier
            || instruction is
            {
                Definition.Opcode: GcBifTwo,
                Operands: [var failure, _, var import, _, _, var result]
            } && failure.Value == rejected.Value && import.Value == unionImport && result == x0;
    }
}
