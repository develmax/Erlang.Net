using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSocketControllerTransfer(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null,
                Patterns: [Pattern.Variable unused, Pattern.Variable controller, Pattern.Variable socket],
                Body: Expr.Maybe
                {
                    Items: [
                        Expr.MaybeMatch
                        {
                            Pattern: Pattern.Literal { Value: Atom ok },
                            Value: Expr.Call { Module: not null, DynamicRemote: false } transfer
                        },
                        Expr.Variable returned
                    ],
                    Clauses: [{
                        Guard: null,
                        Patterns: [Pattern.Tuple { Items: [
                            Pattern.Literal { Value: Atom error }, Pattern.Variable reason
                        ] }],
                        Body: Expr.Call exit
                    }]
                } conditional
            }] || !DistinctBindings(controller, socket, reason)
                || unused.Name == controller.Name || unused.Name == socket.Name || unused.Name == reason.Name
                || returned.Name != socket.Name || owner.ResolvesErlangBif(transfer)
                || !ReadSocketExit(exit, reason, out Atom exitTag))
            {
                return false;
            }

            Term? option = null;
            if (transfer.Arguments is [Expr.Variable socketWithOption, var key, Expr.Variable controllerWithOption]
                && socketWithOption.Name == socket.Name && controllerWithOption.Name == controller.Name
                && AttributeLiteral.TryNormalizeConstant(key, out Term value))
            {
                option = value;
            }
            else if (transfer.Arguments is not [Expr.Variable transferSocket, Expr.Variable transferController]
                || transferSocket.Name != socket.Name || transferController.Name != controller.Name)
            {
                return false;
            }

            int failed = Label();
            int unexpected = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.MoveRegister(XRegisterTag, 2, YRegisterTag, 0);
            owner.MoveToX0(XRegisterTag, 2);
            if (option is not null)
            {
                owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 2);
                owner.EmitConstant(option, 1);
            }
            owner.EmitCallLine(transfer, location);
            owner.EmitStaticRemoteCall(transfer);
            EmitConfigurationAtomTest(ok, failed);
            Load(0);
            owner.EmitFrameReturn(1);

            Mark(failed);
            EmitDetailRecordTest(0, 2, error, unexpected);
            AllocateFailureTuple();
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(exitTag.Name)), (XRegisterTag, 0));
            owner.EmitCallLine(exit, CallLocation(transfer, location));
            TailFailure(AutoImportNames.Exit);

            Mark(unexpected);
            AllocateFailureTuple();
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(ErlangErrorReasons.ElseClause)), (XRegisterTag, 0));
            owner.EmitLine(new(conditional.PreprocessorFile ?? location.File, conditional.SourceLine));
            TailFailure(AutoImportNames.Error);

            return true;

            void AllocateFailureTuple()
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, 1);
            }

            void TailFailure(string functionName)
            {
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, functionName, 1));
                owner.Operand(BeamWire.UnsignedTag, 1);
            }
        }
    }
}
