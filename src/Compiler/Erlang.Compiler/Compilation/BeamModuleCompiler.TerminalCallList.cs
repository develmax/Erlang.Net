using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTerminalCallList(FunctionDefinition function)
        {
            (Clause? Clause, (int Input, EntryCall Tail)? Empty) entry = function.Clauses switch
            {
                [var single] => (single, null),
                [var fast, var fallback] when ReadTerminalListNilArm(fast) is { } plan => (fallback, plan),
                _ => (null, null)
            };
            var (clause, empty) = entry;
            if (clause is not { Guard: null, Body: Expr.List { Tail: null } list }
                || !HasUnconditionalParameters(clause) || list.Items.Count < 2
                || list.Items[0] is not Expr.Call first)
            {
                return false;
            }

            var inputs = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (ReadEntryCall(first, inputs) is not { } firstCall)
            {
                return false;
            }

            var laterCalls = list.Items.Skip(1).OfType<Expr.Call>().ToArray();
            var retainedItems = list.Items.OfType<Expr.Variable>().ToArray();
            if (laterCalls.Length + retainedItems.Length != list.Items.Count - 1
                || retainedItems.Any(item => item.Name == VariableScopeNames.Wildcard || !inputs.Contains(item.Name)))
            {
                return false;
            }

            var lastReads = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < laterCalls.Length; index++)
            {
                foreach (var variable in laterCalls[index].Arguments.OfType<Expr.Variable>())
                {
                    lastReads[variable.Name] = index;
                }
            }

            // List operands remain live after the last call.
            foreach (var item in retainedItems)
            {
                lastReads[item.Name] = laterCalls.Length;
            }

            var savedInputs = inputs.Select((name, register) => (Name: name, Register: register))
                .Where(input => lastReads.ContainsKey(input.Name)).ToArray();
            int frame = savedInputs.Length;
            for (int index = 0; index < laterCalls.Length; index++)
            {
                frame = Math.Max(frame, index + 1 + lastReads.Values.Count(last => last > index));
            }
            var stack = Enumerable.Repeat(VariableScopeNames.Wildcard, frame).ToArray();
            for (int index = 0; index < savedInputs.Length; index++)
            {
                stack[frame - 1 - index] = savedInputs[index].Name;
            }

            var steps = new (EntryCall Call, int Slot, bool Swap)[laterCalls.Length];
            for (int index = 0; index < laterCalls.Length; index++)
            {
                if (ReadEntryCall(laterCalls[index], [], stack) is not { } plan)
                {
                    return false;
                }

                int slot = plan.SavedInputs is [{ Destination: 0, Source: var source }, ..] loads
                    && loads.Count(load => load.Source == source) == 1 && lastReads[stack[source]] == index
                    ? source : -1;
                bool swap = slot >= 0;
                if (!swap)
                {
                    slot = Array.LastIndexOf(stack, VariableScopeNames.Wildcard);
                    if (slot < 0)
                    {
                        return false;
                    }
                }
                steps[index] = (plan, slot, swap);
                for (int saved = 0; saved < stack.Length; saved++)
                {
                    if (lastReads.GetValueOrDefault(stack[saved], -1) == index)
                    {
                        stack[saved] = VariableScopeNames.Wildcard;
                    }
                }
                // An earlier result remains live through every later call.
                stack[slot] = string.Empty;
            }

            if (empty is { } fastPath)
            {
                int nonempty = Label();
                owner.EmitNilTest(XRegisterTag, fastPath.Input, nonempty);
                EmitEntryCallArguments(fastPath.Tail);
                Expr.Call tail = fastPath.Tail.Call;
                EmitTailLocal(entries[(tail.Function, tail.Arguments.Count)], tail.Arguments.Count, null);
                Mark(nonempty);
            }

            int live = Math.Max(EntryCallLiveCount(firstCall, inputs),
                savedInputs.Select(input => input.Register + 1).DefaultIfEmpty(0).Max());
            EmitGeneralFrame(live, initializedByMoves: savedInputs.Length, slots: frame);
            for (int index = savedInputs.Length - 1; index >= 0; index--)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    savedInputs[index].Register,
                    YRegisterTag,
                    frame - 1 - index
                );
            }
            EmitEntryCall(firstCall);
            var previous = CallLocation(first, location);
            foreach (var (plan, slot, swap) in steps)
            {
                if (swap)
                {
                    owner.Op(Swap, YRegisterTag, slot);
                    owner.Operand(XRegisterTag, 0);
                }
                else
                {
                    owner.MoveRegister(
                        XRegisterTag,
                        0,
                        YRegisterTag,
                        slot
                    );
                }
                var loads = swap ? plan.SavedInputs!.Skip(1).ToList() : plan.SavedInputs;
                owner.EmitOrderedTailMoves(plan.Moves, plan.Constants, loads);
                owner.EmitCallLine(plan.Call, previous);
                Call(plan.Module, plan.Call.Function, plan.Call.Arguments.Count);
                previous = CallLocation(plan.Call, previous);
            }

            owner.Op(TestHeap, BeamWire.UnsignedTag, list.Items.Count * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            int callIndex = steps.Length;
            int tailRegister = 1;
            bool emptyTail = true;
            for (int index = list.Items.Count - 1; index >= 0; index--)
            {
                owner.instructions.WriteByte(PutList);
                if (list.Items[index] is Expr.Variable variable)
                {
                    int inputSlot = Array.IndexOf(stack, variable.Name);
                    owner.Operand(YRegisterTag, inputSlot);
                }
                else if (callIndex == steps.Length)
                {
                    owner.Operand(XRegisterTag, 0);
                }
                else
                {
                    owner.Operand(YRegisterTag, steps[callIndex].Slot);
                }

                if (emptyTail)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, tailRegister);
                }

                if (list.Items[index] is Expr.Call)
                {
                    tailRegister = 0;
                    callIndex--;
                }

                owner.Operand(XRegisterTag, tailRegister);
                emptyTail = false;
            }
            owner.EmitFrameReturn(frame);

            return true;
        }

        private (int Input, EntryCall Tail)? ReadTerminalListNilArm(Clause clause)
        {
            if (clause is not { Guard: null, Body: Expr.Call tail })
            {
                return null;
            }

            int nil = -1;
            var inputs = new string[clause.Patterns.Count];
            for (int register = 0; register < inputs.Length; register++)
            {
                switch (clause.Patterns[register])
                {
                    case Pattern.List { Items: [], Tail: null } or Pattern.Literal { Value: Nil } when nil < 0:
                        nil = register;
                        break;
                    case Pattern.Variable { Name: not VariableScopeNames.Wildcard } variable when !inputs.Contains(variable.Name):
                        inputs[register] = variable.Name;
                        break;
                    case Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                    case Erlang.Pattern.Any:
                        break;
                    default:
                        return null;
                }
            }

            return nil >= 0 && ReadEntryCall(tail, inputs) is { Module: null } call ? (nil, call) : null;
        }
    }
}
