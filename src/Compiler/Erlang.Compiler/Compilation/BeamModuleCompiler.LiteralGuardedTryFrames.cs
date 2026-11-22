using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private byte[]? SplitLiteralGuardedTryFrames(ReadOnlyMemory<byte> code, int labelCount)
    {
        if (ModuleOptimizationDisabled || !SsaTypeInferenceEnabled || !code.Span.Contains(TryEnter)
            || !code.Span.Contains(ExactEqualTest)
            || DecodeLateAtomicProjectionSlots(code, labelCount) is not { } decoded)
        {
            return null;
        }

        var (body, stackOperands) = decoded;
        var x0 = new BeamOperand(BeamOperandParameters.X, 0);
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
            int allocation = start + 1;
            int predicate = start + 4;
            int firstTry = predicate + 1;
            if (start == 0 || firstTry + 12 >= end
                || body[start - 1] is not { Definition.Opcode: FunctionInfo, Operands: [_, _, { Value.IsOne: true }] }
                || body[start].Definition.Opcode != Label || body[allocation] is not
                {
                    Definition.Opcode: Allocate,
                    Operands: [var size, { Value.IsOne: true }]
                } || size.Value < 2 || size.Value > BeamOperandParameters.MaximumRegister
                || body[start + 2] is not { Definition.Opcode: InitYRegisters } initialize
                || body[start + 3] is not
                {
                    Definition.Opcode: Move,
                    Operands: [var input, { Kind: BeamOperandParameters.Y, TypeIndex: null } saved]
                } || input != x0 || saved.Value >= size.Value
                || body[predicate] is not
                {
                    Definition.Opcode: ExactEqualTest,
                    Operands: [{ Kind: BeamOperandParameters.Label } rejected, var value,
                    { Kind: BeamOperandParameters.Atom } literal]
                } || value != saved
                || body[firstTry] is not
                {
                    Definition.Opcode: TryEnter,
                    Operands: [{ Kind: BeamOperandParameters.Y, TypeIndex: null } token, _]
                } || token == saved || token.Value >= size.Value
                || body[firstTry + 1] is not
                {
                    Definition.Opcode: Move,
                    Operands: [{ Kind: BeamOperandParameters.Atom }, var firstArgument]
                } || firstArgument != x0)
            {
                continue;
            }

            int width = (int)size.Value;
            if (initialize.Operands.Skip(1).ToHashSet().Count != width - 1
                || initialize.Operands.Skip(1).Any(slot => slot.Kind != BeamOperandParameters.Y
                    || slot.TypeIndex is not null || slot == saved || slot.Value >= width)
                || !(flow ??= new BeamProjectionFlow(body)).TryGetSingleForwardTarget(
                    predicate,
                    (int)rejected.Value,
                    out int fallback
                ) || fallback <= firstTry + 2 || fallback + 10 >= end || body[fallback - 1].Definition.Opcode != Return
                || body[fallback + 1] is not { Definition.Opcode: TryEnter, Operands: [var fallbackToken, var handler] }
                || fallbackToken != token || !IsMove(body[fallback + 2], saved, x0)
                || ReadLiteralGuardedFallbackEnd(
                    body,
                    flow,
                    fallback,
                    end,
                    token,
                    handler,
                    size
                ) is not int fallbackEnd
                || Enumerable.Range(firstTry, fallback - firstTry)
                    .Any(at => body[at].Definition.Opcode is InitYRegisters or DebugLine or ExecutableLine
                        || body[at].Operands.Any(operand => SameProjectionRegister(operand, saved)))
                || PlanBranchStackSlots(
                    body,
                    start,
                    fallback,
                    linearAllocation: allocation,
                    firstOwnedInstruction: firstTry
                ) is not { Trim: null } hot || hot.Count >= width)
            {
                continue;
            }

            // Neither arm reads the other arm's stack. Check the original
            // unary input first, then allocate for the selected handler.
            output.Write(code.Span[copied..body[allocation].Offset]);
            output.WriteByte(ExactEqualTest);
            BeamCompactWriter.Write(output, LabelTag, rejected.Value);
            WriteRegister(output, x0);
            BeamCompactWriter.Write(output, AtomTag, literal.Value);
            WriteAllocation(hot.Count, 0);
            int handlerSlot = hot.Slots[(int)token.Value];
            int[] initializedSlots = Enumerable.Range(0, hot.Count).Where(slot => slot != handlerSlot).ToArray();
            WriteFrameInitialization(output, initializedSlots);
            WriteRegion(
                firstTry,
                fallback,
                hot.Count,
                hot.Slots
            );

            output.Write(code.Span[body[fallback].Offset..body[fallback + 1].Offset]);
            WriteAllocation(1, 1);
            var coldSlots = new int[width];
            coldSlots[(int)token.Value] = 0;
            WriteRegion(
                fallback + 1,
                fallback + 2,
                1,
                coldSlots
            );
            WriteRegion(
                fallback + 3,
                fallbackEnd,
                1,
                coldSlots
            );
            copied = body[fallbackEnd].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        void WriteAllocation(int width, int live)
        {
            output.WriteByte(Allocate);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width);
            BeamCompactWriter.Write(output, BeamWire.UnsignedTag, live);
        }

        void WriteRegion(
            int start,
            int end,
            int width,
            IReadOnlyList<int> slots
        )
        {
            int first = body[start].Offset;
            int slotIndex = FirstStackOperandAtOrAfter(stackOperands, first);
            for (int at = start; at < end; at++)
            {
                int next = body[at + 1].Offset;
                int size = LinearFrameSizeOperand(body[at].Definition.Opcode);
                if (size >= 0)
                {
                    int operandStart = body[at].Offset + 1;
                    var cursor = new BeamCursor(code[operandStart..]);
                    for (int position = 0; position < size; position++)
                    {
                        _ = BeamCompactReader.Read(cursor);
                    }

                    output.Write(code.Span[first..(operandStart + cursor.Position)]);
                    BeamCompactWriter.Write(output, BeamWire.UnsignedTag, width);
                    _ = BeamCompactReader.Read(cursor);
                    first = operandStart + cursor.Position;
                }

                for (; slotIndex < stackOperands.Count && stackOperands[slotIndex].Start < next; slotIndex++)
                {
                    var (from, to, slot) = stackOperands[slotIndex];
                    output.Write(code.Span[first..from]);
                    BeamCompactWriter.Write(output, YRegisterTag, slots[slot]);
                    first = to;
                }
            }

            output.Write(code.Span[first..body[end].Offset]);
        }
    }

    private static int? ReadLiteralGuardedFallbackEnd(
        IReadOnlyList<BeamInstruction> body,
        BeamProjectionFlow flow,
        int fallback,
        int end,
        BeamOperand token,
        BeamOperand handler,
        BeamOperand width
    )
    {
        int installed = fallback + 1;
        if (handler.Kind != BeamOperandParameters.Label || !flow.TryGetSingleForwardTarget(
            installed,
            (int)handler.Value,
            out int landing
        ) || landing + 5 > end)
        {
            return null;
        }

        bool called = false;
        int exit = fallback + 3;
        for (; exit < landing; exit++)
        {
            BeamInstruction instruction = body[exit];
            switch (instruction.Definition.Opcode)
            {
                case Line:
                    continue;
                case Move when instruction.Operands[1].Kind == BeamOperandParameters.X
                    && instruction.Operands[0].Kind is BeamOperandParameters.X or BeamOperandParameters.Atom
                        or BeamOperandParameters.Integer or BeamOperandParameters.Nil:
                    continue;
                case LocalCall or ExternalCall:
                    called = true;
                    continue;
            }

            break;
        }

        if (!called || exit + 3 != landing
            || body[exit] is not { Definition.Opcode: TryEnd, Operands: [var normal] } || normal != token
            || !IsLiteralGuardedFrameReturn(body, exit + 1, width)
            || body[landing + 1] is not { Definition.Opcode: TryCase, Operands: [var exceptional] } || exceptional != token
            || body[landing + 2] is not
            {
                Definition.Opcode: Move,
                Operands: [{ Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.Nil },
                { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null }]
            } || !IsLiteralGuardedFrameReturn(body, landing + 3, width))
        {
            return null;
        }

        return landing + 5;
    }

    private static bool IsLiteralGuardedFrameReturn(IReadOnlyList<BeamInstruction> body, int index, BeamOperand width) =>
        body[index] is { Definition.Opcode: Deallocate, Operands: [var released] }
        && released == width && body[index + 1].Definition.Opcode == Return;
}
