using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record CertificateNameCheck(
            Expr.Call Allowed,
            Expr.Call Peer,
            Expr.Call Decode,
            Expr.Call Hosts,
            Expr.Call Projection,
            Expr.Call Verify,
            Expr.Call AllowLog,
            Expr.Case Verification,
            Term LogLocation,
            Term LogLevel,
            Term LogFormat,
            Term Rejected,
            Atom DecodeMode,
            Atom AddressTag,
            Atom MissingCertificate,
            Atom Ok,
            Atom Error
        );

        private CertificateNameCheck? ReadCertificateNameCheck(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable socket, Pattern.Variable ip],
                Body: Expr.Sequence { Items: [
                    Expr.Match { Pattern: Pattern.Tuple { Items: [
                        Pattern.Literal { Value: Atom ok }, Pattern.Variable allowed
                    ] }, Value: Expr.Call { Arguments: [] } allowedCall },
                    Expr.Case { Value: Expr.Variable configured, Clauses: [
                        { Guard: null, Patterns: [Pattern.List { Items: [], Tail: null } or Pattern.Literal { Value: Nil }],
                            Body: Expr.Literal { Value: Atom unrestricted } },
                        { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                            Body: Expr.Case { Value: Expr.Call { Arguments: [Expr.Variable peerSocket] } peerCall, Clauses: [
                                { Guard: null, Patterns: [Pattern.Tuple { Items: [
                                    Pattern.Literal { Value: Atom peerOk }, Pattern.Variable der
                                ] }], Body: Expr.Sequence { Items: [
                                    Expr.Match { Pattern: Pattern.Variable certificate, Value: Expr.Call
                                        { Arguments: [Expr.Variable decodeArg, Expr.Literal { Value: Atom mode }] } decode },
                                    Expr.Match { Pattern: Pattern.Variable hosts, Value: Expr.Call
                                        { Arguments: [Expr.Variable hostsArg] } hostCall },
                                    Expr.Case { Value: Expr.Call { Arguments: [
                                        Expr.Variable certArg, Expr.List { Items: [
                                            Expr.Tuple { Items: [Expr.Literal { Value: Atom ipTag }, Expr.Variable ipArg] }
                                        ], Tail: Expr.Call { Module: null, DynamicRemote: false,
                                            Arguments: [Expr.Variable projectionArg] } projection }
                                    ] } verify, Clauses: [
                                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom verified }],
                                            Body: Expr.Literal { Value: Atom verifiedResult } },
                                        { Guard: null, Patterns: [Pattern.Literal { Value: Atom rejected }],
                                            Body: Expr.Sequence { Items: [Expr.Case log, var rejectedResult] } }
                                    ] } verification
                                ] } },
                                { Guard: null, Patterns: [Pattern.Tuple { Items: [
                                    Pattern.Literal { Value: Atom error }, Pattern.Literal { Value: Atom missing }
                                ] }], Body: Expr.Literal { Value: Atom missingResult } },
                                { Guard: null, Patterns: [AliasPattern errorAlias], Body: Expr.Variable returned }
                            ] } }
                    ] }
                ] }
            }] || !TryReadPatternAlias(errorAlias, out var errorBinding, out var shape)
                || shape is not Pattern.Tuple { Items: [
                    Pattern.Literal { Value: Atom returnedError }, Pattern.Variable { Name: VariableScopeNames.Wildcard }
                ] } || errorBinding.Name != returned.Name || !error.Equals(returnedError) || error.Equals(ok)
                || !ok.Equals(unrestricted) || !ok.Equals(peerOk) || !ok.Equals(verifiedResult) || !ok.Equals(missingResult)
                || verified.Name != "true" || rejected.Name != "false"
                || !DistinctBindings(socket, ip, allowed, der, certificate, hosts)
                || !DistinctBindings(socket, ip, allowed, errorBinding)
                || configured.Name != allowed.Name || peerSocket.Name != socket.Name || decodeArg.Name != der.Name
                || hostsArg.Name != allowed.Name || certArg.Name != certificate.Name || ipArg.Name != ip.Name
                || projectionArg.Name != hosts.Name || projection.Function != MappingName(function.Name, function.Arity)
                || new[] { allowedCall, peerCall, decode, hostCall, verify }.Any(call =>
                    call.Module is null || call.DynamicRemote || owner.ResolvesErlangBif(call))
                || !AttributeLiteral.TryNormalizeConstant(rejectedResult, out Term rejection)
                || !ReadCertificateLog(log, ip.Name, out var allowLog, out Term logLocation, out Term level, out Term format))
            {
                return null;
            }

            return new(allowedCall, peerCall, decode, hostCall, projection, verify, allowLog, verification,
                logLocation, level, format, rejection, mode, ipTag, missing, ok, error);
        }

        private static bool ReadCertificateLog(
            Expr.Case expression, string ip, out Expr.Call allow, out Term location, out Term level, out Term format
        ) => ReadFixedLog(expression, out allow, out location, out level, out format, out Expr argument)
            && argument is Expr.Variable variable && variable.Name == ip;

        private static bool ReadFixedLog(
            Expr.Case expression, out Expr.Call allow, out Term location, out Term level, out Term format, out Expr argument
        )
        {
            allow = null!;
            argument = null!;
            location = level = format = Nil.Value;
            if (expression is not {
                Value: Expr.Call { Module: "logger", Function: "allow", DynamicRemote: false,
                    Arguments: [Expr.Literal { Value: Atom allowLevel }, Expr.Literal { Value: Atom }] } test,
                Clauses: [
                    { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "true" } }],
                        Body: Expr.Call { Module: ErlangModuleNames.Module, Function: AutoImportNames.Apply,
                            DynamicRemote: false, Arguments: [
                                Expr.Literal { Value: Atom { Name: "logger" } },
                                Expr.Literal { Value: Atom { Name: "macro_log" } },
                                Expr.List logArguments
                            ] } },
                    { Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } }],
                        Body: Expr.Literal { Value: Atom { Name: "ok" } } }
                ]
            })
            {
                return false;
            }

            var arguments = logArguments.Items.ToList();
            Expr? tail = logArguments.Tail;
            while (tail is Expr.List rest)
            {
                arguments.AddRange(rest.Items);
                tail = rest.Tail;
            }
            if (tail is not null || arguments is not [
                var metadata, Expr.Literal { Value: Atom logLevel }, var message,
                Expr.List { Tail: null, Items: [var value] }
            ] || !logLevel.Equals(allowLevel)
                || !AttributeLiteral.TryNormalizeConstant(metadata, out location)
                || !AttributeLiteral.TryNormalizeConstant(message, out format))
            {
                return false;
            }

            allow = test;
            level = logLevel;
            argument = value;
            return true;
        }

        private bool TryEmitCertificateNameCheck(FunctionDefinition function)
        {
            if (ReadCertificateNameCheck(function) is not { } flow
                || !entries.TryGetValue((flow.Projection.Function, 1), out int projection))
            {
                return false;
            }

            int decode = Label();
            int reject = Label();
            int writeLog = Label();
            int rejectReturn = Label();
            int peerError = Label();
            int allow = Label();
            int peerReturn = Label();
            int loggerFailed = Label();
            int verificationFailed = Label();
            int peerFailed = Label();
            int allowedFailed = Label();
            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.InitStackSlot(0);
            owner.MoveRegister(XRegisterTag, 1, YRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 2);
            var previous = location;
            EmitWindowCall(flow.Allowed, ref previous);
            EmitDetailRecordTest(0, 2, flow.Ok, allowedFailed);
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 0);
            owner.Op(ExactNotEqualTest, LabelTag, allow);
            owner.Operand(YRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            Load(2);
            owner.InitStackSlot(2);
            EmitWindowCall(flow.Peer, ref previous);
            owner.EmitTupleCheck(0, 2, peerFailed);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitTuplePart(0, 1, 2);
            if (TermOrder.Compare(flow.Error, flow.Ok, exact: true) < 0)
            {
                owner.EmitAtomSelection(1, peerFailed, flow.Error.Name, peerError, flow.Ok.Name, decode);
            }
            else
            {
                owner.EmitAtomSelection(1, peerFailed, flow.Ok.Name, decode, flow.Error.Name, peerError);
            }
            Mark(decode);
            owner.EmitConstant(flow.DecodeMode, 1);
            owner.MoveToX0(XRegisterTag, 2);
            EmitWindowCall(flow.Decode, ref previous);
            SaveWindowResult(2);
            Load(0);
            owner.InitStackSlot(0);
            EmitWindowCall(flow.Hosts, ref previous);
            EmitWindowLocal(flow.Projection, projection, ref previous);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3 + ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTagMatchTuple(1, (AtomTag, owner.Intern(flow.AddressTag.Name)), (YRegisterTag, 1));
            owner.Op(PutList, XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            Load(2);
            owner.InitStackSlot(2);
            EmitWindowCall(flow.Verify, ref previous);
            owner.EmitAtomSelection(0, verificationFailed, "false", reject, "true", allow);
            Mark(reject);
            owner.EmitConstant(flow.AllowLog.Arguments[1], 1);
            owner.EmitConstant(flow.LogLevel, 0);
            EmitWindowCall(flow.AllowLog, ref previous);
            owner.EmitAtomSelection(0, loggerFailed, "false", rejectReturn, "true", writeLog);
            Mark(writeLog);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Op(PutList, YRegisterTag, 1);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 3);
            owner.EmitConstant(flow.LogLevel, 1);
            owner.EmitConstant(flow.LogFormat, 2);
            owner.InitStackSlot(1);
            owner.EmitConstant(flow.LogLocation, 0);
            owner.Op(ExternalCall, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, owner.Import("logger", "macro_log", 4));
            Mark(rejectReturn);
            owner.EmitConstant(flow.Rejected, 0);
            owner.EmitFrameReturn(3);
            Mark(peerError);
            owner.Op(ExactEqualTest, LabelTag, peerReturn);
            owner.Operand(XRegisterTag, 2);
            owner.LiteralOperand(flow.MissingCertificate);
            Mark(allow);
            owner.EmitConstant(flow.Ok, 0);
            owner.EmitFrameReturn(3);
            Mark(peerReturn);
            owner.EmitFrameReturn(3);
            Mark(loggerFailed);
            owner.Op(CaseEnd, XRegisterTag, 0);
            Mark(verificationFailed);
            owner.EmitLine(new(flow.Verification.PreprocessorFile ?? location.File, flow.Verification.SourceLine));
            owner.Op(CaseEnd, XRegisterTag, 0);
            Mark(peerFailed);
            owner.EmitLine(CallLocation(flow.Peer, location));
            owner.Op(CaseEnd, XRegisterTag, 0);
            Mark(allowedFailed);
            owner.EmitLine(CallLocation(flow.Allowed, location));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;
        }
    }
}
