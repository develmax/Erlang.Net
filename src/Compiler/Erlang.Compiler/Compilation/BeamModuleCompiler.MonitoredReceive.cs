using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const int ReferenceTypeBits = 1 << 10;

        private bool TryEmitMonitoredReceive(Clause clause)
        {
            if (!HasUnconditionalParameters(clause) || clause.Body is not Expr.Sequence { Items: [
                Expr.Match { Pattern: Pattern.Literal { Value: Atom expected }, Value: Expr.Call setup } setupMatch,
                Expr.Match { Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } reference,
                    Value: Expr.Call { Function: AutoImportNames.Monitor, Arguments: [
                        Expr.Literal { Value: Atom { Name: "process" } kind }, Expr.Literal { Value: Atom target }
                    ] } monitor },
                Expr.Receive { Timeout: null, Clauses: [{
                    Guard: null, Patterns: [Pattern.Tuple { Items: [
                        Pattern.Literal { Value: Atom { Name: "DOWN" } notification },
                        Pattern.Variable matched,
                        Pattern.Literal { Value: Atom messageKind },
                        Pattern.Variable { Name: VariableScopeNames.Wildcard },
                        Pattern.Variable { Name: VariableScopeNames.Wildcard }
                    ] }], Body: Expr.Literal result
                }] }
            ] }
                || matched.Name != reference.Name || !messageKind.Equals(kind)
                || clause.Patterns.OfType<Pattern.Variable>().Any(pattern => pattern.Name == reference.Name)
                || !owner.ResolvesErlangBif(monitor))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(pattern => pattern.Name).ToArray();
            if (ReadEntryCall(setup, parameters) is not { } preparation)
            {
                return false;
            }

            const int frame = 2;
            const int referenceSlot = 0;
            const int markerSlot = 1;
            int scan = Label();
            int retry = Label();
            int wait = Label();
            int mismatch = Label();

            EmitGeneralFrame(EntryCallLiveCount(preparation, parameters), slots: frame);
            EmitEntryCall(preparation);
            owner.Op(ExactEqualTest, LabelTag, mismatch);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(expected);
            owner.Op(ReceiveMarkerReserve, YRegisterTag, markerSlot);
            owner.EmitConstant(target, 1);
            owner.EmitConstant(kind, 0);
            var setupLocation = CallLocation(setup, location);
            owner.EmitCallLine(monitor, setupLocation);
            Call(ErlangModuleNames.Module, AutoImportNames.Monitor, 2);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, referenceSlot);
            owner.Op(ReceiveMarkerBind, YRegisterTag, markerSlot);
            owner.Operand(XRegisterTag, 0);
            owner.Op(ReceiveMarkerUse, XRegisterTag, 0);

            Mark(scan);
            owner.Op(LoopReceive, LabelTag, wait);
            owner.Operand(XRegisterTag, 0);
            owner.EmitRecordTest(retry, XRegisterTag, 0, notification, 5);
            owner.EmitTuplePart(0, 1, 1);
            owner.EmitTuplePart(0, 2, 0);
            owner.Op(ExactEqualTest, LabelTag, retry);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(messageKind);
            owner.Op(ExactEqualTest, LabelTag, retry);
            owner.Operand(XRegisterTag, 1);
            owner.EmitTypedRegister(referenceSlot, ReferenceTypeBits, registerTag: YRegisterTag);
            owner.Op(ReceiveMarkerClear, YRegisterTag, referenceSlot);
            owner.instructions.WriteByte(RemoveMessage);
            owner.EmitConstant(result.Value, 0);
            owner.EmitFrameReturn(frame);
            Mark(retry);
            owner.Op(LoopReceiveEnd, LabelTag, scan);
            Mark(wait);
            owner.Op(Wait, LabelTag, scan);
            Mark(mismatch);
            owner.EmitLine(setupMatch.SourceLine == 0 ? setupLocation
                : new BeamSourceLocation(setupMatch.PreprocessorFile, setupMatch.SourceLine));
            owner.Op(BadMatch, XRegisterTag, 0);
            return true;
        }
    }
}
