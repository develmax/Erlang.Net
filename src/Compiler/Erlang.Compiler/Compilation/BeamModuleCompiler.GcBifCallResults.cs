using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? KeepAdjacentResultsInX0(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body,
        IReadOnlyList<(int Start, int End, int Slot)> stackOperands,
        bool mapOnly = false
    )
    {
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        var result = new BeamOperand(BeamOperandParameters.X, 0);
        int copied = 0;
        int entry = 0;
        for (int index = 0; index + 3 < body.Count; index++)
        {
            byte opcode = body[index].Definition.Opcode;
            if (opcode == FunctionInfo)
            {
                entry = index + 1;
            }

            if (!ProducesAdjacentX0(body[index], result)
                || body[index + 1] is not
                { Definition.Opcode: Move, Operands: [var source, { Kind: BeamOperandParameters.Y } saved] }
                || source != result)
            {
                continue;
            }

            int use = index + 2;
            while (body[use].Definition.Opcode == Line)
            {
                use++;
            }

            BeamInstruction consumer = body[use];
            int liveIndex = AdjacentResultLiveOperand(
                consumer,
                result,
                saved,
                mapOnly
            );
            if (liveIndex < 0)
            {
                continue;
            }

            int end = entry;
            bool stableFrame = true;
            while (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                stableFrame &= body[end].Definition.Opcode is not (TrimFrame or TryEnter or CatchEnter or DebugLine);
                end++;
            }

            if (!stableFrame || !(flow ??= new BeamProjectionFlow(body)).HasSingleStackCopyUse(
                entry,
                index + 1,
                end,
                saved,
                use
            ))
            {
                continue;
            }

            // The allocating instruction reads the result from the live X prefix. Clearing
            // the old slot preserves frame initialization until it can be removed.
            output.Write(code.Span[copied..body[index + 1].Offset]);
            WriteFrameInitialization(output, [(int)saved.Value]);
            copied = body[index + 2].Offset;
            int operandStart = consumer.Offset + 1;
            var operands = new BeamCursor(code[operandStart..]);
            for (int position = 0; position < liveIndex; position++)
            {
                _ = BeamCompactReader.Read(operands);
                if (consumer.Operands[position].Kind == BeamOperandParameters.LiteralValue
                    || consumer.Operands[position].TypeIndex is not null)
                {
                    _ = BeamCompactReader.Read(operands);
                }

                if (consumer.Operands[position].TypeIndex is not null)
                {
                    _ = BeamCompactReader.Read(operands);
                }
            }

            int live = operandStart + operands.Position;
            _ = BeamCompactReader.Read(operands);
            var root = new CodeEdit(
                live,
                operandStart + operands.Position,
                BeamWire.UnsignedTag,
                Math.Max(1, (int)consumer.Operands[liveIndex].Value)
            );
            var replacements = new Dictionary<int, (int End, BeamOperand Source)>();
            RecordEntryArgumentUses(
                body,
                stackOperands,
                new Dictionary<int, BeamOperand> { [use] = result },
                saved,
                replacements
            );
            var changes = replacements.Select(replacement =>
                new CodeEdit(
                    replacement.Key,
                    replacement.Value.End,
                    XRegisterTag,
                    0
                )).Append(root);
            foreach (var edit in changes.OrderBy(edit => edit.Start))
            {
                output.Write(code.Span[copied..edit.Start]);
                BeamCompactWriter.Write(output, edit.Tag, edit.Value);
                copied = edit.End;
            }

            index = use;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }

    private static bool ProducesAdjacentX0(BeamInstruction instruction, BeamOperand result) =>
        instruction.Definition.Opcode is LocalCall or ExternalCall
        || instruction.Definition.Opcode is (GcBifOne or GcBifTwo or GcBifThree)
            && instruction.Operands[0].Kind == BeamOperandParameters.Failure
            && instruction.Operands[^1] == result;

    private static int AdjacentResultLiveOperand(
        BeamInstruction instruction,
        BeamOperand result,
        BeamOperand saved,
        bool mapOnly
    )
    {
        var operands = instruction.Operands;
        if (operands.Count == 0 || operands[0].Kind != BeamOperandParameters.Failure)
        {
            return -1;
        }

        if (!mapOnly && instruction.Definition.Opcode is (GcBifOne or GcBifTwo or GcBifThree)
            && operands[^1] == result
            && operands.Skip(3).Take(operands.Count - 4).Any(operand => SameProjectionRegister(operand, saved)))
        {
            return 1;
        }

        if (!mapOnly || instruction.Definition.Opcode != PutMapAssoc || operands[2] != result || !operands[3].Value.IsZero)
        {
            return -1;
        }

        var inputs = operands.Skip(5).Prepend(operands[1]);

        return inputs.All(operand => operand.Kind != BeamOperandParameters.X)
            && inputs.Any(operand => SameProjectionRegister(operand, saved)) ? 3 : -1;
    }
}
