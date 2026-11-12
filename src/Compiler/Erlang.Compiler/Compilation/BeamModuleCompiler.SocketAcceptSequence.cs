using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSocketAcceptSequence(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Patterns: [], Body: Expr.Sequence sequence }]
                || sequence.Items is not [
                    var startup, var listen, var address, var reportPort, var spawn, var accept,
                    var handshake, var accepted, var certificate, var reportCertificate,
                    var sending, var send, var receive, var closed, var closeClient,
                    var closing, var closeListener, var cleanup
                ]
                || ReadTaggedRemote(startup) is not { Call.Arguments: [Expr.Literal service] } started
                || ReadTaggedRemote(listen) is not { } listener
                || listener.Call.Arguments is not [Expr.Literal listenPort, Expr.Call
                {
                    Module: null, DynamicRemote: false, Arguments: [Expr.Literal mode]
                } options]
                || !entries.TryGetValue((options.Function, 1), out int optionsEntry)
                || address is not Expr.Match
                {
                    Pattern: Pattern.Tuple
                    {
                        Items: [Pattern.Literal { Value: Atom { Name: "ok" } },
                        Pattern.Tuple { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable port] }]
                    },
                    Value: Expr.Call addressCall
                } addressMatch || ReadRemote(addressCall) is null
                || !HasArgument(addressCall, listener.Binding)
                || ReadRemote(reportPort) is not
                {
                    Arguments: [Expr.Literal portText, Expr.List
                    {
                        Items: [Expr.Variable printedPort], Tail: null
                    }]
                } printPort || printedPort.Name != port.Name
                || spawn is not Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, DynamicRemote: false,
                    Arguments: [Expr.Literal { Value: Atom } clientModule, Expr.Literal { Value: Atom } clientFunction,
                        Expr.List { Items: [Expr.Variable clientPort], Tail: null }]
                } spawnCall || clientPort.Name != port.Name
                || spawnCall.Module is null && (!AutoImportSignatures.Contains(spawnCall.Function, 3)
                    || entries.ContainsKey((spawnCall.Function, 3)))
                || ReadTaggedRemote(accept) is not { } acceptedSocket || !HasArgument(acceptedSocket.Call, listener.Binding)
                || ReadTaggedRemote(handshake) is not { } socket || !HasArgument(socket.Call, acceptedSocket.Binding)
                || ReadRemote(accepted) is not { Arguments: [Expr.Literal acceptedText] } printAccepted
                || ReadTaggedRemote(certificate) is not { } cert || !HasArgument(cert.Call, socket.Binding)
                || ReadRemote(reportCertificate) is not
                {
                    Arguments: [Expr.Literal certText, Expr.List
                    {
                        Items: [Expr.Call decode], Tail: null
                    }]
                } printCert || ReadRemote(decode) is null
                || decode.Arguments is not [Expr.Variable certArg, Expr.Literal decodeMode] || certArg.Name != cert.Binding.Name
                || ReadRemote(sending) is not { Arguments: [Expr.Literal sendingText] } printSending
                || ReadRemote(send) is not { Arguments: [Expr.Variable sendSocket, Expr.Literal payload] } sendCall
                || sendSocket.Name != socket.Binding.Name
                || receive is not Expr.Match
                {
                    Pattern: Pattern.Tuple { Items: [Pattern.Literal { Value: Atom resultTag }, Pattern.Literal { Value: Atom reason }] },
                    Value: Expr.Call receiveCall
                } receiveMatch || ReadRemote(receiveCall) is null
                || receiveCall.Arguments is not [Expr.Variable receiveSocket, Expr.Literal timeout] || receiveSocket.Name != socket.Binding.Name
                || ReadRemote(closed) is not { Arguments: [Expr.Literal closedText] } printClosed
                || ReadRemote(closeClient) is not { } closeSocket || !HasArgument(closeSocket, socket.Binding)
                || ReadRemote(closing) is not { Arguments: [Expr.Literal closingText] } printClosing
                || ReadRemote(closeListener) is not { } closeListen || !HasArgument(closeListen, listener.Binding)
                || ReadRemote(cleanup) is not { Arguments: [var callback, Expr.Call reverse] } cleanupCall
                || ReadExternalFunctionLiteral(callback) is not { } external
                || ReadRemote(reverse) is null || !HasArgument(reverse, started.Binding)
                || !DistinctBindings(
                    started.Binding,
                    listener.Binding,
                    port,
                    acceptedSocket.Binding,
                    socket.Binding,
                    cert.Binding
                ))
            {
                return false;
            }

            int badReceive = Label();
            int badCert = Label();
            int badHandshake = Label();
            int badAccept = Label();
            int badAddress = Label();
            int badListen = Label();
            int badStartup = Label();
            var previous = location;

            owner.Op(Allocate, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 0);
            Clear(0, 1, 2);
            owner.EmitConstant(service.Value, 0);
            Invoke(started.Call);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                2
            );
            CheckTag(badStartup);
            owner.EmitConstant(mode.Value, 0);
            Line(options);
            owner.EmitFieldCall(1, optionsEntry);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(listenPort.Value, 0);
            Invoke(listener.Call);
            CheckTag(badListen);
            Project(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );

            Load(1);
            Invoke(addressCall);
            CheckTag(badAddress);
            Project(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.Op(IsTuple, LabelTag, badAddress);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TestArity, LabelTag, badAddress);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 2);
            HeapForSingleton(2);
            Project(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            Singleton(YRegisterTag, 1);
            owner.EmitConstant(portText.Value, 0);
            Invoke(printPort);
            HeapForSingleton(0);
            Singleton(YRegisterTag, 2);
            owner.EmitConstant(clientFunction.Value, 1);
            Clear(0);
            owner.EmitConstant(clientModule.Value, 0);
            Invoke(spawnCall);

            Load(1);
            Invoke(acceptedSocket.Call);
            CheckTag(badAccept);
            Project(
                XRegisterTag,
                0,
                XRegisterTag,
                0
            );
            Invoke(socket.Call);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            CheckTag(badHandshake);
            owner.EmitConstant(acceptedText.Value, 0);
            Invoke(printAccepted);
            Project(
                YRegisterTag,
                0,
                YRegisterTag,
                0
            );
            Load(0);
            Invoke(cert.Call);
            CheckTag(badCert);
            Project(
                XRegisterTag,
                0,
                XRegisterTag,
                0
            );
            owner.EmitConstant(decodeMode.Value, 1);
            Invoke(decode);
            HeapForSingleton(1);
            Singleton(XRegisterTag, 1);
            owner.EmitConstant(certText.Value, 0);
            Invoke(printCert);

            owner.EmitConstant(sendingText.Value, 0);
            Invoke(printSending);
            owner.EmitConstant(payload.Value, 1);
            Load(0);
            Invoke(sendCall);
            owner.EmitConstant(timeout.Value, 1);
            Load(0);
            Invoke(receiveCall);
            owner.Op(ExactEqualTest, LabelTag, badReceive);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(Term.Tuple(resultTag, reason));
            owner.EmitConstant(closedText.Value, 0);
            Invoke(printClosed);
            Load(0);
            Trim(2);
            Invoke(closeSocket);
            owner.EmitConstant(closingText.Value, 0);
            Invoke(printClosing);
            Load(0);
            Trim(1);
            Invoke(closeListen);
            Project(
                YRegisterTag,
                0,
                XRegisterTag,
                0
            );
            Trim(0);
            Invoke(reverse);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.EmitConstant(external, 0);
            Line(cleanupCall);
            owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(cleanupCall.Module!, cleanupCall.Function, 2));
            owner.Operand(BeamWire.UnsignedTag, 0);

            Fail(badReceive, receiveMatch);
            Fail(badCert, cert.Match);
            Fail(badHandshake, socket.Match);
            Fail(badAccept, acceptedSocket.Match);
            Fail(badAddress, addressMatch);
            Fail(badListen, listener.Match);
            Fail(badStartup, started.Match);

            return true;

            static bool HasArgument(Expr.Call call, Pattern.Variable binding) =>
                call.Arguments is [Expr.Variable argument] && argument.Name == binding.Name;

            void Clear(params int[] slots)
            {
                owner.instructions.WriteByte(InitYRegisters);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, slots.Length);
                foreach (int slot in slots)
                {
                    owner.Operand(YRegisterTag, slot);
                }
            }

            void Line(Expr.Call call)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
            }

            void Invoke(Expr.Call call)
            {
                Line(call);
                Call(call.Module, call.Function, call.Arguments.Count);
            }

            void CheckTag(int failure)
            {
                owner.Op(IsTaggedTuple, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(BeamWire.UnsignedTag, 2);
                owner.Operand(AtomTag, owner.Intern("ok"));
            }

            void Project(
                int sourceTag,
                int source,
                int destinationTag,
                int destination
            )
            {
                owner.Op(GetTupleElement, sourceTag, source);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Operand(destinationTag, destination);
            }

            void HeapForSingleton(int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            void Singleton(int sourceTag, int destination)
            {
                owner.Op(PutList, sourceTag, 0);
                owner.Operand(AtomTag, NilAtomIndex);
                owner.Operand(XRegisterTag, destination);
            }

            void Trim(int remaining)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, remaining);
            }

            void Fail(int label, Expr.Match match)
            {
                Mark(label);
                owner.EmitLine(new(location.File, match.SourceLine == 0 ? location.Line : match.SourceLine));
                owner.Op(BeamEmissionFormat.BadMatch, XRegisterTag, 0);
            }
        }
    }
}
