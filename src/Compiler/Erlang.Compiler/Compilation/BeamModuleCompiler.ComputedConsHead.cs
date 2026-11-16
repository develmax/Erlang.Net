using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record ConsHeadCall(
            EntryCall Call,
            int ResultSource,
            int[] Kept,
            bool ExtractTail,
            (int Register, int Field)? DeferredArgument = null
        );

        private ConsCallResult? ReadComputedConsHead(
            Expr head,
            Expr.Call tail,
            string[] inputs,
            int directHead,
            int listInput
        )
        {
            Expr.Call? first = head as Expr.Call;
            EntryConstructedArgument? tuple = null;
            int resultSource = inputs.Length;
            int[] kept = [];
            if (head is Expr.Tuple expression && expression.Items.OfType<Expr.Call>().ToArray() is [var field])
            {
                first = field;
                const string resultName = "\0cons_head_result";
                var rewritten = expression with
                {
                    Items = expression.Items.Select(item => ReferenceEquals(item, field)
                        ? new Expr.Variable(resultName) : item).ToArray()
                };
                tuple = ReadEntryConstructedArgument(rewritten, 1, [.. inputs, resultName]);
                if (tuple is null)
                {
                    return null;
                }

                kept = tuple.Operands.Where(operand => operand.Source.HasValue && operand.Source != resultSource)
                    .Select(operand => operand.Source!.Value).Distinct().ToArray();
            }

            if (first is null || ReadEntryCall(tail, [], inputs) is not { } finish)
            {
                return null;
            }

            int[] saved = (finish.SavedInputs ?? []).Select(move => move.Source).Distinct().Union(kept).ToArray();
            bool extractTail = directHead >= 0 && saved.Contains(listInput) && !saved.Contains(directHead)
                && first.Arguments.OfType<Expr.Variable>().Any(argument => argument.Name == inputs[directHead])
                && !first.Arguments.OfType<Expr.Variable>().Any(argument => argument.Name == inputs[listInput]);
            if (extractTail)
            {
                inputs = (string[])inputs.Clone();
                inputs[listInput] = inputs[directHead];
                inputs[directHead] = VariableScopeNames.Wildcard;
            }

            return ReadEntryCall(first, inputs) is { } start
                ? new(finish, saved, null, tuple, false, HeadCall: new(start, resultSource, kept, extractTail))
                : null;
        }

        private void EmitComputedConsHead(
            ConsCallResult result,
            ConsHeadCall head,
            int live,
            Action? emitBindings,
            int listInput,
            BeamSourceLocation? previous
        )
        {
            int[] saved = result.Saved;
            var tail = result.Call;
            int resultSlot = Array.FindLastIndex(saved, source => !head.Kept.Contains(source));
            bool dedicatedSlot = resultSlot < 0;
            if (dedicatedSlot)
            {
                resultSlot = saved.Length;
            }

            int frame = saved.Length + (dedicatedSlot ? 1 : 0);
            EmitFrameAllocation(live, frame);
            if (dedicatedSlot)
            {
                EmitFrameInitialization(resultSlot, frame);
            }

            if (!head.ExtractTail)
            {
                emitBindings?.Invoke();
            }

            for (int slot = 0; slot < saved.Length; slot++)
            {
                int source = saved[slot];
                if (!head.ExtractTail || source != listInput)
                {
                    owner.MoveRegister(XRegisterTag, source, YRegisterTag, slot);
                }
            }

            if (head.ExtractTail)
            {
                owner.EmitListParts(listInput, XRegisterTag, listInput, YRegisterTag, Array.IndexOf(saved, listInput));
            }

            if (head.DeferredArgument is { } deferred)
            {
                owner.EmitTuplePart(deferred.Register, deferred.Field, 0);
            }

            EmitEntryCall(head.Call, previous);
            int trimmed = head.Kept.Select(source => Array.IndexOf(saved, source)).Append(resultSlot).Min();
            var moves = (tail.SavedInputs ?? []).Select(move =>
                (move.Destination, Slot: Array.IndexOf(saved, move.Source))).ToArray();
            bool swap = moves.Contains((0, resultSlot));
            bool hasFirstArgument = false;
            foreach (var (destination, slot) in moves)
            {
                if (destination == 0)
                {
                    hasFirstArgument = true;
                    if (!swap)
                    {
                        owner.MoveRegister(YRegisterTag, slot, XRegisterTag, tail.Call.Arguments.Count);
                    }
                }
                else
                {
                    owner.MoveRegister(YRegisterTag, slot, XRegisterTag, destination);
                }
            }

            if (swap)
            {
                owner.Op(Swap, YRegisterTag, resultSlot);
                owner.Operand(XRegisterTag, 0);
            }
            else
            {
                owner.MoveRegister(XRegisterTag, 0, YRegisterTag, resultSlot);
            }

            if (trimmed != 0)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, trimmed);
                owner.Operand(BeamWire.UnsignedTag, frame - trimmed);
            }

            if (!swap && hasFirstArgument)
            {
                owner.MoveRegister(XRegisterTag, tail.Call.Arguments.Count, XRegisterTag, 0);
            }

            owner.EmitOrderedTailMoves(tail.Moves, tail.Constants);
            owner.EmitCallLine(tail.Call, CallLocation(head.Call.Call, location));
            Call(tail.Module, tail.Call.Function, tail.Call.Arguments.Count);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords + (result.Tuple?.HeapWords ?? 0));
            owner.Operand(BeamWire.UnsignedTag, 1);

            if (result.Tuple is { } tuple)
            {
                owner.EmitTupleHeader(XRegisterTag, tuple.Operands.Length, tuple.Register);
                foreach (var (source, constant) in tuple.Operands)
                {
                    if (source is int register)
                    {
                        int slot = register == head.ResultSource ? resultSlot : Array.IndexOf(saved, register);
                        owner.Operand(YRegisterTag, slot - trimmed);
                    }
                    else
                    {
                        owner.LiteralOperand(constant!);
                    }
                }

                owner.Op(PutList, XRegisterTag, tuple.Register);
            }
            else
            {
                owner.Op(PutList, YRegisterTag, resultSlot - trimmed);
            }

            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame - trimmed);
        }
    }
}
