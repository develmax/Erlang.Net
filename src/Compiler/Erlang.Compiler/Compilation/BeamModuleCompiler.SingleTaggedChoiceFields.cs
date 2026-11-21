using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSingleProjectedChoice(
            FunctionDefinition function,
            Expr.Case decision,
            (Atom Key, Expr.Call Call, Term Constant) branch,
            int tail,
            (Expr.Call Call, List<Atom> Tags) error
        )
        {
            var selector = (Expr.Call)decision.Value;
            var payload = (Expr.Call)branch.Call.Arguments[0];
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || !owner.ResolvesErlangBif(selector) || !owner.ResolvesErlangBif(payload)
                || AlwaysCalledWithEmptySecondArgument(function))
            {
                return false;
            }

            int failure = Label();
            int badPair = branch.Call.Module is not null ? Label() : 0;
            int element = owner.Import(ErlangModuleNames.Module, selector.Function, 2);
            owner.EmitCallLine(selector, location);
            EmitChoiceElement(element, 1, 2);
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, 2);
            owner.LiteralOperand(branch.Key);

            BeamSourceLocation previous = CallLocation(selector, location);
            owner.EmitCallLine(payload, previous);
            previous = CallLocation(payload, previous);
            EmitChoiceElement(element, 2, 0);
            EmitFrameAllocation(arity: 2, size: 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitConstant(branch.Constant, 1);
            owner.EmitCallLine(branch.Call, previous);
            Call(branch.Call.Module, branch.Call.Function, 2);
            if (badPair != 0)
            {
                owner.EmitTupleCheck(0, 2, badPair);
            }

            owner.EmitTupleElement(0, 1);
            owner.EmitTupleElement(1, 2);
            owner.MoveToX0(YRegisterTag, 0);
            EmitTailLocal(tail, 3, 1);

            Mark(failure);
            EmitTaggedExit(
                error.Call,
                error.Tags,
                CallLocation(selector, location),
                framed: false,
                live: 3,
                source: 2
            );
            if (badPair != 0)
            {
                Mark(badPair);
                previous = location;
                owner.EmitArithmeticLine(decision.SourceLine, ref previous);
                owner.Op(BadMatch, XRegisterTag, 0);
            }

            return true;
        }

        private bool TryEmitSingleForwardedTupleChoice(
            int arity,
            Expr.Case decision,
            Expr.Call prepare,
            int prepareEntry,
            Pattern.Variable input
        )
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || decision.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal key, Pattern.Variable value] }],
                    Body: Expr.Tuple
                    {
                        Items:
                                [
                                    Expr.Literal { Value: Atom tag },
                                    Expr.Call
                                {
                                    Module: null, DynamicRemote: false,
                                    Arguments: [Expr.Variable selected, var extras]
                                } decode
                                ]
                    }
                },
                { Guard: null, Patterns: [Pattern.Variable fallback], Body: var failed }
                ]
                || !DistinctBindings(input, value) || !DistinctBindings(input, fallback)
                || selected.Name != value.Name || ReadTaggedExit(failed, fallback.Name) is not { } error
                || !AttributeLiteral.TryNormalizeConstant(extras, out Term constant)
                || !entries.TryGetValue((decode.Function, 2), out int decodeEntry))
            {
                return false;
            }

            EmitFrameAllocation(arity: arity, size: 0);
            owner.EmitCallLine(prepare, location);
            owner.EmitFieldCall(2, prepareEntry);
            EmitSingletonUnwrap();

            int failure = Label();
            owner.EmitTupleCheck(0, 2, failure);
            owner.EmitTuplePart(0, 0, 1);
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(key.Value);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitConstant(constant, 1);
            var previous = CallLocation(prepare, location);
            owner.EmitCallLine(decode, previous);
            owner.EmitFieldCall(2, decodeEntry);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(tag);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(0);

            Mark(failure);
            EmitTaggedExit(
                error.Call,
                error.Tags,
                previous,
                framed: true
            );

            return true;
        }
    }
}
