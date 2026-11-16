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
            if (count < 2 || count > arity
                || start > 0 && body[start - 1].Definition.Opcode == Move)
            {
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

            if (order.SequenceEqual(Enumerable.Range(start, count)))
            {
                continue;
            }

            output.Write(code.Span[copied..body[start].Offset]);
            foreach (int index in order)
            {
                output.Write(code.Span[body[index].Offset..body[index + 1].Offset]);
            }

            copied = body[end].Offset;
        }

        if (copied == 0)
        {
            return null;
        }

        output.Write(code.Span[copied..]);

        return output.ToArray();
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
