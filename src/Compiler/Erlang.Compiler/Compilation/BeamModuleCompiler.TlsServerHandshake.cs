using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTlsServerHandshake(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom recordTag }, ..] } record,
                    Pattern.Variable socket, Pattern.Variable ip, Pattern.Variable port],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Variable options, Value: Expr.Call { Arguments: [Expr.Variable optionsIp] } defaults },
                    Expr.Case { Value: Expr.Call { Arguments: [
                        Expr.Variable handshakeSocket,
                        Expr.Call { Arguments: [Expr.Variable mergeOptions,
                            Expr.List { Items: [Expr.Variable family, ..] } additions,
                            Expr.List { Items: [], Tail: null } or Expr.Literal { Value: Nil }] } merge,
                        Expr.Call { Arguments: [] } timeout
                    ] } handshake, Clauses: [
                        { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom ok }, Pattern.Variable sslSocket] }],
                            Body: Expr.Case { Value: Expr.Call { Module: null, DynamicRemote: false,
                                Arguments: [Expr.Variable verifySocket, Expr.Variable verifyIp] } verify, Clauses: [
                                    { Guard: null, Patterns: [Pattern.Literal { Value: Atom verified }],
                                        Body: Expr.Tuple { Items: [Expr.Variable returnedSocket,
                                            Expr.Tuple { Items: [Expr.Variable returnedIp, Expr.Variable returnedPort] }] } },
                                    { Guard: null, Patterns: [Pattern.Tuple { Items: [
                                        Pattern.Literal { Value: Atom denied }, Pattern.Variable deniedReason
                                    ] }], Body: Expr.Sequence { Items: [
                                        Expr.Call { Arguments: [Expr.Variable deniedSocket] } closeSsl, Expr.Call deniedExit
                                    ] } }
                                ] } },
                        { Guard: null, Patterns: [AliasPattern errorAlias], Body: Expr.Sequence { Items: [
                            Expr.Case log, Expr.Call { Arguments: [Expr.Variable optionsSocket] } closeOptions, Expr.Call optionsExit
                        ] } },
                        { Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom error }, Pattern.Variable reason] }],
                            Body: Expr.Sequence { Items: [
                                Expr.Call { Arguments: [Expr.Variable errorSocket] } closeError, Expr.Call errorExit
                            ] } }
                    ] } decision
                ] }
            }] || !TryReadPatternAlias(errorAlias, out var errorBinding, out var errorShape)
                || errorShape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom optionsError }, AliasPattern reasonAlias] }
                || !TryReadPatternAlias(reasonAlias, out var optionsReason, out var reasonShape)
                || reasonShape is not Pattern.Tuple { Items: [
                    Pattern.Literal { Value: Atom optionTag }, Pattern.Variable { Name: VariableScopeNames.Wildcard }
                ] }
                || !DistinctBindings(socket, ip, port, options, new(family.Name), sslSocket, deniedReason)
                || !DistinctBindings(socket, ip, port, options, new(family.Name), errorBinding, optionsReason)
                || !DistinctBindings(socket, ip, port, options, new(family.Name), reason)
                || !ok.Equals(verified) || !error.Equals(optionsError) || ok.Equals(error)
                || optionsIp.Name != ip.Name || verifyIp.Name != ip.Name || returnedIp.Name != ip.Name
                || returnedPort.Name != port.Name || mergeOptions.Name != options.Name
                || !new[] { handshakeSocket, optionsSocket, errorSocket }.All(value => value.Name == socket.Name)
                || !new[] { verifySocket, returnedSocket, deniedSocket }.All(value => value.Name == sslSocket.Name)
                || !ReadSocketExit(deniedExit, deniedReason, out Atom deniedExitTag)
                || !ReadSocketExit(optionsExit, optionsReason, out Atom optionsExitTag)
                || !ReadSocketExit(errorExit, reason, out Atom errorExitTag)
                || !ReadFixedLog(log, out var allowLog, out Term logLocation, out Term logLevel, out Term logFormat, out Expr logArgument)
                || logArgument is not Expr.Call { Arguments: [Expr.Variable formatted] } format || formatted.Name != errorBinding.Name
                || new[] { defaults, merge, timeout, handshake, closeSsl, closeOptions, closeError, format }.Any(call =>
                    call.Module is null || call.DynamicRemote || owner.ResolvesErlangBif(call))
                || !AttributeLiteral.TryNormalizeConstant(additions with { Items = additions.Items.Skip(1).ToArray() }, out Term optionTail)
                || !localFunctions.TryGetValue((verify.Function, 2), out var verifier)
                || ReadCertificateNameCheck(verifier) is not { } verifiedShape
                || !ok.Equals(verifiedShape.Ok) || !denied.Equals(verifiedShape.Error)
                || verifiedShape.Rejected is not TupleTerm { Items: [Atom rejectedTag, _] } || !denied.Equals(rejectedTag)
                || !entries.TryGetValue((verify.Function, 2), out int verifyEntry))
            {
                return false;
            }

            int familyField = -1;
            for (int index = 1; index < record.Items.Count; index++)
            {
                if (record.Items[index] is Pattern.Variable variable && variable.Name == family.Name && familyField < 0)
                {
                    familyField = index;
                }
                else if (record.Items[index] is not Pattern.Variable { Name: VariableScopeNames.Wildcard })
                {
                    return false;
                }
            }
            if (familyField < 0)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int accepted = Label();
            int verifiedOk = Label();
            int handshakeError = Label();
            int writeLog = Label();
            int finishOptions = Label();
            int finishError = Label();
            int badLog = Label();
            int badHandshake = Label();
            EmitDetailRecordTest(0, record.Items.Count, recordTag, functionInfo);
            owner.Op(Allocate, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.InitStackSlot(0);
            for (int register = 3; register >= 0; register--)
            {
                owner.MoveRegister(XRegisterTag, register, YRegisterTag, 4 - register);
            }
            owner.MoveToX0(XRegisterTag, 2);
            var previous = location;
            EmitWindowCall(defaults, ref previous);
            Heap(ConsHeapWords, 1);
            LoadTupleField(4, familyField, XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 1);
            owner.LiteralOperand(optionTail);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(Nil.Value, 2);
            owner.InitStackSlot(4);
            EmitWindowCall(merge, ref previous);
            SaveWindowResult(4);
            EmitWindowCall(timeout, ref previous);
            Load(4, 1);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 2);
            owner.InitStackSlot(4);
            Load(3);
            EmitWindowCall(handshake, ref previous);
            SaveWindowResult(4);
            owner.EmitTupleCheck(0, 2, badHandshake);
            owner.EmitTuplePart(0, 0, 0);
            LoadTupleField(4, 1, YRegisterTag, 0);
            if (TermOrder.Compare(error, ok, exact: true) < 0)
            {
                owner.EmitAtomSelection(0, badHandshake, error.Name, handshakeError, ok.Name, accepted);
            }
            else
            {
                owner.EmitAtomSelection(0, badHandshake, ok.Name, accepted, error.Name, handshakeError);
            }
            Mark(accepted);
            Load(2, 1);
            InitPair(3, 4);
            Load(0);
            EmitWindowLocal(verify, verifyEntry, ref previous);
            SaveWindowResult(4);
            owner.Op(IsTuple, LabelTag, verifiedOk);
            owner.EmitTypedRegister(0, AtomTypeBits | TupleTypeBits);
            Load(0);
            TrimToReason();
            EmitWindowCall(closeSsl, ref previous);
            Heap(3, 0);
            LoadTupleField(0, 1, XRegisterTag, 0);
            Exit(deniedExit, deniedExitTag, XRegisterTag);
            Mark(verifiedOk);
            Heap(6, 0);
            owner.EmitTagMatchTuple(0, (YRegisterTag, 2), (YRegisterTag, 1));
            owner.EmitTagMatchTuple(0, (YRegisterTag, 0), (XRegisterTag, 0));
            owner.EmitFrameReturn(5);
            Mark(handshakeError);
            owner.Op(IsTaggedTuple, LabelTag, finishError);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(optionTag);
            owner.EmitConstant(allowLog.Arguments[1], 1);
            InitPair(1, 2);
            owner.EmitConstant(logLevel, 0);
            EmitWindowCall(allowLog, ref previous);
            owner.EmitAtomSelection(0, badLog, "false", finishOptions, "true", writeLog);
            Mark(writeLog);
            Load(4);
            owner.InitStackSlot(4);
            EmitWindowCall(format, ref previous);
            Heap(ConsHeapWords, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 3);
            owner.EmitConstant(logLevel, 1);
            owner.EmitConstant(logFormat, 2);
            owner.EmitConstant(logLocation, 0);
            owner.EmitCallLine(allowLog, previous);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("logger", "macro_log", 4));
            previous = CallLocation(allowLog, previous);
            Mark(finishOptions);
            CloseAndExit(closeOptions, optionsExit, optionsExitTag);
            Mark(finishError);
            CloseAndExit(closeError, errorExit, errorExitTag);
            Mark(badLog);
            owner.EmitLine(CallLocation(allowLog, location));
            owner.Op(CaseEnd, XRegisterTag, 0);
            Mark(badHandshake);
            owner.EmitLine(new(decision.PreprocessorFile ?? location.File, decision.SourceLine));
            owner.Op(CaseEnd, YRegisterTag, 4);

            return true;

            void Heap(int words, int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, words);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            void InitPair(int first, int second)
            {
                owner.instructions.WriteByte(InitYRegisters);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Operand(YRegisterTag, first);
                owner.Operand(YRegisterTag, second);
            }

            void TrimToReason()
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, 4);
                owner.Operand(BeamWire.UnsignedTag, 1);
            }

            void Exit(Expr.Call call, Atom tag, int reasonRegisterTag)
            {
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (reasonRegisterTag, 0));
                owner.EmitCallLine(call, previous);
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Exit, 1));
                owner.Operand(BeamWire.UnsignedTag, 1);
            }

            void CloseAndExit(Expr.Call close, Expr.Call exit, Atom tag)
            {
                Load(3);
                owner.MoveRegister(YRegisterTag, 0, YRegisterTag, 4);
                TrimToReason();
                EmitWindowCall(close, ref previous);
                Heap(3, 0);
                Exit(exit, tag, YRegisterTag);
            }
        }
    }
}
