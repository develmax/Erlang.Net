using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static byte[]? OrderIndependentStackStores(ReadOnlyMemory<byte> code, IReadOnlyList<BeamInstruction> body)
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int start = 0; start + 1 < body.Count; start++)
        {
            if (!IsStackStore(body[start]))
            {
                continue;
            }

            int end = start + 1;
            while (end + 1 < body.Count && IsStackStore(body[end]))
            {
                end++;
            }

            if (end - start > 1)
            {
                // All sources are X registers, so a later write to the same Y slot
                // makes the earlier store dead throughout this run.
                IEnumerable<int> original = Enumerable.Range(start, end - start);
                int[] order = original.GroupBy(index => body[index].Operands[1].Value)
                    .OrderBy(stores => stores.Key)
                    .Select(stores => stores.Last())
                    .ToArray();
                if (!order.SequenceEqual(original))
                {
                    output.Write(code.Span[copied..body[start].Offset]);
                    foreach (int index in order)
                    {
                        output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
                    }

                    copied = body[end].Offset;
                }
            }

            start = end - 1;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        static bool IsStackStore(BeamInstruction instruction) => instruction is
        {
            Definition.Opcode: Move,
            Operands: [{ Kind: BeamOperandParameters.X }, { Kind: BeamOperandParameters.Y }]
        };
    }

    private static byte[]? OrderIndependentCallArguments(
        ReadOnlyMemory<byte> code,
        IReadOnlyList<BeamInstruction> body
    )
    {
        using var output = new MemoryStream();
        int copied = 0;
        for (int call = 0; call < body.Count; call++)
        {
            int extraArguments = body[call].Definition.Opcode switch
            {
                LocalCall or TailLocalCall or TailLocalCallWithFrame => 0,
                ExternalCall or TailExternalCall or TailExternalCallWithFrame => 0,
                DynamicApply or TailDynamicApply => 2,
                _ => -1
            };
            if (extraArguments < 0)
            {
                continue;
            }

            int end = call;
            while (end > 0 && body[end - 1].Definition.Opcode == Line)
            {
                end--;
            }

            int start = end;
            while (start > 0 && body[start - 1] is
                { Definition.Opcode: Move, Operands: [_, { Kind: BeamOperandParameters.X }] })
            {
                start--;
            }

            int count = end - start;
            int arity = (int)body[call].Operands[0].Value + extraArguments;
            if (count == 2 && body[call].Definition.Opcode is (LocalCall or ExternalCall)
                && WriteProjectedCopies(start, end, arity))
            {
                continue;
            }

            if (extraArguments == 0 && count >= 2
                && FindOutgoingResultSave(
                    body,
                    start,
                    end - 1,
                    arity
                ) is int save)
            {
                int[] retained = Enumerable.Range(start, count - 1).Where(index => index != save).ToArray();
                WriteMoves(start, end, retained);
                continue;
            }

            if (count < 2 || count > arity
                || start > 0 && body[start - 1].Definition.Opcode == Move)
            {
                continue;
            }

            if (count >= 3 && count == arity && extraArguments == 0
                && ReadCallResultLiteralMoveOrder(
                    body,
                    start,
                    call,
                    count
                ) is { } resultOrder)
            {
                WriteMoves(start, end, resultOrder);
                continue;
            }

            if (count == 3 && arity == count
                && ReadNestedTailCallMoveOrder(body, start, call) is { } nestedOrder)
            {
                WriteMoves(start, end, nestedOrder);
                continue;
            }

            bool retainedResult = extraArguments == 0 && count == arity - 1 && start > 0
                && body[start - 1].Definition.Opcode is (LocalCall or ExternalCall)
                && !CallFollowsFrameTrim(body, start - 1);
            BeamOperand? constructed = null;
            if (count != arity && !retainedResult)
            {
                if (start == 0)
                {
                    continue;
                }

                BeamInstruction constructor = body[start - 1];
                constructed = constructor.Definition.Opcode switch
                {
                    PutTuple => constructor.Operands[0],
                    PutList => constructor.Operands[^1],
                    _ => null
                };
                if (constructed is not { Kind: BeamOperandParameters.X } || constructed.Value >= arity)
                {
                    continue;
                }
            }

            // A tuple or list can already occupy an outgoing register. Order only
            // the remaining moves, provided they do not depend on their destinations.
            // An immediately preceding call can already have supplied X0.
            var byDestination = new int[arity];
            Array.Fill(byDestination, -1);
            bool hasNonStackSource = false;
            bool independent = true;
            for (int index = start; index < end; index++)
            {
                BeamOperand source = body[index].Operands[0];
                BeamOperand destination = body[index].Operands[1];
                if (destination.Value >= arity || byDestination[(int)destination.Value] >= 0
                    || source.Kind == BeamOperandParameters.X && source.Value < arity)
                {
                    independent = false;
                    break;
                }

                byDestination[(int)destination.Value] = index;
                hasNonStackSource |= source.Kind != BeamOperandParameters.Y;
            }

            if (!independent || (retainedResult ? hasNonStackSource || byDestination[0] >= 0 : !hasNonStackSource)
                || constructed is not null && (byDestination[(int)constructed.Value] >= 0 || byDestination[0] < 0))
            {
                continue;
            }

            int[] present = count == arity ? byDestination : byDestination.Where(index => index >= 0).ToArray();
            int[] order;
            if (retainedResult)
            {
                order = present.OrderBy(index => body[index].Operands[0].Value).ToArray();
            }
            else
            {
                order = IndependentCallMoveOrder(count);
                for (int index = 0; index < order.Length; index++)
                {
                    order[index] = present[order[index]];
                }

                // beam_block sorts contiguous Y-to-X loads by source, excluding the final X0 move.
                for (int first = 0; first < order.Length - 1;)
                {
                    int last = first;
                    while (last < order.Length - 1 && body[order[last]].Operands[0] is
                        { Kind: BeamOperandParameters.Y, TypeIndex: null })
                    {
                        last++;
                    }

                    if (last - first > 1)
                    {
                        int[] sorted = order[first..last].Reverse()
                            .OrderBy(index => body[index].Operands[0].Value)
                            .ToArray();
                        sorted.CopyTo(order, first);
                    }

                    first = Math.Max(last, first + 1);
                }
            }

            WriteMoves(start, end, order);
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();

        bool WriteProjectedCopies(int start, int end, int arity)
        {
            if (start < 2 || body[start - 2] is not
                {
                    Definition.Opcode: Move,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } source,
                    { Kind: BeamOperandParameters.X, TypeIndex: null } scratch
                    ]
                }
                || body[start - 1] is not
                {
                    Definition.Opcode: GetTupleElement,
                    Operands: [
                    { Kind: BeamOperandParameters.X, TypeIndex: null } tuple,
                        _,
                    { Kind: BeamOperandParameters.Y, TypeIndex: null } field
                    ]
                }
                || source == tuple || source.Value >= arity || tuple.Value >= arity || scratch.Value < arity
                || body[start].Operands is not [var fieldInput, var fieldOutput]
                || fieldInput != field || fieldOutput != source
                || body[start + 1].Operands is not [var savedInput, var savedOutput]
                || savedInput != scratch || savedOutput != tuple)
            {
                return false;
            }

            // The projection must read the original tuple before X is reused.
            // Its write to Y leaves the original argument in X until both copies.
            output.Write(code.Span[copied..body[start - 2].Offset]);
            output.Write(code.Span[body[start - 1].Offset..body[start].Offset]);
            output.WriteByte(Move);
            BeamCompactWriter.Write(output, XRegisterTag, source.Value);
            BeamCompactWriter.Write(output, XRegisterTag, tuple.Value);
            output.Write(code.Span[body[start].Offset..body[start + 1].Offset]);
            copied = body[end].Offset;

            return true;
        }

        void WriteMoves(int start, int end, int[] order)
        {
            if (order.SequenceEqual(Enumerable.Range(start, end - start)))
            {
                return;
            }

            output.Write(code.Span[copied..body[start].Offset]);
            foreach (int index in order)
            {
                output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
            }

            copied = body[end].Offset;
        }
    }

    private static int? FindOutgoingResultSave(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int restore,
        int arity
    )
    {
        if (body[restore].Operands is not
            [
            { Kind: BeamOperandParameters.X, TypeIndex: null, Value: var scratch },
            { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true }
            ] || scratch.IsZero || scratch < arity)
        {
            return null;
        }

        // Ordinary calls only consume X0 through X(arity-1). No move between
        // the save and the final restore can read or overwrite either register.
        for (int index = restore - 1; index >= start; index--)
        {
            BeamOperand source = body[index].Operands[0];
            BeamOperand destination = body[index].Operands[1];
            if (source.Kind == BeamOperandParameters.X && (source.Value.IsZero || source.Value == scratch)
                || destination.Value.IsZero || destination.Value == scratch)
            {
                return source is { Kind: BeamOperandParameters.X, TypeIndex: null, Value.IsZero: true }
                    && destination.TypeIndex is null && destination.Value == scratch ? index : null;
            }
        }

        return null;
    }

    private static int[]? ReadCallResultLiteralMoveOrder(
        IReadOnlyList<BeamInstruction> body,
        int start,
        int call,
        int arity
    )
    {
        if (start == 0 || body[start - 1].Definition.Opcode is not (LocalCall or ExternalCall)
            || body[call].Definition.Opcode is not (LocalCall or ExternalCall))
        {
            return null;
        }

        var byDestination = new int[arity];
        Array.Fill(byDestination, -1);
        bool copiedResult = false;
        for (int index = start; index < start + arity; index++)
        {
            BeamOperand source = body[index].Operands[0];
            BeamOperand destination = body[index].Operands[1];
            if (destination.Value >= arity || byDestination[(int)destination.Value] >= 0)
            {
                return null;
            }

            if (source is { Kind: BeamOperandParameters.X, Value.IsZero: true, TypeIndex: null })
            {
                if (copiedResult || destination.Value.IsZero || byDestination[0] >= 0)
                {
                    return null;
                }

                copiedResult = true;
            }
            else if (!IsNonRegisterValue(source))
            {
                return null;
            }

            byDestination[(int)destination.Value] = index;
        }

        if (!copiedResult)
        {
            return null;
        }

        // The ordered move chain leaves the replacement of X0 last, after its
        // only read. Literal arguments have no dependencies on the chain.
        int[] order = IndependentCallMoveOrder(arity);
        for (int index = 0; index < order.Length; index++)
        {
            order[index] = byDestination[order[index]];
        }

        return order;
    }

    private static int[]? ReadNestedTailCallMoveOrder(IReadOnlyList<BeamInstruction> body, int start, int call)
    {
        if (start == 0 || body[start - 1].Definition.Opcode is not (LocalCall or ExternalCall)
            || body[call].Definition.Opcode is not (TailLocalCallWithFrame or TailExternalCallWithFrame)
            || body[start].Operands is not
                [{ Kind: BeamOperandParameters.X, Value.IsZero: true }, { Kind: BeamOperandParameters.X, Value: var result }] || result != 2
            || body[start + 1].Operands is not
                [{ Kind: BeamOperandParameters.Atom or BeamOperandParameters.Integer or BeamOperandParameters.LiteralValue or BeamOperandParameters.Nil },
                { Kind: BeamOperandParameters.X, Value.IsZero: true }]
            || body[start + 2].Operands is not
                [{ Kind: BeamOperandParameters.Y }, { Kind: BeamOperandParameters.X, Value: var saved }] || saved != 1)
        {
            return null;
        }

        // OTP order_moves emits the separate Y load before the chain that reads and replaces X0.
        return [start + 2, start, start + 1];
    }

    private static bool CallFollowsFrameTrim(IReadOnlyList<BeamInstruction> body, int call)
    {
        int previous = call - 1;
        while (previous >= 0 && body[previous].Definition.Opcode is (Line or Move or Swap))
        {
            previous--;
        }

        // OTP preserves argument preparation through a frame trim.
        return previous >= 0 && body[previous].Definition.Opcode == TrimFrame;
    }
}
