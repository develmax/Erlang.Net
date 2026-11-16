using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedListResult(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null,
                    Patterns: [Pattern.Variable data, Pattern.Variable input,
                        Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil },
                        Pattern.Variable unused],
                    Body: Expr.Case
                    {
                        Value: Expr.Call { DynamicRemote: false, Arguments: [Expr.Literal key, Expr.Variable used] } sourceCall,
                        Clauses: [
                            { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable result] }],
                                Body: Expr.List { Tail: null, Items: [Expr.Variable head, Expr.Variable saved] } },
                            { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }], Body: Expr.Variable fallback }
                        ]
                    }
                }
            ] || !DistinctBindings(data, input, result)
                || unused.Name != VariableScopeNames.Wildcard && !DistinctBindings(data, input, unused)
                || input.Name != used.Name || result.Name != head.Name
                || data.Name != saved.Name || data.Name != fallback.Name
                || owner.ResolveImportedCall(sourceCall, entries) is not { Module: not null } call)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int notTuple = Label();
            int invalid = Label();
            owner.Op(IsNil, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, 2);
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
            owner.EmitConstant(key.Value, 0);
            owner.EmitCallLine(call, location);
            Call(call.Module, call.Function, 2);
            owner.Op(IsTuple, LabelTag, notTuple);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsTaggedTuple, LabelTag, invalid);
            owner.EmitTypedRegister(0, TupleTypeBits);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(tag);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 2 * ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTuplePart(0, 1, 0);
            owner.Op(PutList, YRegisterTag, 0);
            owner.Operand(AtomTag, NilAtomIndex);
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);
            Mark(notTuple);
            owner.EmitAtomTest(0, "false", invalid);
            Load(0);
            owner.EmitFrameReturn(1);
            Mark(invalid);
            owner.Op(CaseEnd, XRegisterTag, 0);
            return true;
        }
    }
}
