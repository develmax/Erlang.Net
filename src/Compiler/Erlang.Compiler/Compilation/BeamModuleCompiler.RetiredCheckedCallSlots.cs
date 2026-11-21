using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ReuseRetiredCheckedCallSlot(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(IsTaggedTuple)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var body = decoded.Body;
        var raw = new BeamOperand(BeamOperandParameters.X, 0);
        using var output = new MemoryStream();
        BeamProjectionFlow? flow = null;
        int copied = 0;
        int entry = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int start = entry;
            entry = end + 1;
            if (!Enumerable.Range(start, end - start).Any(index => body[index].Definition.Opcode == IsTaggedTuple)
                || Enumerable.Range(start, end - start).Any(index => body[index].Definition.Opcode is
                    TryEnter or CatchEnter or TrimFrame or LoopReceive or LoopReceiveEnd or Wait or WaitTimeout
                    or DebugLine or ExecutableLine))
            {
                continue;
            }

            (int[] Owners, Dictionary<int, bool[]> Uses)? frames = null;
            for (int test = start + 1; test + 3 < end; test++)
            {
                int projection = test + 1;
                int guard = test + 2;
                int success = test + 3;
                if (body[test - 1].Definition.Opcode is not (LocalCall or ExternalCall)
                    || body[test] is not
                    {
                        Definition.Opcode: IsTaggedTuple,
                        Operands: [{ Kind: BeamOperandParameters.Label } failure, var result, var arity, _]
                    } || result != raw
                    || body[projection] is not
                    {
                        Definition.Opcode: GetTupleElement,
                        Operands: [var tuple, var field, { Kind: BeamOperandParameters.X, TypeIndex: null, Value.Sign: > 0 } scratch]
                    } || tuple != raw || field.Value < 0 || field.Value >= arity.Value
                    || body[guard] is not
                    {
                        Definition.Opcode: ExactEqualTest or ExactNotEqualTest,
                        Operands:
                        [
                            var rejected, var checkedField,
                        {
                            Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer
                                or BeamOperandParameters.Nil or BeamOperandParameters.LiteralValue, TypeIndex: null
                        } expected
                        ]
                    } || rejected != failure || checkedField != scratch
                    || !(flow ??= new BeamProjectionFlow(body)).TryGetExclusiveForwardTarget(
                        test,
                        guard,
                        (int)failure.Value,
                        out int cold
                    ) || cold <= success || cold + 2 >= end
                    || body[cold + 1] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var original, { Kind: BeamOperandParameters.Y, TypeIndex: null } coldSlot]
                    } || original != raw
                    || (frames ??= ReadDisjointFrameOwnership(body, start, end)) is not { } frame || frame.Uses.Count != 1
                    || frame.Owners[test - start] < 0 || frame.Owners[cold + 1 - start] != frame.Owners[test - start]
                    || ReadColdCheckedResultBlock(cold + 2, end, coldSlot) is not { } jump
                    || !flow.TryGetForwardLabel(jump, (int)body[jump].Operands[0].Value, out int join)
                    || join + 1 >= end || frame.Owners[join - start] != frame.Owners[test - start]
                    || !flow.IsDead(join + 1, coldSlot, afterCall: true)
                    || !flow.IsEntryTemporaryDead(success, raw) || !flow.IsEntryTemporaryDead(success, scratch)
                    || !flow.IsEntryTemporaryDead(cold + 2, raw) || !flow.IsEntryTemporaryDead(cold + 2, scratch))
                {
                    continue;
                }

                int width = frame.Uses.Values.Single().Length;
                BeamOperand? saved = FindRetiredSlot(
                    flow,
                    width,
                    test,
                    success,
                    cold,
                    jump,
                    join,
                    coldSlot
                );
                if (saved is null)
                {
                    continue;
                }

                // Only the cold definition's uses are rewritten. In particular,
                // another branch can still read the earlier value of this slot.
                output.Write(code.Span[copied..body[test].Offset]);
                output.WriteByte(Move);
                WriteRegister(output, raw);
                WriteRegister(output, saved);
                output.Write(code.Span[body[test].Offset..body[projection].Offset]);
                output.WriteByte(GetTupleElement);
                WriteRegister(output, raw);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, field.Value);
                WriteRegister(output, raw);
                WriteGuard(body[guard].Definition.Opcode, failure, expected);
                output.Write(code.Span[body[success].Offset..body[cold + 1].Offset]);
                copied = body[cold + 2].Offset;
                int coldEnd = body[jump + 1].Offset;
                int operand = FirstStackOperandAtOrAfter(decoded.StackOperands, copied);
                while (operand < decoded.StackOperands.Count && decoded.StackOperands[operand].Start < coldEnd)
                {
                    var use = decoded.StackOperands[operand++];
                    if (use.Slot == (int)coldSlot.Value)
                    {
                        output.Write(code.Span[copied..use.Start]);
                        WriteRegister(output, saved);
                        copied = use.End;
                    }
                }

                test = jump;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        int? ReadColdCheckedResultBlock(int start, int end, BeamOperand slot)
        {
            bool read = false;
            for (int index = start; index < end; index++)
            {
                var instruction = body[index];
                byte opcode = instruction.Definition.Opcode;
                if (opcode == Jump)
                {
                    return read ? index : null;
                }

                if (opcode is not (Line or Move or InitYRegisters or TestHeap or PutList or PutTuple or LocalCall or ExternalCall))
                {
                    return null;
                }

                for (int position = 0; position < instruction.Operands.Count; position++)
                {
                    if (!SameProjectionRegister(instruction.Operands[position], slot))
                    {
                        continue;
                    }

                    int writes = ReadPostCallAliasWriteMask(instruction);
                    if (opcode == InitYRegisters || writes < 0 || position < sizeof(int) * 8 && (writes & (1 << position)) != 0)
                    {
                        return null;
                    }

                    read = true;
                }
            }

            return null;
        }

        BeamOperand? FindRetiredSlot(
            BeamProjectionFlow valueFlow,
            int width,
            int test,
            int success,
            int cold,
            int jump,
            int join,
            BeamOperand coldSlot
        )
        {
            // OTP takes the first available provisional Y register from its
            // ordered pool and then reverses the numbering within the frame.
            for (int index = width - 1; index >= 0; index--)
            {
                var candidate = new BeamOperand(BeamOperandParameters.Y, index);
                if (candidate == coldSlot || !valueFlow.IsInitialized(test, candidate, allowFramePredicates: true)
                    || !valueFlow.IsDead(success, candidate, afterCall: true)
                    || !valueFlow.IsDead(join + 1, candidate, afterCall: true)
                    || Enumerable.Range(cold + 2, jump - cold - 2).Any(operation => body[operation].Operands.Any(
                        operand => SameProjectionRegister(operand, candidate)
                    )))
                {
                    continue;
                }

                return candidate;
            }

            return null;
        }

        void WriteGuard(byte opcode, BeamOperand failure, BeamOperand expected)
        {
            output.WriteByte(opcode);
            BeamCompactWriter.Write(output, LabelTag, failure.Value);
            WriteRegister(output, raw);
            int tag = AtomTag;
            if (expected.Kind == BeamOperandParameters.LiteralValue)
            {
                output.WriteByte(ExtendedLiteralOperand);
                tag = BeamWire.UnsignedTag;
            }
            else if (expected.Kind == BeamOperandParameters.Integer)
            {
                tag = BeamWire.IntegerTag;
            }

            BeamCompactWriter.Write(output, tag, expected.Value);
        }
    }
}
