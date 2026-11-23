using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedPairListResult(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable first, Pattern.Variable second],
                        Body: Expr.Tuple
                        {
                            Items: [Expr.Literal { Value: Atom outerTag }, Expr.List
                            {
                                Tail: null,
                                Items:
                                [
                                    Expr.Tuple { Items: [Expr.Literal { Value: Atom firstTag }, Expr.Variable left] },
                                    Expr.Tuple { Items: [Expr.Literal { Value: Atom secondTag }, Expr.Variable right] }
                                ]
                            }]
                        }
                    }
                ]
                || !DistinctBindings(first, second) || left.Name != first.Name || right.Name != second.Name)
            {
                return false;
            }

            const int tupleHeapWords = 3;
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3 * tupleHeapWords + 2 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 2);
            EmitTaggedValue(secondTag, 1, 1);
            owner.Op(PutList, XRegisterTag, 1);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            EmitTaggedValue(firstTag, 0, 0);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            EmitTaggedValue(outerTag, 0, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private void EmitTaggedValue(Atom tag, int value, int destination)
        {
            owner.EmitTupleHeader(XRegisterTag, 2, destination);
            owner.LiteralOperand(tag);
            owner.Operand(XRegisterTag, value);
        }
    }
}
