using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitConditionalTupleTail(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.Literal { Value: Atom selected }, Pattern.Tuple { Items: [
                        Pattern.Variable ignoredFirst, Pattern.Variable ignoredSecond
                    ] }], Body: Expr.Literal result
                },
                {
                    Guard: null, Patterns: [Pattern.Variable transport, Pattern.Tuple { Items: [
                        Pattern.Variable ignored, Pattern.Variable socket
                    ] }],
                    Body: Expr.Call { DynamicRemote: true, Arguments: [
                        Expr.Variable target, Expr.Literal { Value: Atom member },
                        Expr.List { Tail: null, Items: [Expr.Variable argument] }
                    ] } call
                }
            ] || !DistinctBindings(transport, socket) || target.Name != transport.Name || argument.Name != socket.Name
                || (ignored.Name != VariableScopeNames.Wildcard && (ignored.Name == transport.Name || ignored.Name == socket.Name))
                || (ignoredFirst.Name != VariableScopeNames.Wildcard && ignoredFirst.Name == ignoredSecond.Name))
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int dynamicCall = Label();
            owner.Op(ExactEqualTest, LabelTag, dynamicCall);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(selected);
            owner.Op(IsTuple, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TestArity, LabelTag, dynamicCall);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitConstant(result.Value, 0);
            owner.instructions.WriteByte(Return);
            Mark(dynamicCall);
            owner.EmitTupleCheck(1, 2, functionInfo);
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(1, 1, 1);
            owner.EmitConstant(member, 2);
            owner.Op(Swap, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitCallLine(call, location);
            owner.Op(TailDynamicApply, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);

            return true;
        }
    }
}
