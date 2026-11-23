using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTaggedPayloadEffectTail(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Variable first, Pattern.Variable second],
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                            Arguments: [Expr.Variable readFirst, Expr.Variable readSecond]
                        } read,
                        Clauses:
                        [
                        {
                            Guard: null,
                            Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable payload] }],
                            Body: Expr.Sequence
                            {
                                Items:
                                    [
                                        Expr.Call
                                    {
                                        Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                                        Arguments: [Expr.Variable effectValue, Expr.Literal { Value: Atom context }]
                                    } effect,
                                        Expr.Call
                                    {
                                        Module: null, DynamicRemote: false,
                                        Arguments: [Expr.Variable tailFirst, Expr.Variable tailValue]
                                    } tail
                                    ]
                            }
                        },
                        { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Variable returned }
                        ]
                    }
                }
                || !DistinctBindings(
                    first,
                    second,
                    payload,
                    fallback
                )
                || readFirst.Name != first.Name || readSecond.Name != second.Name
                || effectValue.Name != payload.Name || tailFirst.Name != first.Name
                || tailValue.Name != payload.Name || returned.Name != fallback.Name
                || !entries.TryGetValue((tail.Function, 2), out int target))
            {
                return false;
            }

            const int payloadSlot = 0;
            const int firstSlot = 1;
            const int slots = 2;
            int unmatched = Label();
            // The entry value and the projected payload both cross the effect call.
            // The payload starts in its stack home on the successful match edge.
            EmitFrameAllocation(2, slots);
            EmitFrameInitialization(payloadSlot, firstSlot);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                firstSlot
            );
            owner.EmitCallLine(read, location);
            owner.EmitStaticRemoteCall(read);
            EmitDetailRecordTest(
                0,
                2,
                tag,
                unmatched,
                ReturnsTuple(read, 0, []) ? TupleTypeBits : 0
            );
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, payloadSlot);
            owner.EmitConstant(context, 1);
            owner.MoveRegister(
                YRegisterTag,
                payloadSlot,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(effect, CallLocation(read, location));
            owner.EmitStaticRemoteCall(effect);
            owner.MoveRegister(
                YRegisterTag,
                payloadSlot,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                YRegisterTag,
                firstSlot,
                XRegisterTag,
                0
            );
            owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(LabelTag, target);
            owner.Operand(BeamWire.UnsignedTag, slots);
            Mark(unmatched);
            owner.EmitFrameReturn(slots);

            return true;
        }
    }
}
