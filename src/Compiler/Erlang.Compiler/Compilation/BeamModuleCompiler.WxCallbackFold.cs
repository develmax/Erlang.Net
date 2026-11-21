using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitWxCallbackFoldCommand(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not [
                {
                    Patterns: [Pattern.Variable text, Pattern.Variable callback, Pattern.Variable options],
                    Guard: { } guard,
                    Body: Expr.Sequence
                    {
                        Items: [
                            Expr.Match { Pattern: Pattern.Variable callbackId, Value: Expr.Call registration },
                            Expr.Match { Pattern: Pattern.Variable mapper, Value: Expr.Fun { SelfName: null } lambda },
                            Expr.Match
                        {
                            Pattern: Pattern.Variable convertedOptions,
                            Value: Expr.Call
                            {
                                Module: "lists", Function: "foldl", DynamicRemote: false,
                                Arguments: [Expr.Variable folder, var initial, Expr.Variable foldOptions]
                            } fold
                        },
                            Expr.Match
                        {
                            Pattern: Pattern.Variable convertedText,
                            Value: Expr.Call
                            {
                                Module: "unicode", Function: "characters_to_binary", DynamicRemote: false,
                                Arguments: [Expr.Variable sourceText]
                            } convert
                        },
                            Expr.Match
                        {
                            Pattern: Pattern.Variable command,
                            Value: Expr.Literal { Value: Integer commandNumber }
                        },
                            Expr.Call
                        {
                            Module: "wxe_util", Function: "queue_cmd", DynamicRemote: false,
                            Arguments: [
                                    Expr.Variable queuedText, Expr.Variable queuedCallback, Expr.Variable queuedOptions,
                                    Expr.Case environment, Expr.Variable queuedCommand
                                ]
                        } queue,
                            Expr.Call
                        {
                            Module: "wxe_util", Function: "rec", DynamicRemote: false,
                            Arguments: [Expr.Variable receivedCommand]
                        } receive
                        ]
                    }
                } clause]
                || !HasUnconditionalParameters(clause)
                || !IsWxCallbackRegistration(registration, callback.Name)
                || !HasFreshWxFoldBindings(
                    environment,
                    text,
                    callback,
                    options,
                    callbackId,
                    mapper,
                    convertedOptions,
                    convertedText,
                    command
                )
                || folder.Name != mapper.Name || foldOptions.Name != options.Name
                || initial is not (Expr.Literal { Value: Nil } or Expr.List { Items.Count: 0, Tail: null })
                || sourceText.Name != text.Name || queuedText.Name != convertedText.Name
                || queuedCallback.Name != callbackId.Name || queuedOptions.Name != convertedOptions.Name
                || queuedCommand.Name != command.Name || receivedCommand.Name != command.Name
                || ReadWxCallbackFoldArms(lambda.Clauses) is null
                || ReadForwardGuard(guard, [text.Name, callback.Name, options.Name]) is not ForwardGuard.Conjunction
                {
                    Left: ForwardGuard.Conjunction
                    {
                        Left: ForwardGuard.Test { Opcode: IsList, Register: 0 },
                        Right: ForwardGuard.Test { Opcode: IsFunction, Register: 1 }
                    },
                    Right: ForwardGuard.Test { Opcode: IsList, Register: 2 }
                })
            {
                return false;
            }

            int functionFailure = owner.nextLabel - 2;
            int missingEnvironment = Label();
            EmitWxFoldTypeTest(IsList, 0, functionFailure);
            EmitWxFoldTypeTest(IsFunction, 1, functionFailure);
            EmitWxFoldTypeTest(IsList, 2, functionFailure);

            // Keep only the options and title across callback registration.
            EmitFrameAllocation(3, 2);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            EmitWxCallbackFoldCall(registration, location);

            owner.instructions.WriteByte(TestHeap);
            owner.EmitClosureHeap(0, 1, 1);
            EmitWxOptionMapper(lambda, 1, arity: 2);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitConstant(Nil.Value, 1);
            EmitWxCallbackFoldCall(fold, CallLocation(registration, location));

            // The conversion consumes the title while the fold result takes its slot.
            owner.Op(Swap, YRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            EmitWxCallbackFoldCall(convert, CallLocation(fold, location));
            EmitWxEnvironmentCheck(1, missingEnvironment);
            owner.EmitTuplePart(1, 1, 3);
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.EmitConstant(commandNumber, 4);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 0);
            EmitWxCallbackFoldCall(queue, CallLocation(convert, location));

            owner.EmitConstant(commandNumber, 0);
            owner.EmitCallLine(receive, CallLocation(queue, location));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(receive.Module!, receive.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, 0);

            Mark(missingEnvironment);
            owner.EmitConstant(Term.Tuple(Term.A("wx"), Term.A("unknown_env")), 0);
            owner.EmitCallLine((Expr.Call)environment.Clauses[1].Body, CallLocation(receive, location));
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, "error", 1));
            owner.Operand(BeamWire.UnsignedTag, 2);

            return true;
        }

        private bool TryEmitWxCallbackFoldClosure(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Arity != 2
                || ReadWxCallbackFoldArms(function.Clauses) is not { } arms)
            {
                return false;
            }

            int failure = owner.nextLabel - 2;
            var branches = arms.OrderByDescending(arm => arm.Tag).Select(arm => (arm.Tag, arm.Call, Label: Label())).ToArray();
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                2,
                failure
            );
            owner.EmitTuplePart(0, 0, 2);
            owner.EmitTuplePart(0, 1, 3);
            owner.Op(SelectValue, XRegisterTag, 2);
            owner.Operand(LabelTag, failure);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, branches.Length * 2);
            foreach (var branch in branches.Reverse())
            {
                owner.LiteralOperand(branch.Tag);
                owner.Operand(LabelTag, branch.Label);
            }

            BeamSourceLocation previous = location;
            foreach (var branch in branches)
            {
                Mark(branch.Label);
                EmitWxFoldTypeTest(IsFunction, 3, failure);
                EmitFrameAllocation(4, 1);
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    YRegisterTag,
                    0
                );
                owner.MoveRegister(
                    XRegisterTag,
                    3,
                    XRegisterTag,
                    0
                );
                EmitWxCallbackFoldCall(branch.Call, previous);
                previous = CallLocation(branch.Call, previous);
                owner.Op(TestHeap, BeamWire.UnsignedTag, 5);
                owner.Operand(BeamWire.UnsignedTag, 1);
                EmitWxOptionTupleValue(branch.Tag);
                owner.Op(PutList, XRegisterTag, 0);
                owner.Operand(YRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.EmitFrameReturn(1);
            }

            return true;
        }

        private List<(Atom Tag, Expr.Call Call)>? ReadWxCallbackFoldArms(IReadOnlyList<Clause> clauses)
        {
            if (clauses.Count < 2)
            {
                return null;
            }

            var tags = new HashSet<Atom>();
            var arms = new List<(Atom Tag, Expr.Call Call)>();
            foreach (var clause in clauses)
            {
                if (clause is not
                    {
                        Patterns: [
                            Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable callback] },
                            Pattern.Variable accumulator
                        ],
                        Guard: Expr.Call { Function: "is_function", Arguments: [Expr.Variable tested] } guard,
                        Body: Expr.List
                        {
                            Items: [Expr.Tuple { Items: [Expr.Literal { Value: Atom returnedTag }, Expr.Call registration] }],
                            Tail: Expr.Variable returnedAccumulator
                        }
                    }
                    || callback.Name != tested.Name || accumulator.Name != returnedAccumulator.Name
                    || callback.Name == accumulator.Name || callback.Name == VariableScopeNames.Wildcard
                    || accumulator.Name == VariableScopeNames.Wildcard || !tag.Equals(returnedTag) || !tags.Add(tag)
                    || !owner.ResolvesErlangBif(guard) || !IsWxCallbackRegistration(registration, callback.Name))
                {
                    return null;
                }

                arms.Add((tag, registration));
            }

            return arms;
        }

        private static bool HasFreshWxFoldBindings(Expr.Case environment, params Pattern.Variable[] variables)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            if (variables.Any(variable => variable.Name == VariableScopeNames.Wildcard || !names.Add(variable.Name)))
            {
                return false;
            }

            return UsesFreshWxEnvironment(environment, names);
        }

        private static bool IsWxCallbackRegistration(Expr.Call call, string callback) => call is
        {
            Module: "wxe_util", Function: "get_cbId", DynamicRemote: false,
            Arguments: [Expr.Variable passed]
        } && passed.Name == callback;

        private void EmitWxFoldTypeTest(byte opcode, int register, int failure)
        {
            owner.Op(opcode, LabelTag, failure);
            owner.Operand(XRegisterTag, register);
        }

        private void EmitWxCallbackFoldCall(Expr.Call call, BeamSourceLocation previous)
        {
            owner.EmitCallLine(call, previous);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, call.Arguments.Count);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(call.Module!, call.Function, call.Arguments.Count));
        }
    }
}
