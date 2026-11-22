using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? FoldUnaryTailListScratch(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutList)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        return RewriteUnaryTailListScratch(code, body);
    }

    private static byte[]? RewriteUnaryTailListScratch(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var edits = new List<CodeEdit>();
        for (int heap = 0; heap + 5 < body.Count; heap++)
        {
            if (body[heap] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsOne: true }] }
                || body[heap + 1] is not
                {
                    Definition.Opcode: PutList,
                    Operands: [var item, { Kind: BeamOperandParameters.Nil }, var singleton]
                }
                || item != x0 || singleton != x0
                || body[heap + 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var list, { Kind: BeamOperandParameters.X, TypeIndex: null } scratch]
                }
                || list != x0 || scratch == x0
                || body[heap + 3] is not { Definition.Opcode: PutList, Operands: [var head, var suffix, var combined] }
                || head != scratch || !IsNonRegisterValue(suffix) || combined != x0)
            {
                continue;
            }

            int tail = heap + 4;
            while (tail < body.Count && body[tail] is
                {
                    Definition.Opcode: PutList,
                    Operands: [var prefix, var current, var result]
                } && IsNonRegisterValue(prefix) && current == x0 && result == x0)
            {
                tail++;
            }

            if (tail == heap + 4 || tail >= body.Count || body[tail] is not
                {
                    Definition.Opcode: TailLocalCallWithFrame or TailExternalCallWithFrame,
                    Operands: [{ Value.IsOne: true }, _, { Value.IsZero: true }]
                })
            {
                continue;
            }

            // Within this heap reservation the next cons reads the singleton
            // before overwriting X0. The later prefix and unary call use only X0.
            int use = body[heap + 3].Offset;
            var cursor = new BeamCursor(code[(use + 1)..]);
            _ = BeamCompactReader.Read(cursor);
            edits.Add(new CodeEdit(
                body[heap + 2].Offset,
                use,
                0,
                0,
                IsRemoval: true
            ));
            edits.Add(new CodeEdit(
                use + 1,
                use + 1 + cursor.Position,
                XRegisterTag,
                0
            ));
            heap = tail;
        }

        return ApplyDuplicatedFrameReadEdits(code, edits);
    }

    private byte[]? FoldStagedListConstructions(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !code.Span.Contains(PutList))
        {
            return null;
        }

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
            body = BeamInstructionReader.Read(header, atoms.Count + 1, literalBytes.Count);
        }
        catch (Exception error) when (error is InvalidDataException or NotSupportedException)
        {
            return null;
        }

        using var output = new MemoryStream();
        int copied = 0;
        for (int index = 0; index + 4 < body.Count; index++)
        {
            BeamInstruction staged = body[index];
            BeamInstruction independent = body[index + 2];
            BeamInstruction copy = body[index + 3];
            if (staged is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    {
                        Kind: BeamOperandParameters.Nil or BeamOperandParameters.Atom
                            or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue
                    },
                    { Kind: BeamOperandParameters.X, TypeIndex: null } temporary]
                }
                || body[index + 1] is not { Definition.Opcode: PutList, Operands: [var head, var tail, var result] }
                || copy is not
                {
                    Definition.Opcode: Move,
                    Operands: [var copiedResult, { Kind: BeamOperandParameters.X, TypeIndex: null } destination]
                }
                || temporary != head || temporary != result || temporary != copiedResult
                || destination == temporary || tail != destination || independent.Definition.Opcode != PutList
                || Mentions(independent, temporary) || Mentions(independent, destination))
            {
                continue;
            }

            int first = index;
            int next = index + 4;
            if (index > 0 && body[index - 1].Offset >= copied && next + 1 < body.Count
                && body[index - 1] is
                {
                    Definition.Opcode: Move,
                    Operands: [var saved, { Kind: BeamOperandParameters.X, TypeIndex: null } scratch]
                }
                && saved == temporary && scratch != temporary && scratch != destination
                && body[next] is { Definition.Opcode: Move, Operands: [var restored, var restoredDestination] }
                && restored == scratch && restoredDestination == temporary && !Mentions(independent, scratch)
                && DiscardsCopiedConstructionTemporary(body, next + 1, scratch))
            {
                first--;
                next++;
            }
            else if (!DiscardsCopiedConstructionTemporary(body, next, temporary))
            {
                continue;
            }

            // The cons reads its original tail before writing the argument register.
            int literalStart = staged.Offset + 1;
            var cursor = new BeamCursor(code[literalStart..body[index + 1].Offset]);
            if (BeamCompactReader.Read(cursor).WireTag == BeamOperandParameters.ExtendedTag)
            {
                _ = BeamCompactReader.Read(cursor);
            }

            output.Write(code.Span[copied..body[first].Offset]);
            output.WriteByte(PutList);
            output.Write(code.Span[literalStart..(literalStart + cursor.Position)]);
            BeamCompactWriter.Write(output, XRegisterTag, destination.Value);
            BeamCompactWriter.Write(output, XRegisterTag, destination.Value);
            output.Write(code.Span[independent.Offset..copy.Offset]);
            copied = body[next].Offset;
            index = next - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static bool Mentions(BeamInstruction instruction, BeamOperand register) =>
            instruction.Operands.Any(operand => SameProjectionRegister(operand, register));
    }
}
