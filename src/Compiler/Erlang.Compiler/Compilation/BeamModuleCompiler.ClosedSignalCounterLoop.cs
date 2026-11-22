using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitClosedSignalCounterLoop(FunctionDefinition function)
        {
            if (!CanUseEntryTypeFacts() || recursiveClosure is not null || function.Arity != 2
                || function.Clauses is not
                [
                {
                    Patterns: [global::Erlang.Pattern.Variable payload, global::Erlang.Pattern.Literal { Value: Integer limit }],
                    Guard: null,
                    Body: Expr.Binary
                    {
                        Operator: ErlangOperators.Send,
                        Left: Expr.Call
                        {
                            Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                            Function: AutoImportNames.Self, Arguments: []
                        } self,
                        Right: Expr.Variable sent
                    } send
                },
                {
                    Patterns: [global::Erlang.Pattern.Variable resource, global::Erlang.Pattern.Variable counter],
                    Guard: null,
                    Body: Expr.Case
                    {
                        Value: Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable read] } producer,
                        Clauses: [
                        {
                            Patterns: [global::Erlang.Pattern.Literal { Value: Atom repeat }],
                            Guard: null,
                            Body: Expr.Call
                            {
                                Module: null, DynamicRemote: false,
                                Arguments: [
                                            Expr.Variable original,
                                            Expr.Binary
                                {
                                    Operator: ErlangOperators.Plus, Left: Expr.Variable previous,
                                    Right: Expr.Literal { Value: Integer increment }
                                } advance
                                        ]
                            } recur
                        },
                        {
                            Patterns: [global::Erlang.Pattern.Variable or global::Erlang.Pattern.Any],
                            Guard: null,
                            Body: Expr.Literal { Value: Atom done }
                        }
                            ]
                    }
                }
                ]
                || payload.Name == VariableScopeNames.Wildcard || sent.Name != payload.Name
                || !owner.ResolvesErlangBif(self) || !DistinctBindings(resource, counter)
                || read.Name != resource.Name || original.Name != resource.Name || previous.Name != counter.Name
                || recur.Function != function.Name || increment.Value.Sign <= 0 || limit.Value.Sign < 0
                || owner.ResolvesErlangBif(producer) || !entries.TryGetValue((function.Name, 2), out int entry)
                || ReadEntryCall(producer, [resource.Name]) is not { } next
                || !owner.ReadClosedIntegerRecurrence(
                    function,
                    1,
                    localFunctions,
                    zeroSeededNonnegative: true
                ))
            {
                return false;
            }

            int loop = Label();
            int finished = Label();
            owner.Op(ExactEqualTest, LabelTag, loop);
            owner.EmitTypedRegister(1, IntegerTypeBits, lower: 0);
            owner.LiteralOperand(limit);
            EmitFrameAllocation(1, 0);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            EmitZeroGuardBif(self.Function, 0);
            var source = location;
            owner.EmitArithmeticLine(send.SourceLine, ref source);
            owner.instructions.WriteByte(Send);
            owner.EmitFrameReturn(0);

            Mark(loop);
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
            EmitEntryCall(next);
            owner.Op(ExactEqualTest, LabelTag, finished);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(repeat);
            var callLocation = CallLocation(producer, location);
            owner.EmitArithmeticLine(advance.SourceLine, ref callLocation);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Plus, 2));
            owner.EmitTypedRegister(
                0,
                IntegerTypeBits,
                lower: 0,
                registerTag: YRegisterTag
            );
            owner.LiteralOperand(increment);
            owner.Operand(XRegisterTag, 1);
            owner.MoveRegister(
                YRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, entry);
            owner.Operand(BeamWire.UnsignedTag, 2);

            Mark(finished);
            owner.EmitConstant(done, 0);
            owner.EmitFrameReturn(2);

            return true;
        }
    }
}
