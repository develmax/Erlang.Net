using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseRetiredPairCallArgument(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(PutTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
        var x1 = new BeamOperand(BeamOperandParameters.X, 1);
        var x2 = new BeamOperand(BeamOperandParameters.X, 2);
        var entries = new HashSet<int>();
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
            int frame = FindLinearFrameSuffix(body, entry, end);
            if (frame < 0 || body[frame].Definition.Opcode != Allocate
                || ReadRetiredPairCallArgument(
                    body,
                    entry,
                    end,
                    frame,
                    x0,
                    x1
                ) is not { } plan)
            {
                continue;
            }

            var (save, setupEnd, oldResult, expired, roots) = plan;
            output.Write(code.Span[copied..body[save].Offset]);
            output.WriteByte(TestHeap);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, body[save + 1].Operands[0].Value);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
            int tuple = body[save + 2].Offset;
            var cursor = new BeamCursor(code[(tuple + 1)..]);
            _ = BeamCompactReader.Read(cursor);
            output.WriteByte(PutTuple);
            WriteRegister(output, x1);
            output.Write(code.Span[(tuple + 1 + cursor.Position)..body[save + 3].Offset]);
            WriteMove(expired, x2);
            WriteMove(x0, expired);
            WriteMove(x1, x0);
            WriteMove(x2, x1);
            WriteFrameInitialization(output, roots);
            copied = body[setupEnd].Offset;

            int operand = FirstStackOperandAtOrAfter(stackOperands, copied);
            for (; operand < stackOperands.Count && stackOperands[operand].Start < body[end].Offset; operand++)
            {
                var (from, to, slot) = stackOperands[operand];
                if (slot == oldResult.Value)
                {
                    output.Write(code.Span[copied..from]);
                    WriteRegister(output, expired);
                    copied = to;
                }
            }

            entries.Add((int)body[entry].Operands[0].Value);
        }

        if (entries.Count == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);
        var rewritten = output.ToArray();
        if (DecodeLateAtomicProjectionSlots(rewritten, labelCount) is not { } remapped)
        {
            return null;
        }

        // Only the proven functions gain a dead interior slot.
        return RewriteDisjointFrames(
            rewritten,
            remapped.Body,
            remapped.StackOperands,
            atomicOnly: false,
            entriesToCompact: entries
        );

        void WriteMove(BeamOperand source, BeamOperand target)
        {
            output.WriteByte(Move);
            WriteRegister(output, source);
            WriteRegister(output, target);
        }
    }

    private static (int Save, int SetupEnd, BeamOperand Result, BeamOperand Expired, int[] Roots)?
        ReadRetiredPairCallArgument(
            IReadOnlyList<BeamInstruction> body,
            int entry,
            int end,
            int frame,
            BeamOperand x0,
            BeamOperand x1
        )
    {
        for (int save = frame + 2; save + 5 < end; save++)
        {
            if (body[save - 1].Definition.Opcode is LocalCall or ExternalCall
                && body[save] is
                {
                    Definition.Opcode: Move,
                    Operands: [var returned, { Kind: BeamOperandParameters.Y, TypeIndex: null } stored]
                } && returned == x0
                && body[save + 1] is
                {
                    Definition.Opcode: TestHeap,
                    Operands: [{ Kind: BeamOperandParameters.Unsigned, Value: var size }, { Value.IsZero: true }]
                } && size == 3
                && body[save + 2] is
                {
                    Definition.Opcode: PutTuple,
                    Operands: [var target, _, var first, var second]
                } && target == x0 && IsPairInput(first, stored) && IsPairInput(second, stored)
                && body[save + 3] is
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } retired, var argument]
                }
                && retired != stored && argument == x1
                && body[save + 4] is
                {
                    Definition.Opcode: InitYRegisters,
                    Operands: [{ Value.IsOne: true }, var cleared]
                } && cleared == retired)
            {
                int setupEnd = save + 5;
                int call = setupEnd;
                while (call < end && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                if (call + 1 >= end || body[call] is not
                    {
                        Definition.Opcode: LocalCall or ExternalCall,
                        Operands: [{ Value: var arity }, _]
                    }
                    || arity != 2 || !CanReuseDisplacedBranchSlot(
                        body,
                        entry,
                        end,
                        frame,
                        call,
                        stored,
                        retired
                    ) || !RetainedPairCallHasSinglePrefix(
                        body,
                        frame,
                        save,
                        end,
                        stored
                    ))
                {
                    return null;
                }

                int[] roots = new[] { first, second }.Distinct()
                    .Where(operand => operand.Kind == BeamOperandParameters.Y && operand != retired
                        && !Enumerable.Range(call + 1, end - call - 1).Any(index =>
                            body[index].Operands.Any(read => SameProjectionRegister(read, operand))))
                    .Select(operand => (int)operand.Value)
                    .Order()
                    .ToArray();

                return (save, setupEnd, stored, retired, roots);
            }

            if (body[save].Definition.Opcode is not
                    (Move or Swap or Line or InitYRegisters or GetTupleElement or LocalCall or ExternalCall
                    or TestHeap or PutTuple or PutList))
            {
                break;
            }
        }

        return null;

        static bool IsPairInput(BeamOperand operand, BeamOperand stored) =>
            operand is { Kind: BeamOperandParameters.Y, TypeIndex: null } && operand != stored
                || IsNonRegisterValue(operand);
    }

    private static bool RetainedPairCallHasSinglePrefix(
        IReadOnlyList<BeamInstruction> body,
        int frame,
        int save,
        int end,
        BeamOperand stored
    )
    {
        if (body[frame + 1].Definition.Opcode is not
                (Move or Swap or Line or InitYRegisters or GetTupleElement or LocalCall or ExternalCall)
            || Enumerable.Range(frame + 1, save - frame - 1).Any(index =>
                body[index].Definition.Opcode != InitYRegisters
                && body[index].Operands.Any(slot => SameProjectionRegister(slot, stored))))
        {
            return false;
        }

        // The rewritten stack names describe values only after the old
        // argument was consumed. No continuation may restart the old prefix.
        var prefixLabels = body.Take(save)
            .Where(instruction => instruction.Definition.Opcode == Label)
            .Select(instruction => instruction.Operands[0].Value)
            .ToHashSet();

        return !Enumerable.Range(save + 5, end - save - 5).Any(index =>
            body[index].Definition.Opcode is not (LocalCall or TailLocalCall or TailLocalCallWithFrame)
            && body[index].Operands.Any(operand => operand.Kind == BeamOperandParameters.Label
                && prefixLabels.Contains(operand.Value)));
    }
}
