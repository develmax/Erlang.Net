using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTwoCheckedExternalCalls(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                { Guard: null, Patterns: [Pattern.Variable carried], Body: var statements } clause
                ]
                || !HasUnconditionalParameters(clause)
                || SkipIgnoredLiterals(statements) is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match
                    {
                        Pattern: Pattern.Literal { Value: Atom firstExpected },
                        Value: Expr.Call { Arguments: [Expr.Literal { Value: Atom firstTarget }, Expr.Variable firstInput] } first
                    } firstMatch,
                        Expr.Match
                    {
                        Pattern: Pattern.Literal { Value: Atom nextExpected },
                        Value: Expr.Call { Arguments: [Expr.Literal { Value: Atom nextTarget }, Expr.Variable nextInput] } next
                    }
                    ]
                }
                || firstInput.Name != carried.Name || nextInput.Name != carried.Name
                || !TryResolveOrdinaryCall(first, out string? firstModule) || firstModule is null
                || !TryResolveOrdinaryCall(next, out string? nextModule) || nextModule is null)
            {
                return false;
            }

            int secondFailure = Label();
            int firstFailure = Label();
            EmitFrameAllocation(1, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(firstTarget, 0);
            owner.EmitCallLine(first, location);
            Call(firstModule, first.Function, 2);
            Check(firstExpected, firstFailure);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitConstant(nextTarget, 0);
            owner.EmitCallLine(next, CallLocation(first, location));
            Call(nextModule, next.Function, 2);
            Check(nextExpected, secondFailure);
            owner.EmitFrameReturn(0);

            Mark(secondFailure);
            owner.Op(BadMatch, XRegisterTag, 0);
            Mark(firstFailure);
            owner.EmitLine(firstMatch.SourceLine == 0 ? CallLocation(first, location) : new(firstMatch.PreprocessorFile, firstMatch.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void Check(Atom expected, int failure)
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
                owner.LiteralOperand(expected);
            }
        }

        private bool TryEmitTaggedSingletonArgumentResult(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                { Guard: null, Patterns: [Pattern.Literal { Value: Atom selector }, Pattern.Variable carried], Body: var source },
                {
                    Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: var rejected
                }
                ]
                || carried.Name == VariableScopeNames.Wildcard
                || SkipIgnoredLiterals(source) is not Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match { Pattern: Pattern.Variable bound, Value: var binding },
                        Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Arguments:
                                [
                                    Expr.Variable first,
                                    var second,
                                    Expr.List { Items: [Expr.Variable passed], Tail: null }
                                ]
                        } start,
                        Clauses:
                            [
                            {
                                Guard: null,
                                Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom accepted }, Pattern.Variable value] }],
                                Body: Expr.Tuple
                                {
                                    Items:
                                        [
                                            Expr.Literal { Value: Atom resultTag },
                                            Expr.Variable result,
                                            Expr.Tuple { Items: [Expr.Literal { Value: Atom original }, Expr.Variable returned] }
                                        ]
                                }
                            },
                            { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Variable forwarding }
                            ]
                    }
                    ]
                }
                || !DistinctBindings(
                    carried,
                    bound,
                    value,
                    fallback
                )
                || bound.Name != first.Name || passed.Name != carried.Name || returned.Name != carried.Name
                || result.Name != value.Name || fallback.Name != forwarding.Name || !selector.Equals(original)
                || !AttributeLiteral.TryNormalizeConstant(binding, out Term firstArgument)
                || !AttributeLiteral.TryNormalizeConstant(second, out Term secondArgument)
                || !AttributeLiteral.TryNormalizeConstant(SkipIgnoredLiterals(rejected), out Term invalid)
                || !TryResolveOrdinaryCall(start, out string? module, preparedArgument: 2) || module is null)
            {
                return false;
            }

            int passthrough = Label();
            int failure = Label();
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(selector);
            EmitFrameAllocation(2, 1, heapWords: ConsHeapWords);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.Op(PutList, XRegisterTag, 1);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 2);
            owner.EmitConstant(secondArgument, 1);
            owner.EmitConstant(firstArgument, 0);
            owner.EmitCallLine(start, location);
            Call(module, start.Function, 3);
            owner.Op(IsTaggedTuple, LabelTag, passthrough);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(accepted);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 7);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitTagMatchTuple(1, (AtomTag, owner.Intern(selector.Name)), (YRegisterTag, 0));
            owner.EmitTagMatchTuple(
                0,
                (AtomTag, owner.Intern(resultTag.Name)),
                (XRegisterTag, 0),
                (XRegisterTag, 1)
            );
            owner.EmitFrameReturn(1);

            Mark(passthrough);
            owner.EmitFrameReturn(1);
            Mark(failure);
            owner.EmitConstant(invalid, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}
