using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosedBooleanListResult(
            EntrySequenceBody sequence,
            (Pattern.Variable Variable, int Register)[] inputs,
            IReadOnlyList<Pattern> entryPatterns
        )
        {
            if (!CanUseEntryTypeFacts() || recursiveClosure is not null || owner.receiveScratchRegisters is not null
                || decoderSlotCount != 0 || parentFunction is not { } function
                || entryPatterns is not [Pattern.Literal { Value: Nil } or Pattern.List { Items: [], Tail: null }, ..]
                || inputs.Length != function.Arity - 1
                || inputs.Any(input => input.Register <= 0 || input.Register >= function.Arity)
                || sequence.Binding is not { Name: not VariableScopeNames.Wildcard } result
                || sequence.Call.Call.DynamicRemote || owner.ResolvesErlangBif(sequence.Call.Call)
                || sequence.Continuation is not Expr.Tuple
                {
                    Items: [
                        Expr.If
                    {
                        Clauses: [
                        {
                            Patterns: [], Guard: Expr.Variable condition, Body: Expr.List
                            { Items: [Expr.Tuple, ..], Tail: Expr.Variable tail } accepted
                        },
                        {
                            Patterns: [], Guard: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } },
                            Body: Expr.Variable otherwise
                        }
                            ]
                    },
                        Expr.Variable returned
                    ]
                }
                || condition.Name != returned.Name || tail.Name != otherwise.Name
                || !inputs.Any(input => input.Variable.Name == condition.Name)
                || !inputs.Any(input => input.Variable.Name == tail.Name)
                || !ReadClosedBooleanParameter(function, inputs.First(input => input.Variable.Name == condition.Name).Register))
            {
                return false;
            }

            var saved = inputs.Where(input => sequence.Used.Contains(input.Variable.Name))
                .OrderByDescending(input => input.Register).ToArray();
            var slots = saved.Select((input, slot) => (input.Variable.Name, Slot: slot))
                .ToDictionary(input => input.Name, input => input.Slot, StringComparer.Ordinal);
            if (!slots.TryGetValue(condition.Name, out int selector) || !slots.ContainsKey(tail.Name))
            {
                return false;
            }

            var positive = new Expr.Tuple([accepted, new Expr.Literal(Term.A(ErlangBooleanAtoms.True))]);
            var negative = new Expr.Tuple([otherwise, new Expr.Literal(Term.A(ErlangBooleanAtoms.False))]);
            var callResult = new Dictionary<string, int>(StringComparer.Ordinal) { [result.Name] = 0 };
            if (ReadCallArgumentConstructions(
                [positive],
                -1,
                slots,
                inputs: callResult,
                stageConsOperands: true
            ) is not { Live: 1, Moves.Count: 0, Loads.Count: 0, Constants.Count: 0 } success
                || ReadCallArgumentConstructions(
                    [negative],
                    -1,
                    slots,
                    inputs: new Dictionary<string, int>()
                ) is not { Live: 0 } failure)
            {
                return false;
            }

            int frame = saved.Length;
            peakSlot = Math.Max(peakSlot, frame);
            EmitFrameAllocation(Math.Max(sequence.Live, saved[0].Register + 1), frame);
            for (int slot = 0; slot < frame; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    saved[slot].Register,
                    YRegisterTag,
                    slot
                );
            }

            EmitEntryCall(sequence.Call);
            int rejected = Label();
            owner.Op(ExactEqualTest, LabelTag, rejected);
            owner.Operand(YRegisterTag, selector);
            owner.LiteralOperand(Term.A(ErlangBooleanAtoms.True));
            EmitCallArgumentConstructions(success, -1);
            owner.EmitFrameReturn(frame);

            Mark(rejected);
            EmitCallArgumentConstructions(failure, -1);
            owner.EmitFrameReturn(frame);

            return true;
        }

        private bool ReadClosedBooleanParameter(FunctionDefinition target, int index)
        {
            var signature = (target.Name, target.Arity);
            if (owner.exportedFunctions.Contains(signature) || owner.IsDeclaredNative(signature)
                || !localFunctions.ContainsKey(signature))
            {
                return false;
            }

            bool initialized = false;
            foreach (FunctionDefinition caller in localFunctions.Values)
            {
                bool recursive = (caller.Name, caller.Arity) == signature;
                foreach (Clause clause in caller.Clauses)
                {
                    foreach (Expr expression in LocalFunctionEntries.ClauseExpressions([clause])
                        .SelectMany(LocalFunctionEntries.Walk))
                    {
                        if (expression is Expr.FunctionReference reference && (reference.Name, reference.Arity) == signature)
                        {
                            return false;
                        }

                        if (expression is not Expr.Call { Module: null, DynamicRemote: false } call
                            || (call.Function, call.Arguments.Count) != signature)
                        {
                            continue;
                        }

                        Expr argument = call.Arguments[index];
                        if (argument is Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True or ErlangBooleanAtoms.False } })
                        {
                            initialized |= !recursive;
                            continue;
                        }

                        // A direct self edge reaching the head binding cannot shadow it.
                        if (!recursive || !ReferenceEquals(call, clause.Body)
                            || clause.Patterns[index] is not Pattern.Variable { Name: not VariableScopeNames.Wildcard } parameter
                            || argument is not Expr.Variable forwarded || forwarded.Name != parameter.Name)
                        {
                            return false;
                        }
                    }
                }
            }

            return initialized;
        }
    }
}
