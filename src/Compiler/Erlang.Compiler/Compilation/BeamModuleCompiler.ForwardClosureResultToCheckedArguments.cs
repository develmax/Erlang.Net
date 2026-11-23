using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? ForwardClosureResultToCheckedArguments(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(MakeFun)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { Body: var body })
        {
            return null;
        }

        BeamOperand x0 = new(BeamOperandParameters.X, 0);
        BeamOperand x1 = new(BeamOperandParameters.X, 1);
        BeamProjectionFlow? flow = null;
        using var output = new MemoryStream(code.Length);
        int copied = 0;
        int start = 0;
        for (int end = 0; end < body.Count; end++)
        {
            if (body[end].Definition.Opcode is not (FunctionInfo or EndCode))
            {
                continue;
            }

            int entry = start;
            start = end + 1;
            if (!HasUnprotectedCallKillSchedule(body, entry))
            {
                continue;
            }

            for (int save = entry + 3; save + 10 < end; save++)
            {
                if (body[save] is not
                    {
                        Definition.Opcode: Move,
                        Operands: [var result, { Kind: BeamOperandParameters.Y, TypeIndex: null } spilled]
                    } || result != x0
                    || body[save - 1] is not { Definition.Opcode: ExternalCall or LocalCall, Operands: [{ Value: var arity }, _] }
                    || arity != 1 || body[save + 1] is not
                    {
                        Definition.Opcode: ExactEqualTest,
                        Operands: [var rejected, { Kind: BeamOperandParameters.Y, TypeIndex: null } checkedSlot,
                        { Kind: BeamOperandParameters.Atom }]
                    } || body[save + 2] is not
                    {
                        Definition.Opcode: IsAtom,
                        Operands: [var invalid, { Kind: BeamOperandParameters.Y, TypeIndex: null } field]
                    } || rejected != invalid || checkedSlot == field || checkedSlot == spilled || field == spilled
                    || body[save + 3] is not { Definition.Opcode: TestHeap, Operands: [_, { Value.IsZero: true }] }
                    || body[save + 4] is not
                    {
                        Definition.Opcode: PutTuple,
                        Operands: [var tuple, { Value: var elements }, { Kind: BeamOperandParameters.Atom }, var payload]
                    } || tuple != x0 || elements != 2 || payload != field || !IsMove(body[save + 5], spilled, x1))
                {
                    continue;
                }

                int call = save + 6;
                while (call < end && body[call].Definition.Opcode == Line)
                {
                    call++;
                }

                int closure = save - 2;
                while (closure > entry && body[closure].Definition.Opcode == Line)
                {
                    closure--;
                }

                BeamOperand[] capturedSlots = body[closure].Operands.Skip(3).ToArray();
                if (call + 4 >= end || body[call] is not
                    { Definition.Opcode: LocalCall or ExternalCall, Operands: [{ Value: var arguments }, _] }
                    || arguments != 2
                    || body[call + 1] is not
                    {
                        Definition.Opcode: TailLocalCallWithFrame or TailExternalCallWithFrame,
                        Operands: [{ Value.IsOne: true }, _, var width]
                    } || !(flow ??= new BeamProjectionFlow(body)).TryGetExclusiveForwardTarget(
                        save + 1,
                        save + 2,
                        (int)rejected.Value,
                        out int cold
                    ) || cold != call + 2 || !IsMove(body[cold + 1], spilled, x0)
                    || body[cold + 2].Definition.Opcode != body[call + 1].Definition.Opcode
                    || !body[cold + 2].Operands.SequenceEqual(body[call + 1].Operands)
                    || body[closure] is not
                    {
                        Definition.Opcode: MakeFun, Operands:
                        [_, var closureDestination, { Value: var captures }, ..]
                    } || closureDestination != x0 || captures != width.Value
                    || capturedSlots.Length != width.Value
                    || ReadDisjointFrameOwnership(body, entry, end) is not { } ownership
                    || !ownership.Uses.TryGetValue(ownership.Owners[closure - entry], out bool[]? frame)
                    || frame.Length != width.Value || ownership.Owners[call - entry] != ownership.Owners[closure - entry]
                    || !capturedSlots.All(slot => slot is { Kind: BeamOperandParameters.Y, TypeIndex: null }
                        && slot.Value < frame.Length && flow.IsInitialized(closure, slot))
                    || capturedSlots.Distinct().Count() != frame.Length
                    || !capturedSlots.Contains(checkedSlot) || !capturedSlots.Contains(field) || !capturedSlots.Contains(spilled))
                {
                    continue;
                }

                // The callback carries its captures by value, and these two
                // checks and the tuple are the only later reads of the old frame.
                int afterClosure = body[closure + 1].Offset;
                output.Write(code.Span[copied..afterClosure]);
                WriteFrameInitialization(output, capturedSlots.Where(slot => slot != checkedSlot && slot != field)
                    .Select(slot => (int)slot.Value).Order().ToArray());
                output.Write(code.Span[afterClosure..body[save].Offset]);
                output.Write(code.Span[body[save + 1].Offset..body[save + 3].Offset]);

                int heapEnd = body[save + 4].Offset;
                output.Write(code.Span[body[save + 3].Offset..(heapEnd - 1)]);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 1);
                output.WriteByte(Move);
                WriteRegister(output, x0);
                WriteRegister(output, x1);
                output.Write(code.Span[heapEnd..body[save + 5].Offset]);
                output.WriteByte(TrimFrame);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, frame.Length);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                output.Write(code.Span[body[save + 6].Offset..body[call + 1].Offset]);

                int frameEnd = body[cold].Offset;
                int tailStart = body[call + 1].Offset;
                var cursor = new BeamCursor(code[(tailStart + 1)..frameEnd]);
                _ = BeamCompactReader.Read(cursor);
                _ = BeamCompactReader.Read(cursor);
                output.Write(code.Span[tailStart..(tailStart + 1 + cursor.Position)]);
                BeamCompactWriter.Write(output, BeamWire.UnsignedTag, 0);
                output.Write(code.Span[frameEnd..body[cold + 1].Offset]);
                copied = body[cold + 2].Offset;
                break;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
    }
}
