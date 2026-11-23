using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitListDefaultTupleContinuation(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Clauses is not
                [
                {
                    Patterns: [Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable list, Pattern.Variable retained] }],
                    Guard: Expr.Call { Function: AutoImportNames.IsList, Arguments: [Expr.Variable checkedList] } guard,
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                var effectExpression,
                                Expr.Match
                            {
                                Pattern: Pattern.Variable normalized,
                                Value: Expr.Case
                                {
                                    Value: Expr.Call
                                    {
                                        Module: ListModuleNames.Module,
                                        Function: ListModuleNames.KeyMember,
                                        DynamicRemote: false,
                                        Arguments: [Expr.Literal key, Expr.Literal { Value: Integer position }, Expr.Variable inspected]
                                    } contains,
                                    Clauses:
                                        [
                                        {
                                            Guard: null,
                                            Patterns: [Pattern.Literal { Value: Atom { Name: "true" } yes }],
                                            Body: Expr.Variable present
                                        },
                                        {
                                            Guard: null,
                                            Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }],
                                            Body: Expr.List { Items: [var firstDefault], Tail: Expr.Variable absent }
                                        }
                                        ]
                                }
                            },
                                Expr.Match
                            {
                                Pattern: Pattern.Variable selected,
                                Value: Expr.Case
                                {
                                    Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable candidate] } filter,
                                    Clauses:
                                        [
                                        {
                                            Guard: null,
                                            Patterns: [Pattern.Literal { Value: Nil } or Pattern.List { Items: [], Tail: null }],
                                            Body: Expr.List { Items: [var secondDefault], Tail: Expr.Variable empty }
                                        },
                                        {
                                            Guard: null,
                                            Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                            Body: Expr.Variable nonempty
                                        }
                                        ]
                                }
                            },
                                Expr.Case
                            {
                                Value: Expr.Call { Arguments: [Expr.Variable input, Expr.Variable option] } consumer,
                                Clauses:
                                    [
                                    {
                                        Guard: null,
                                        Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom valueTag }, Pattern.Variable value] }],
                                        Body: Expr.Tuple
                                        {
                                            Items:
                                                [
                                                    Expr.Literal { Value: Atom outputTag },
                                                    Expr.Tuple { Items: [Expr.Variable delivered, Expr.Variable stored, Expr.Variable saved] }
                                                ]
                                        }
                                    },
                                    {
                                        Guard: null,
                                        Patterns: [Pattern.Variable other],
                                        Body: Expr.Variable returned
                                    }
                                    ]
                            }
                            ]
                    }
                }
                ]
                || !DistinctBindings(
                    first,
                    list,
                    retained,
                    normalized,
                    selected,
                    value,
                    other
                )
                || checkedList.Name != list.Name || !owner.ResolvesErlangBif(guard)
                || inspected.Name != list.Name || present.Name != list.Name || absent.Name != list.Name
                || candidate.Name != normalized.Name || empty.Name != normalized.Name || nonempty.Name != normalized.Name
                || input.Name != first.Name || option.Name != selected.Name
                || delivered.Name != value.Name || stored.Name != first.Name || saved.Name != retained.Name
                || returned.Name != other.Name
                || DiscardedCall(effectExpression) is not { Arguments: [var firstArg, var secondArg] } effect
                || !AttributeLiteral.TryNormalizeConstant(firstArg, out Term leftOperand)
                || !AttributeLiteral.TryNormalizeConstant(secondArg, out Term rightOperand)
                || !AttributeLiteral.TryNormalizeConstant(firstDefault, out Term missingOption)
                || !AttributeLiteral.TryNormalizeConstant(secondDefault, out Term emptyOption)
                || !TryResolveOrdinaryCallTarget(consumer, out string? consumerModule)
                || !entries.ContainsKey((filter.Function, 1))
                || !localFunctions.TryGetValue((filter.Function, 1), out var filterDefinition)
                || !(filterDefinition.HasProperListResult || filterDefinition.IsClosedListMapper))
            {
                return false;
            }

            if (!TryResolveOrdinaryCallTarget(effect, out string? effectModule))
            {
                if (effect.Function != AutoImportNames.ProcessFlag || !owner.ResolvesErlangBif(effect))
                {
                    return false;
                }

                effectModule = ErlangModuleNames.Module;
            }

            int functionInfo = owner.nextLabel - 2;
            int keyPresent = Label();
            int filterNonempty = Label();
            int optionsReady = Label();
            int reject = Label();
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                3,
                functionInfo
            );
            owner.EmitTuplePart(0, 1, 1);
            owner.Op(IsList, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, 1);
            EmitFrameAllocation(2, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );

            owner.EmitConstant(rightOperand, 1);
            owner.EmitConstant(leftOperand, 0);
            owner.EmitCallLine(effect, location);
            Call(effectModule, effect.Function, 2);
            var previous = CallLocation(effect, location);
            owner.EmitConstant(position, 1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.EmitConstant(key.Value, 0);
            owner.EmitCallLine(contains, previous);
            Call(contains.Module, contains.Function, 3);
            previous = CallLocation(contains, previous);
            owner.Op(ExactNotEqualTest, LabelTag, keyPresent);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(yes);
            EmitDefault(missingOption, 0);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            Mark(keyPresent);

            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(filter, previous);
            Call(null, filter.Function, 1);
            previous = CallLocation(filter, previous);
            owner.Op(IsNil, LabelTag, filterNonempty);
            owner.EmitTypedRegister(0, ProperListTypeBits);
            EmitDefault(emptyOption, 1);
            JumpTo(optionsReady);
            Mark(filterNonempty);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            Mark(optionsReady);

            // Retain the checked tuple until each field is used. The normalized
            // list slot becomes available for the consumer's saved argument.
            EmitSavedTuplePart(
                1,
                0,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(consumer, previous);
            Call(consumerModule, consumer.Function, 2);
            owner.EmitRecordTest(
                reject,
                XRegisterTag,
                0,
                valueTag,
                2
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4 + 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, 1, 0);
            EmitSavedTuplePart(
                1,
                2,
                XRegisterTag,
                1
            );
            owner.EmitTagMatchTuple(
                0,
                (XRegisterTag, 0),
                (YRegisterTag, 0),
                (XRegisterTag, 1)
            );
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(outputTag.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(2);
            Mark(reject);
            owner.EmitFrameReturn(2);

            return true;

            void EmitDefault(Term constant, int destination)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 0);
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(constant);
                owner.Operand(YRegisterTag, 0);
                owner.Operand(XRegisterTag, destination);
            }
        }
    }
}
