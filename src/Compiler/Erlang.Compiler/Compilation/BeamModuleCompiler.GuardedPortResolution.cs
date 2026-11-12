using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedPortResolution(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Patterns: [Pattern.Variable listener, Pattern.Variable port],
                Guard: Expr.Binary { Operator: ErlangOperators.AndAlso,
                    Left: Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Function: AutoImportNames.IsPid, Arguments: [Expr.Variable testedListener] } pidCheck,
                    Right: Expr.Call { Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                        Function: AutoImportNames.IsInteger, Arguments: [Expr.Variable testedPort] } integerCheck
                },
                Body: Expr.Case { Value: Expr.Variable input, Clauses: [
                    { Guard: null, Patterns: [Pattern.Literal { Value: Integer key }], Body: Expr.Sequence { Items: [
                        Expr.Match { Pattern: Pattern.Tuple { Items: [
                            Pattern.Literal { Value: Atom tag },
                            Pattern.Tuple { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable resolved] }
                        ] }, Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable remoteArg] } call },
                        Expr.Variable found
                    ] } },
                    { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: Expr.Variable unchanged }
                ] }
            }] || !DistinctBindings(listener, port, resolved)
                || listener.Name != testedListener.Name || listener.Name != remoteArg.Name
                || port.Name != testedPort.Name || port.Name != input.Name || port.Name != unchanged.Name
                || resolved.Name != found.Name || !owner.ResolvesErlangBif(pidCheck) || !owner.ResolvesErlangBif(integerCheck)
                || owner.ResolvesErlangBif(call))
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int existing = Label();
            int badMatch = Label();
            owner.Op(IsPid, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsInteger, LabelTag, functionInfo);
            owner.Operand(XRegisterTag, 1);
            owner.Op(ExactEqualTest, LabelTag, existing);
            owner.EmitTypedRegister(1, IntegerTypeBits);
            owner.LiteralOperand(key);
            owner.Op(Allocate, BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            var previous = location;
            EmitWindowCall(call, ref previous);
            EmitDetailRecordTest(0, 2, tag, badMatch);
            owner.EmitTuplePart(0, 1, 1);
            owner.EmitTupleCheck(1, 2, badMatch);
            owner.EmitTuplePart(1, 1, 0);
            owner.EmitFrameReturn(0);
            Mark(existing);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            owner.instructions.WriteByte(Return);
            Mark(badMatch);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }
    }
}
