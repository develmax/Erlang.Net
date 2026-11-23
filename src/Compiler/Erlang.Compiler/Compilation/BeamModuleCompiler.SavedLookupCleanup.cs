using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSavedLookupCleanup(Clause clause)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.Variable input],
                    Body: Expr.Case
                    {
                        Value: Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments:
                            [
                                Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false, Arguments: [var readKey] } read,
                                Expr.Variable query
                            ]
                        } lookup,
                        Clauses:
                        [
                        { Guard: null, Patterns: [Erlang.Pattern.Literal { Value: Atom absent }], Body: Expr.Literal missing },
                        {
                            Guard: null, Patterns: [Erlang.Pattern.Variable found],
                            Body: Expr.Sequence
                            {
                                Items:
                                    [
                                        Expr.Call
                                    {
                                        Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                                        Arguments: [var cleanupKey, Expr.Variable cleanupName]
                                    } cleanup,
                                        Expr.Call
                                    {
                                        Module: not (null or ErlangModuleNames.Module), DynamicRemote: false,
                                        Arguments: [var finishKey, Expr.Variable finishName]
                                    } finish,
                                        Expr.Literal result
                                    ]
                            }
                        }
                        ]
                    }
                }
                || !DistinctBindings(input, found)
                || input.Name != query.Name || cleanupName.Name != found.Name || finishName.Name != found.Name
                || !entries.TryGetValue((lookup.Function, 2), out int entry)
                || !AttributeLiteral.TryNormalizeConstant(readKey, out Term readArgument)
                || !AttributeLiteral.TryNormalizeConstant(cleanupKey, out Term cleanupArgument)
                || !AttributeLiteral.TryNormalizeConstant(finishKey, out Term finishArgument))
            {
                return false;
            }

            const int resultSlot = 0;
            const int frame = 1;
            int branch = Label();
            EmitFrameAllocation(1, frame);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                resultSlot
            );
            owner.EmitConstant(readArgument, 0);
            var previous = location;
            EmitCall(read);

            ReadSavedName();
            owner.InitStackSlot(resultSlot);
            owner.EmitCallLine(lookup, previous);
            previous = CallLocation(lookup, previous);
            owner.EmitFieldCall(2, entry);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                resultSlot
            );
            owner.Op(ExactEqualTest, LabelTag, branch);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(absent);
            owner.EmitConstant(missing.Value, 0);
            owner.EmitFrameReturn(frame);

            Mark(branch);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(cleanupArgument, 0);
            EmitCall(cleanup);

            ReadSavedName();
            owner.Op(TrimFrame, BeamWire.UnsignedTag, frame);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.EmitConstant(finishArgument, 0);
            EmitCall(finish);
            owner.EmitConstant(result.Value, 0);
            owner.EmitFrameReturn(0);

            return true;

            void ReadSavedName()
            {
                owner.MoveRegister(
                    YRegisterTag,
                    resultSlot,
                    XRegisterTag,
                    1
                );
            }

            void EmitCall(Expr.Call call)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.EmitStaticRemoteCall(call);
            }
        }
    }
}
