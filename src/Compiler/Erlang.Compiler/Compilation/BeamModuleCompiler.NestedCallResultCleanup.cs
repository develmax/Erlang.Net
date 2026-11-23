using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNestedCallResultCleanup(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.Variable input, Erlang.Pattern.Variable retained],
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                            Arguments: [Expr.Variable readInput, var constant]
                        } read,
                        Clauses: [var matched, var outerFallback]
                    }
                }
                || matched is not
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.Tuple
                    {
                        Items: [Erlang.Pattern.Literal { Value: Atom readTag }, Erlang.Pattern.Variable payload]
                    }],
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                            Arguments: [Expr.Variable writeInput, Expr.Variable writePayload]
                        } write,
                        Clauses: [var accepted, var innerFallback]
                    }
                }
                || accepted is not
                {
                    Guard: null, Patterns: [Erlang.Pattern.Literal { Value: Atom acceptedValue }],
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom resultTag }, Expr.Variable result] }
                }
                || !DistinctBindings(input, retained, payload) || readInput.Name != input.Name
                || writeInput.Name != retained.Name || writePayload.Name != payload.Name || result.Name != retained.Name
                || !AttributeLiteral.TryNormalizeConstant(constant, out Term readOption)
                || ReadCleanup(outerFallback) is not { } outer
                || ReadCleanup(innerFallback, payload.Name) is not { } inner)
            {
                return false;
            }

            const int resultSlot = 0;
            const int retainedSlot = 1;
            const int frame = 2;
            int innerFailure = Label();
            int outerFailure = Label();

            EmitFrameAllocation(2, frame);
            EmitFrameInitialization(resultSlot, retainedSlot);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                retainedSlot
            );
            owner.EmitConstant(readOption, 1);
            var previous = location;
            EmitCallAndSave(read, ref previous);
            EmitDetailRecordTest(
                0,
                2,
                readTag,
                outerFailure
            );
            owner.EmitTuplePart(0, 1, 1);

            // Only the outer failure edge still holds the first selector.
            // On success the same slot is available for the next call result.
            EmitFrameInitialization(resultSlot, retainedSlot);
            owner.MoveRegister(
                YRegisterTag,
                retainedSlot,
                XRegisterTag,
                0
            );
            EmitCallAndSave(write, ref previous);
            owner.Op(ExactEqualTest, LabelTag, innerFailure);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(acceptedValue);

            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(resultTag.Name)), (YRegisterTag, retainedSlot));
            owner.EmitFrameReturn(frame);

            EmitCleanup(innerFailure, inner, ref previous);
            EmitCleanup(outerFailure, outer, ref previous);

            return true;

            (Expr.Call Call, int Entry)? ReadCleanup(Clause branch, string? projected = null)
            {
                if (branch is not
                    {
                        Guard: null, Patterns: [Erlang.Pattern.Variable rejected],
                        Body: Expr.Sequence
                        {
                            Items:
                            [
                                Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable argument] } call,
                                Expr.Variable returned
                            ]
                        }
                    }
                    || rejected.Name == VariableScopeNames.Wildcard
                    || rejected.Name == input.Name || rejected.Name == retained.Name || rejected.Name == projected
                    || rejected.Name != returned.Name || argument.Name != retained.Name
                    || !entries.TryGetValue((call.Function, 1), out int target))
                {
                    return null;
                }

                return (call, target);
            }

            void EmitCallAndSave(Expr.Call call, ref BeamSourceLocation preceding)
            {
                owner.EmitCallLine(call, preceding);
                preceding = CallLocation(call, preceding);
                owner.EmitStaticRemoteCall(call);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    resultSlot
                );
            }

            void EmitCleanup(int label, (Expr.Call Call, int Entry) cleanup, ref BeamSourceLocation preceding)
            {
                Mark(label);
                owner.MoveRegister(
                    YRegisterTag,
                    retainedSlot,
                    XRegisterTag,
                    0
                );
                owner.MoveRegister(
                    YRegisterTag,
                    resultSlot,
                    YRegisterTag,
                    retainedSlot
                );
                owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.EmitCallLine(cleanup.Call, preceding);
                preceding = CallLocation(cleanup.Call, preceding);
                owner.EmitFieldCall(1, cleanup.Entry);
                owner.MoveRegister(
                    YRegisterTag,
                    resultSlot,
                    XRegisterTag,
                    0
                );
                owner.EmitFrameReturn(1);
            }
        }
    }
}
