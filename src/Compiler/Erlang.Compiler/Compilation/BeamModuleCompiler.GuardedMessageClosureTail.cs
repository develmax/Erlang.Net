using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedMessageClosureTail(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || !entries.TryGetValue((function.Name, function.Arity), out int entry)
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable state],
                        Body: Expr.Receive
                        {
                            Timeout: null, After: null, Clauses:
                            [
                                {
                                    Patterns: [Pattern.Variable message],
                                    Guard: Expr.GuardAlternatives choices,
                                    Body: Expr.Sequence
                                    {
                                        Items:
                                        [
                                            Expr.Match
                                        {
                                            Pattern: Pattern.Variable produced,
                                            Value: Expr.Call { Arguments: [Expr.Fun { SelfName: null } callback] } submit
                                        },
                                            Expr.Match
                                        {
                                            Pattern: Pattern.Variable updated,
                                            Value: Expr.Call
                                            {
                                                Arguments: [Expr.Variable priorResult, Expr.Variable retainedState]
                                            } consume
                                        },
                                            Expr.Call
                                        {
                                            Module: null, DynamicRemote: false, Arguments: [Expr.Variable nextState]
                                        } recur
                                        ]
                                    }
                                }
                            ]
                        }
                    }
                ]
                || !DistinctBindings(
                    state,
                    message,
                    produced,
                    updated
                )
                || priorResult.Name != produced.Name || retainedState.Name != state.Name
                || nextState.Name != updated.Name || recur.Function != function.Name
                || ReadEntryCall(submit, [], preparedOperand: 0) is not { } firstCall
                || ReadEntryCall(consume, [produced.Name], [state.Name]) is not { } secondCall)
            {
                return false;
            }

            var keys = new SortedSet<Atom>(MapKeyOrder);
            foreach (Expr choice in choices.Items)
            {
                if (choice is not Expr.Binary { Operator: ErlangOperators.NumericEqual or ErlangOperators.ExactEqual } test)
                {
                    return false;
                }

                var (access, constant) = test.Left is Expr.Call ? (test.Left, test.Right) : (test.Right, test.Left);
                if (access is not Expr.Call
                    {
                        Function: AutoImportNames.Element, DynamicRemote: false,
                        Arguments: [Expr.Literal { Value: Integer index }, Expr.Variable input]
                    } read || !index.Value.IsOne || input.Name != message.Name || !owner.ResolvesErlangBif(read)
                    || constant is not Expr.Literal { Value: Atom key } || !keys.Add(key))
                {
                    return false;
                }
            }

            if (keys.Count < 2 || callback.Clauses.Any(clause => clause.Patterns.Count != 0))
            {
                return false;
            }

            var bindings = new Dictionary<string, Term>(StringComparer.Ordinal)
            {
                [state.Name] = Nil.Value,
                [message.Name] = Nil.Value
            };
            if (!ClosureCaptures.Capture(callback, bindings).Keys.ToHashSet(StringComparer.Ordinal).SetEquals(bindings.Keys))
            {
                return false;
            }

            int scan = Label();
            int accept = Label();
            int retry = Label();
            int wait = Label();

            EmitFrameAllocation(1, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            Mark(scan);
            owner.Op(LoopReceive, LabelTag, wait);
            owner.Operand(XRegisterTag, 0);

            // A failing element read cannot satisfy any of the atom alternatives.
            // Keep the received term in X0 so closure creation owns it without a Y spill.
            owner.Op(BifTwo, LabelTag, retry);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Element, 2));
            owner.LiteralOperand(new Integer(1));
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitMembershipSelect(1, retry, keys.Select(key => ((Term)key, accept)));
            Mark(accept);
            owner.EmitClosureAllocation(2, 1, 1);
            owner.instructions.WriteByte(RemoveMessage);

            var oldVariables = variables;
            var oldOrder = entryCaptureOrder;
            variables = new(StringComparer.Ordinal) { [state.Name] = 0 };
            entryCaptureOrder = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [state.Name] = 0,
                [message.Name] = 1
            };
            EmitGeneralClosure(callback, heapChecked: true, x0Capture: message.Name);
            variables = oldVariables;
            entryCaptureOrder = oldOrder;

            EmitEntryCall(firstCall);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            EmitEntryCall(secondCall);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, 0);
            Mark(retry);
            owner.Op(LoopReceiveEnd, LabelTag, scan);
            Mark(wait);
            owner.Op(Wait, LabelTag, scan);

            return true;
        }
    }
}
