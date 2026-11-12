using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRecordFieldCallUpdate(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [
                    Pattern.Tuple { Items: [Pattern.Literal { Value: Atom messageTag }, Pattern.Variable key] },
                    AliasPattern { Left: Pattern.Tuple statePattern, Right: Pattern.Variable state }
                  ], Body: Expr.Sequence
                  {
                      Items: [
                    Expr.Match
                      {
                          Pattern: Pattern.Variable result, Value: Expr.Call
                          {
                              Module: not null, DynamicRemote: false,
                              Arguments: [Expr.Variable inputKey, Expr.Literal { Value: Integer index }, Expr.Variable inputField]
                          } call
                      },
                    Expr.Tuple
                      {
                          Items: [Expr.Literal { Value: Atom reply }, Expr.RecordUpdate
                          {
                              Value: Expr.Variable oldState, Fields: [var field]
                          } update]
                      }
                  ]
                  }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable message, Pattern.Variable fallback],
                    Body: Expr.Sequence
                    {
                        Items: [
                    Expr.Call
                        {
                            Module: not null, DynamicRemote: false, Arguments: [
                        Expr.Literal { Value: var format }, Expr.List
                            {
                                Tail: null, Items: [
                            Expr.Literal { Value: Atom module }, Expr.Literal { Value: Integer line }, Expr.Variable logged
                        ]
                            }
                    ]
                        } log,
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom otherReply }, Expr.Variable returned] }
                  ]
                    }
                }
            ] || statePattern.Items is not [Pattern.Literal { Value: Atom recordTag }, ..]
                || update.Name != recordTag.Name || update.Arity != statePattern.Items.Count
                || field.Index <= 0 || field.Index >= update.Arity
                || statePattern.Items[field.Index] is not Pattern.Variable bound
                || statePattern.Items.Skip(1).Where((_, offset) => offset + 1 != field.Index)
                    .Any(pattern => pattern is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                || !DistinctBindings(
                    key,
                    state,
                    bound,
                    result
                ) || !DistinctBindings(message, fallback)
                || inputKey.Name != key.Name || inputField.Name != bound.Name || oldState.Name != state.Name
                || field.Value is not Expr.Variable replacement || replacement.Name != result.Name
                || logged.Name != message.Name || returned.Name != fallback.Name || !reply.Equals(otherReply))
            {
                return false;
            }

            int other = Label();
            EmitDetailRecordTest(
                0,
                2,
                messageTag,
                other
            );
            EmitDetailRecordTest(
                1,
                update.Arity,
                recordTag,
                other
            );
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitTuplePart(1, field.Index, 2);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitConstant(index, 1);
            var previous = location;
            EmitWindowCall(call, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, update.Arity + 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(UpdateRecord, AtomTag, owner.Intern("reuse"));
            owner.Operand(BeamWire.UnsignedTag, update.Arity);
            owner.EmitTypedRegister(0, TupleTypeBits, registerTag: YRegisterTag);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, field.Index + 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(reply);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);

            Mark(other);
            owner.EmitLoggedReply(log, format, module, line, reply, previous, stateRegister: 1);

            return true;
        }
    }
}
