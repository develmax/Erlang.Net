using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSingletonTupleListReturns(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Arity != 1
                || function.Clauses is not
                [
                { Guard: null, Patterns: [var emptyPattern], Body: var emptyBody },
                { Guard: null, Patterns: [AliasPattern singleton], Body: Expr.Variable returned },
                {
                    Guard: null,
                    Patterns: [Pattern.Variable input],
                    Body: Expr.Call fallback
                }
                ]
                || ListEntryTest(emptyPattern) != IsNil
                || !AttributeLiteral.TryNormalizeConstant(emptyBody, out Term empty) || empty is not Nil
                || !TryReadPatternAlias(singleton, out var alias, out var shape)
                || alias.Name == VariableScopeNames.Wildcard || returned.Name != alias.Name
                || shape is not Pattern.List
                {
                    Items: [Pattern.Tuple { Items.Count: > 0 } tuple], Tail: null
                }
                || tuple.Items.Any(item => item is not global::Erlang.Pattern.Variable)
                || !DistinctBindings([alias, .. tuple.Items.Cast<Pattern.Variable>()
                    .Where(field => field.Name != VariableScopeNames.Wildcard)])
                || input.Name == VariableScopeNames.Wildcard
                || ReadEntryCallChain(fallback, [input.Name]) is not { } chain)
            {
                return false;
            }

            // Both fast paths return the original X0. The checked head and
            // suffix only occupy scratch registers, including on every retry.
            int nonCons = Label();
            int continueCall = Label();
            owner.EmitNonemptyList(0, nonCons);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.EmitTupleCheck(1, tuple.Items.Count, continueCall);
            owner.EmitNilTest(XRegisterTag, 2, continueCall);
            owner.instructions.WriteByte(Return);

            Mark(nonCons);
            owner.EmitNilTest(XRegisterTag, 0, continueCall);
            owner.instructions.WriteByte(Return);

            Mark(continueCall);
            EmitEntryCallChain(
                chain.First,
                [input.Name],
                chain.Continuation,
                chain.RelocateBeforeTrim
            );

            return true;
        }

        private bool TryEmitSingletonListCheckedCalls(FunctionDefinition function, int functionInfo)
        {
            if (function.Clauses is not [
                {
                    Patterns: [Pattern.Variable input], Body: Expr.Call
                    { Module: null, DynamicRemote: false, Arguments: [Expr.List { Items: [Expr.Variable wrapped], Tail: null }] } forward
                } first,
                {
                    Guard: null, Patterns: [Pattern.List { Items: [Pattern.Variable head], Tail: null }],
                    Body: Expr.Sequence
                    {
                        Items: [
                        Expr.Match { Pattern: Pattern.Variable binding, Value: Expr.Call read },
                        Expr.Case
                        {
                            Value: Expr.Call check, Clauses: [
                            {
                                Guard: null, Patterns: [Pattern.Literal accepted], Body: Expr.Case
                                {
                                    Value: Expr.Call open, Clauses: [
                                    {
                                        Guard: null, Patterns: [Pattern.Literal ready],
                                        Body: Expr.Sequence { Items: [Expr.Call finish, Expr.Literal success] }
                                    },
                                    { Guard: null, Patterns: [Pattern.Variable rejected], Body: Expr.Variable returned }
                                ]
                                }
                            },
                            { Guard: null, Patterns: [Pattern.Variable other], Body: var failure }
                        ]
                        }
                    ]
                    }
                }
            ] || !DistinctBindings(head, binding, rejected) || returned.Name != rejected.Name
                || other.Name == head.Name || other.Name == binding.Name
                || first.Guard is not Expr.Call { Function: AutoImportNames.IsAtom }
                || ReadEntryTypeGuard(first, first.Guard) != (IsAtom, 0)
                || wrapped.Name != input.Name || forward.Function != function.Name
                || ReadRegisterTailList(first, forward) is not { } list
                || read.Arguments is not [Expr.Variable readInput] || readInput.Name != head.Name
                || check.Arguments is not [Expr.Variable checkedInput] || checkedInput.Name != binding.Name
                || finish.Arguments is not [Expr.Variable finalInput] || finalInput.Name != binding.Name
                || !AttributeLiteral.TryNormalizeConstant(failure, out Term failureValue)
                || open.Arguments.Count != 0
                || ReadEntryCall(read, [VariableScopeNames.Wildcard, head.Name]) is not { } producer
                || ReadEntryCall(check, [binding.Name]) is not { } checkCall
                || ReadEntryCall(open, []) is not { } openCall
                || ReadEntryCall(finish, [binding.Name]) is not { } finishCall)
            {
                return false;
            }

            int listEntry = Label();
            int openFailure = Label();
            int checkFailure = Label();
            owner.Op(IsAtom, LabelTag, listEntry);
            owner.Operand(XRegisterTag, 0);
            EmitRegisterTailList(forward, list);

            Mark(listEntry);
            owner.EmitNonemptyList(0, functionInfo);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.Op(IsNil, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, 2);
            EmitGeneralFrame(2, slots: 1);
            EmitEntryCall(producer);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );

            var previous = CallLocation(read, location);
            EmitEntryCall(checkCall, previous);
            EmitResultTest(accepted.Value, checkFailure);
            previous = CallLocation(check, previous);
            EmitEntryCall(openCall, previous);
            EmitResultTest(ready.Value, openFailure);
            previous = CallLocation(open, previous);
            Load(0);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            EmitEntryCall(finishCall, previous);
            owner.EmitConstant(success.Value, 0);
            owner.EmitFrameReturn(0);
            Mark(openFailure);
            owner.EmitFrameReturn(1);
            Mark(checkFailure);
            owner.EmitConstant(failureValue, 0);
            owner.EmitFrameReturn(1);

            return true;

            void EmitResultTest(Term value, int target)
            {
                owner.Op(ExactEqualTest, LabelTag, target);
                owner.Operand(XRegisterTag, 0);
                owner.LiteralOperand(value);
            }
        }
    }
}
