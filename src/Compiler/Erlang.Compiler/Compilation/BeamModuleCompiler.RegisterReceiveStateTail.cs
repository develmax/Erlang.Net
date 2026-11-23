using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRegisterReceiveStateTail(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || !entries.TryGetValue((function.Name, function.Arity), out int entry) || function.Clauses is not
                [
                    {
                        Guard: null, Patterns: [Pattern.Variable state], Body: Expr.Receive
                        {
                            Timeout: null, After: null, Clauses:
                            [
                            {
                                Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable payload] }],
                                Body: Expr.Sequence
                                {
                                    Items:
                                        [
                                            Expr.Match { Pattern: Pattern.Variable updated, Value: Expr.Call step },
                                            Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable next] } tail
                                        ]
                                }
                            },
                            {
                                Guard: null, Patterns: [Pattern.Literal { Value: Atom finished }], Body: var done
                            }
                            ]
                        }
                    }
                ]
                || state.Name == VariableScopeNames.Wildcard || payload.Name == VariableScopeNames.Wildcard
                || payload.Name == state.Name || updated.Name == state.Name || updated.Name == payload.Name
                || updated.Name == VariableScopeNames.Wildcard || updated.Name != next.Name || tail.Function != function.Name
                || step.Arguments is not [Expr.Variable first, Expr.Variable second] || first.Name != payload.Name || second.Name != state.Name
                || ReadEntryCall(step, [payload.Name, state.Name]) is not { } call
                || !AttributeLiteral.TryNormalizeConstant(done, out Term result))
            {
                return false;
            }

            int scan = Label();
            int atom = Label();
            int retry = Label();
            int wait = Label();

            EmitFrameAllocation(1, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            Mark(scan);
            owner.Op(LoopReceive, LabelTag, wait);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsTuple, LabelTag, atom);
            owner.Operand(XRegisterTag, 0);
            EmitDetailRecordTest(
                0,
                2,
                tag,
                retry,
                TupleTypeBits
            );
            owner.instructions.WriteByte(RemoveMessage);
            owner.EmitTuplePart(0, 1, 0);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitCallLine(step, location);
            Call(call.Module, step.Function, step.Arguments.Count);
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, 0);

            Mark(atom);
            owner.EmitAtomTest(0, finished.Name, retry);
            owner.instructions.WriteByte(RemoveMessage);
            owner.EmitConstant(result, 0);
            owner.EmitFrameReturn(1);
            Mark(retry);
            owner.Op(LoopReceiveEnd, LabelTag, scan);
            Mark(wait);
            owner.Op(Wait, LabelTag, scan);

            return true;
        }
    }
}
