using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TaggedCall(Expr.Match Match, Pattern.Variable Binding, Expr.Call Call);

        private static TaggedCall? ReadTaggedRemote(Expr expression) =>
            expression is Expr.Match
            {
                Pattern: Pattern.Tuple { Items: [Pattern.Literal { Value: Atom { Name: "ok" } }, Pattern.Variable binding] },
                Value: Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false } call
            } match ? new(match, binding, call) : null;

        private static Expr.Call? ReadRemote(Expr expression) =>
            expression is Expr.Call { Module: not (null or ErlangModuleNames.Module), DynamicRemote: false } call ? call : null;

        private bool TryEmitSocketConnectSequence(FunctionDefinition function)
        {
            if (function.Clauses is not [{
                Guard: null, Patterns: [Pattern.Variable port],
                Body: Expr.Sequence { Items: [var getHost, var connect, var connected, var getCertificate,
                    var printCertificate, var receive, var printData, var closing, var close] }
            }] || ReadTaggedRemote(getHost) is not { Call.Arguments.Count: 0 } host
                || ReadTaggedRemote(connect) is not { } socket
                || socket.Call.Arguments is not [Expr.Variable hostArg, Expr.Variable portArg, Expr.Call
                {
                    Module: null, DynamicRemote: false, Arguments: [Expr.Literal mode]
                } options]
                || !entries.TryGetValue((options.Function, 1), out int optionsEntry)
                || ReadTaggedRemote(getCertificate) is not { } certificate
                || certificate.Call.Arguments is not [Expr.Variable certSocket]
                || ReadTaggedRemote(receive) is not { } data
                || data.Call.Arguments is not [Expr.Variable receiveSocket, Expr.Literal timeout]
                || ReadRemote(connected) is not { Arguments: [Expr.Literal connectedText] } printConnected
                || ReadRemote(printCertificate) is not { Arguments: [Expr.Literal certText, Expr.List
                {
                    Items: [Expr.Call decode], Tail: null
                }] } printCert
                || ReadRemote(decode) is null || decode.Arguments is not [Expr.Variable certArg, Expr.Literal decodeMode]
                || ReadRemote(printData) is not { Arguments: [Expr.Literal dataText, Expr.List
                {
                    Items: [Expr.Variable dataArg], Tail: null
                }] } printReceived
                || ReadRemote(closing) is not { Arguments: [Expr.Literal closingText] } printClosing
                || ReadRemote(close) is not { Arguments: [Expr.Variable closeSocket] } closeCall
                || !DistinctBindings(port, host.Binding, socket.Binding, certificate.Binding, data.Binding)
                || hostArg.Name != host.Binding.Name || portArg.Name != port.Name
                || certSocket.Name != socket.Binding.Name || receiveSocket.Name != socket.Binding.Name
                || closeSocket.Name != socket.Binding.Name || certArg.Name != certificate.Binding.Name || dataArg.Name != data.Binding.Name)
            {
                return false;
            }

            int badData = Label();
            int badCertificate = Label();
            int badSocket = Label();
            int badHost = Label();
            var previous = location;

            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 1);
            Clear(0);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 1);
            Remote(host.Call);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
            Check(badHost);
            owner.EmitConstant(mode.Value, 0);
            owner.EmitCallLine(options, previous);
            owner.EmitFieldCall(1, optionsEntry);
            previous = CallLocation(options, previous);
            Project(YRegisterTag, 0, XRegisterTag, 1);
            owner.MoveRegister(XRegisterTag, 0, XRegisterTag, 2);
            owner.MoveRegister(XRegisterTag, 1, XRegisterTag, 0);
            Load(1, 1);
            Clear(1);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            Remote(socket.Call);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
            Check(badSocket);
            owner.EmitConstant(connectedText.Value, 0);
            Remote(printConnected);
            Project(YRegisterTag, 0, YRegisterTag, 0);
            Load(0);
            Remote(certificate.Call);
            Check(badCertificate);
            Project(XRegisterTag, 0, XRegisterTag, 0);
            owner.EmitConstant(decodeMode.Value, 1);
            Remote(decode);
            EmitSingleton();
            owner.EmitConstant(certText.Value, 0);
            Remote(printCert);
            owner.EmitConstant(timeout.Value, 1);
            Load(0);
            Remote(data.Call);
            Check(badData);
            HeapForSingleton();
            Project(XRegisterTag, 0, XRegisterTag, 0);
            PutSingleton();
            owner.EmitConstant(dataText.Value, 0);
            Remote(printReceived);
            owner.EmitConstant(closingText.Value, 0);
            Remote(printClosing);
            Load(0);
            owner.EmitCallLine(closeCall, previous);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(closeCall.Module!, closeCall.Function, 1));
            owner.Operand(BeamWire.UnsignedTag, 1);
            BadMatch(badData, data.Match);
            BadMatch(badCertificate, certificate.Match);
            BadMatch(badSocket, socket.Match);
            BadMatch(badHost, host.Match);

            return true;

            void Remote(Expr.Call call)
            {
                owner.EmitCallLine(call, previous);
                Call(call.Module, call.Function, call.Arguments.Count);
                previous = CallLocation(call, previous);
            }

            void Check(int failure)
            {
                owner.Op(IsTaggedTuple, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Operand(AtomTag, owner.Intern("ok"));
            }

            void Project(int sourceTag, int source, int destinationTag, int destination)
            {
                owner.Op(GetTupleElement, sourceTag, source);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Operand(destinationTag, destination);
            }

            void Clear(int slot)
            {
                owner.instructions.WriteByte(InitYRegisters);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Operand(YRegisterTag, slot);
            }

            void HeapForSingleton()
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
            }

            void PutSingleton()
            {
                owner.Op(PutList, XRegisterTag, 0);
                owner.Operand(AtomTag, NilAtomIndex);
                owner.Operand(XRegisterTag, 1);
            }

            void EmitSingleton()
            {
                HeapForSingleton();
                PutSingleton();
            }

            void BadMatch(int label, Expr.Match match)
            {
                Mark(label);
                owner.EmitLine(new(location.File, match.SourceLine == 0 ? location.Line : match.SourceLine));
                owner.Op(BeamEmissionFormat.BadMatch, XRegisterTag, 0);
            }
        }
    }
}
