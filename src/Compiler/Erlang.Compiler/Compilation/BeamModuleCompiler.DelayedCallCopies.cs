using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private readonly record struct DelayedCallCopy(bool FromStack, int Source, int Destination);

        private bool EmitDelayedCallCopies(
            SavedCallStep step,
            int[] remap,
            bool beforeFrameCleanup,
            out int delayedReceiver,
            bool allowExternal = false
        )
        {
            delayedReceiver = -1;
            var call = step.Call;
            int arity = call.Call.Arguments.Count;
            if (step.Spill is not int spill || step.Closure is not null || step.TupleArguments is not null
                || call.RegisterConstructions is not null || !allowExternal && call.Module is not null
                || call.SavedInputs is not { Count: > 0 } loads || call.Constants.Count > 1
                || loads.Count + call.Moves.Count + call.Constants.Count != arity)
            {
                return false;
            }

            var destinations = loads.Select(load => load.Destination)
                .Concat(call.Moves.Select(move => move.Destination))
                .Concat(call.Constants.Select(constant => constant.Destination));
            int stored = remap[spill];
            if (stored < 0 || loads.Any(load => remap[load.Source] < 0)
                || loads.Count(load => load.Source == spill) > 1
                || !destinations.Order().SequenceEqual(Enumerable.Range(0, arity)))
            {
                return false;
            }

            bool fromStack = loads.Any(load => load.Destination == 0);
            int scratch = fromStack
                ? Enumerable.Range(1, arity).First(register => loads.All(load => load.Destination != register))
                : -1;
            int forward = -1;
            if (call.Moves.Count != 0)
            {
                if (!fromStack || call.Constants.Count != 0 || call.Moves is not [(var destination, 0)] || destination != scratch)
                {
                    return false;
                }

                forward = destination;
            }

            // SSA delays the previous return-value copy until it has
            // reserved the stack-sourced arguments in reverse argument order.
            var pending = loads.OrderByDescending(load => load.Destination)
                .Select(load => new DelayedCallCopy(true, remap[load.Source], load.Destination == 0 ? scratch : load.Destination))
                .ToList();
            pending.Add(new DelayedCallCopy(false, 0, stored));

            // There is only one Y destination, and none of these copies
            // writes X0. The only dependency is a read of the old spill slot.
            var ordered = new List<DelayedCallCopy>(pending.Count);
            while (pending.Count != 0)
            {
                var copy = pending[0];
                pending.RemoveAt(0);
                if (copy.FromStack && copy.Source == stored)
                {
                    int spillCopy = pending.FindIndex(candidate => !candidate.FromStack);
                    if (spillCopy >= 0)
                    {
                        ordered.Insert(0, pending[spillCopy]);
                        pending.RemoveAt(spillCopy);
                    }
                }

                ordered.Insert(0, copy);
                pending.Reverse();
            }

            for (int index = 0; index < ordered.Count;)
            {
                if (!ordered[index].FromStack)
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        stored
                    );
                    index++;
                    continue;
                }

                int end = index + 1;
                while (end < ordered.Count && ordered[end].FromStack)
                {
                    end++;
                }

                // beam_block sorts each adjacent stack-to-X run by its Y source.
                foreach (var copy in ordered.GetRange(index, end - index).AsEnumerable().Reverse().OrderBy(copy => copy.Source))
                {
                    owner.MoveRegister(
                        YRegisterTag,
                        copy.Source,
                        XRegisterTag,
                        copy.Destination
                    );
                }

                index = end;
            }

            if (forward >= 0)
            {
                owner.Op(Swap, XRegisterTag, 0);
                owner.Operand(XRegisterTag, forward);
            }
            else
            {
                if (scratch >= 0 && call.Constants.Any(constant => constant.Destination == scratch))
                {
                    owner.MoveToX0(XRegisterTag, scratch);
                    scratch = -1;
                }

                owner.EmitIndependentCallMoves(call.Constants);
                if (scratch >= 0 && beforeFrameCleanup)
                {
                    delayedReceiver = scratch;
                }
                else if (scratch >= 0)
                {
                    owner.MoveToX0(XRegisterTag, scratch);
                }
            }

            return true;
        }
    }
}
