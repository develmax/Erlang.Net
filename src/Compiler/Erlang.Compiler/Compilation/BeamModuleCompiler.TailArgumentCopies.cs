using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? ComposeTailArgumentCopies(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        int entry = -1;
        for (int call = 0; call < body.Count; call++)
        {
            BeamInstruction tail = body[call];
            if (tail.Definition.Opcode == FunctionInfo)
            {
                entry = call + 1;
                copied = WriteGuardedProjectionTailCopies(
                    code,
                    body,
                    output,
                    copied,
                    entry,
                    (int)tail.Operands[^1].Value
                );
                continue;
            }

            bool framed = tail.Definition.Opcode == TailLocalCallWithFrame;
            bool ordinary = tail.Definition.Opcode == LocalCall;
            if (!framed && !ordinary && tail.Definition.Opcode is not (TailLocalCall or TailExternalCall))
            {
                continue;
            }

            int end = call;
            while (end > 0 && body[end - 1].Definition.Opcode == Line)
            {
                end--;
            }

            int start = end;
            while (start > 0 && body[start - 1].Definition.Opcode == Move
                && (!ordinary || body[start - 1].Operands is
                    [{ Kind: BeamOperandParameters.X }, { Kind: BeamOperandParameters.X }]))
            {
                start--;
            }

            int arity = (int)tail.Operands[0].Value;
            if (end - start < 2 || ordinary && !IsEntryScratchShuffle(
                entry,
                start,
                end,
                arity
            ))
            {
                continue;
            }

            var frame = framed ? ReadMoveOnlyFrame(entry, start - 1, call) : null;
            if (framed && frame is null || ReadTailCopyPlan(
                start,
                end,
                arity,
                frame
            ) is not { } plan)
            {
                continue;
            }

            int replacement = framed ? start - 1 : start;
            output.Write(code.Span[copied..body[replacement].Offset]);
            foreach (var move in plan)
            {
                int original = move.Source.Instruction;
                int originalDestination = (int)body[original].Operands[1].Value;
                int sourceStart = body[original].Offset + 1;
                int sourceEnd = body[original + 1].Offset - CompactRegisterCountWidth(originalDestination);
                output.WriteByte(Move);
                output.Write(code.Span[sourceStart..sourceEnd]);
                BeamCompactWriter.Write(output, XRegisterTag, move.Destination);
            }

            copied = body[end].Offset;
            if (frame is { } removed)
            {
                output.Write(code.Span[copied..tail.Offset]);
                output.WriteByte(TailLocalCall);
                int next = body[call + 1].Offset;
                output.Write(code.Span[(tail.Offset + 1)..(next - CompactRegisterCountWidth(removed.Slots))]);
                copied = next;
            }
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool IsEntryScratchShuffle(
            int entry,
            int start,
            int end,
            int arity
        )
        {
            if (entry <= 0 || entry + 1 >= start || body[entry].Definition.Opcode != Label
                || body[entry + 1] is not { Definition.Opcode: Allocate, Operands: [var slots, var live] }
                || live.Value > body[entry - 1].Operands[^1].Value || arity > live.Value)
            {
                return false;
            }

            for (int index = entry + 2; index < start; index++)
            {
                if (body[index] is not
                    {
                        Definition.Opcode: Move,
                        Operands:
                        [
                        { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                        { Kind: BeamOperandParameters.Y, TypeIndex: null } saved
                        ]
                    }
                    || source.Value >= live.Value || saved.Value >= slots.Value)
                {
                    return false;
                }
            }

            // A local call consumes only the outgoing argument prefix and
            // replaces all caller X registers with its result in X0.
            var scratch = new HashSet<int>();
            bool readScratch = false;
            for (int index = start; index < end; index++)
            {
                var operands = body[index].Operands;
                int source = (int)operands[0].Value;
                int target = (int)operands[1].Value;
                if (source >= live.Value)
                {
                    if (!scratch.Contains(source))
                    {
                        return false;
                    }

                    readScratch = true;
                }

                if (target >= live.Value)
                {
                    scratch.Add(target);
                }
            }

            return readScratch;
        }

        (int Slots, int Live)? ReadMoveOnlyFrame(int entry, int allocation, int call)
        {
            if (entry <= 0 || allocation < entry || call + 1 >= body.Count
                || body[allocation] is not
                {
                    Definition.Opcode: Allocate, Operands: [var slots, var live]
                }
                || slots.Value <= 0 || slots.Value > BeamOperandParameters.MaximumRegister
                || slots.Value != body[call].Operands[^1].Value
                || live.Value > body[entry - 1].Operands[^1].Value)
            {
                return null;
            }

            for (int index = entry; index < allocation; index++)
            {
                BeamInstruction instruction = body[index];
                if (!(instruction.Definition.Opcode is Label or Line or Jump
                    || IsEntryFramePredicate(instruction.Definition.Opcode))
                    || instruction.Operands.Any(operand => operand.Kind == BeamOperandParameters.Y))
                {
                    return null;
                }
            }

            // The guard may fail through func_info. Every path reaching the call
            // must own this allocation, and the frame cannot escape the move block.
            if (ReadDisjointFrameOwnership(body, entry, call + 1) is not { } ownership
                || ownership.Uses.Count != 1 || ownership.Owners[call - entry] != allocation)
            {
                return null;
            }

            return ((int)slots.Value, (int)live.Value);
        }

        IReadOnlyList<TailArgumentCopy>? ReadTailCopyPlan(
            int start,
            int end,
            int arity,
            (int Slots, int Live)? frame
        )
        {
            var original = new Dictionary<int, TailArgumentOrigin>();
            var stack = frame.HasValue ? new Dictionary<int, TailArgumentOrigin>() : null;
            bool reloaded = false;
            for (int index = start; index < end; index++)
            {
                if (body[index].Operands is not [var source, { TypeIndex: null } target] || source.TypeIndex is not null)
                {
                    return null;
                }

                TailArgumentOrigin origin;
                if (source.Kind == BeamOperandParameters.Y)
                {
                    if (stack is null || !stack.TryGetValue((int)source.Value, out origin))
                    {
                        return null;
                    }

                    reloaded |= target.Kind == BeamOperandParameters.X;
                }
                else if (source.Kind is BeamOperandParameters.X or BeamOperandParameters.Atom
                    or BeamOperandParameters.Nil or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue)
                {
                    if (frame is { } checkedFrame && source.Kind == BeamOperandParameters.X
                        && source.Value >= checkedFrame.Live && !original.ContainsKey((int)source.Value))
                    {
                        return null;
                    }

                    origin = ResolveTailArgumentOrigin(original, new TailArgumentOrigin(source, index));
                }
                else
                {
                    return null;
                }

                if (target.Kind == BeamOperandParameters.X)
                {
                    original[(int)target.Value] = origin;
                }
                else if (stack is not null && frame is { } stackFrame
                    && target.Kind == BeamOperandParameters.Y && target.Value < stackFrame.Slots)
                {
                    stack[(int)target.Value] = origin;
                }
                else
                {
                    return null;
                }
            }

            if (frame is { } entryFrame && (!reloaded || Enumerable.Range(entryFrame.Live, Math.Max(0, arity - entryFrame.Live))
                .Any(argument => !original.ContainsKey(argument))))
            {
                return null;
            }

            var remaining = new List<TailArgumentCopy>();
            for (int argument = 0; argument < arity; argument++)
            {
                if (original.TryGetValue(argument, out var origin)
                    && !(origin.Value.Kind == BeamOperandParameters.X && origin.Value.Value == argument))
                {
                    remaining.Add(new(origin, argument));
                }
            }

            if (remaining.Count >= end - start || OrderTailCopies(remaining) is not { } ordered)
            {
                return null;
            }

            // Compare every outgoing argument's origin before encoding. Argument setup
            // does not need to preserve the X registers outside the call's arity.
            var rewritten = new Dictionary<int, TailArgumentOrigin>();
            foreach (var move in ordered)
            {
                rewritten[move.Destination] = ResolveTailArgumentOrigin(rewritten, move.Source);
            }

            for (int argument = 0; argument < arity; argument++)
            {
                if (FinalTailArgumentValue(original, argument) != FinalTailArgumentValue(rewritten, argument))
                {
                    return null;
                }
            }

            return ordered;
        }
    }

    // setup_args supplies destination order to beam_ssa_codegen:order_moves/1. The
    // acyclic form follows source-to-destination chains and prepends each chain.
    private static IReadOnlyList<TailArgumentCopy>? OrderTailCopies(List<TailArgumentCopy> remaining)
    {
        var ordered = new List<TailArgumentCopy>(remaining.Count);
        while (remaining.Count > 0)
        {
            var move = remaining[0];
            remaining.RemoveAt(0);
            var chain = new List<TailArgumentCopy> { move };
            while (move.Source.Value.Kind == BeamOperandParameters.X)
            {
                int source = (int)move.Source.Value.Value;
                if (chain.Any(item => item.Destination == source))
                {
                    return null;
                }

                int next = remaining.FindIndex(item => item.Destination == source);
                if (next < 0)
                {
                    break;
                }

                move = remaining[next];
                remaining.RemoveAt(next);
                chain.Add(move);
            }

            ordered.InsertRange(0, chain);
        }

        return ordered;
    }

    private static TailArgumentOrigin ResolveTailArgumentOrigin(
        Dictionary<int, TailArgumentOrigin> values,
        TailArgumentOrigin operand
    ) =>
        operand.Value.Kind == BeamOperandParameters.X && values.TryGetValue((int)operand.Value.Value, out var origin)
            ? origin : operand;

    private static BeamOperand FinalTailArgumentValue(Dictionary<int, TailArgumentOrigin> values, int register) =>
        values.TryGetValue(register, out var origin) ? origin.Value : new BeamOperand(BeamOperandParameters.X, register);

    private readonly record struct TailArgumentOrigin(BeamOperand Value, int Instruction);

    private readonly record struct TailArgumentCopy(TailArgumentOrigin Source, int Destination);
}
