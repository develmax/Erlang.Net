using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitAtomEntryListResult(FunctionDefinition function, int functionInfo)
        {
            // Measure and emit the same branch layout using the original source facts.
            BeamModuleCompiler facts = owner.layoutProbeSource ?? owner;
            if (facts.ModuleOptimizationDisabled || !facts.SsaTypeInferenceEnabled || facts.OnLoadFunction is not null
                || function.Arity != 3 || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns:
                        [
                            Pattern.Literal { Value: Atom first },
                            Pattern.Variable unusedInput,
                            Pattern.Variable unusedTail
                        ],
                    Body: var empty
                },
                {
                    Guard: null,
                    Patterns:
                        [
                            Pattern.Literal { Value: Atom second },
                            Pattern.Variable input,
                            Pattern.Variable tail
                        ],
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: null,
                            DynamicRemote: false,
                            Arguments:
                                [
                                    Expr.Variable selected,
                                    Expr.List { Items: [var argumentHead], Tail: Expr.Variable argumentTail }
                                ]
                        } producer,
                        Clauses:
                            [
                            { Guard: null, Patterns: [var nilPattern], Body: var nilResult },
                            {
                                Guard: null,
                                Patterns: [Pattern.Variable result],
                                Body: Expr.List { Items: [var resultHead], Tail: Expr.Variable resultTail }
                            }
                            ]
                    }
                }
                ]
                || !IsNilExpression(empty) || !DistinctBindings(input, tail, result)
                || unusedInput.Name != VariableScopeNames.Wildcard && unusedInput.Name == unusedTail.Name
                || first.Equals(second) || ReadAtomEntryType(function, 0) != 0
                || selected.Name != input.Name || argumentTail.Name != tail.Name
                || !IsEmptyListPattern(nilPattern) || !IsNilExpression(nilResult) || resultTail.Name != result.Name
                || !AttributeLiteral.TryNormalizeConstant(argumentHead, out Term argumentValue)
                || !AttributeLiteral.TryNormalizeConstant(resultHead, out Term resultValue))
            {
                return false;
            }

            var signature = (producer.Function, producer.Arguments.Count);
            if (!(facts.properListSourceFunctions?.TryGetValue(signature, out var definition) == true)
                || definition.IsInlineFailure || facts.IsDeclaredNative(signature)
                || !entries.TryGetValue(signature, out int callee)
                || !(definition.HasProperListResult || facts.ReadPrivateEntryFacts()?.ProvesProperListResult(signature) == true))
            {
                return false;
            }

            // Only the selected call arm has a frame. Neither original
            // input is needed after construction of its outgoing arguments.
            bool emptyFirst = TermOrder.Compare(first, second, exact: true) > 0;
            int firstLabel = Label();
            int secondLabel = Label();
            int emptyLabel = emptyFirst ? firstLabel : secondLabel;
            int callLabel = emptyFirst ? secondLabel : firstLabel;
            owner.EmitMembershipSelect(
                0,
                functionInfo,
                emptyFirst ? [(second, callLabel), (first, emptyLabel)] : [(first, emptyLabel), (second, callLabel)]
            );
            if (emptyFirst)
            {
                EmitEmptyArm();
                EmitCallArm();
            }
            else
            {
                EmitCallArm();
                EmitEmptyArm();
            }

            return true;

            void EmitEmptyArm()
            {
                Mark(emptyLabel);
                owner.Op(Move, AtomTag, NilAtomIndex);
                owner.Operand(XRegisterTag, 0);
                owner.instructions.WriteByte(Return);
            }

            void EmitCallArm()
            {
                Mark(callLabel);
                owner.Op(AllocateHeap, BeamWire.UnsignedTag, 0);
                owner.Operand(BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, function.Arity);
                owner.MoveRegister(
                    XRegisterTag,
                    1,
                    XRegisterTag,
                    0
                );
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(argumentValue);
                owner.Operand(XRegisterTag, 2);
                owner.Operand(XRegisterTag, 1);
                owner.EmitCallLine(producer, location);
                owner.Op(LocalCall, BeamWire.UnsignedTag, producer.Arguments.Count);
                owner.Operand(LabelTag, callee);

                int nonempty = Label();
                owner.Op(IsNil, LabelTag, nonempty);
                owner.EmitTypedRegister(0, ProperListTypeBits);
                owner.EmitFrameReturn(0);
                Mark(nonempty);

                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.instructions.WriteByte(PutList);
                owner.LiteralOperand(resultValue);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
                owner.EmitFrameReturn(0);
            }
        }
    }
}
