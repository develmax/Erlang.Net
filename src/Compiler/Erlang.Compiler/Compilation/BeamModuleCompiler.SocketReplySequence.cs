using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSocketReplySequence(FunctionDefinition function)
        {
            if (function.Clauses is not [
                {
                    Guard: null, Patterns: [Pattern.List
                    {
                        Items: [Pattern.Variable address, Pattern.Variable port, Pattern.Variable message], Tail: null
                    }], Body: Expr.Sequence sequence
                },
                { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: var fallback }
            ] || sequence.Items is not [
                Expr.Match { Pattern: Pattern.Variable ip, Value: Expr.Call { Arguments: [Expr.Call addressText] } parseAddress },
                Expr.Match { Pattern: Pattern.Variable number, Value: Expr.Call { Arguments: [Expr.Call portText] } parsePort },
                Expr.Match { Pattern: Pattern.Variable payload, Value: Expr.Call messageText },
                var connect,
                Expr.Match { Pattern: Pattern.Literal { Value: Atom sentTag }, Value: Expr.Call send } sent,
                Expr.Call close,
                Expr.Literal result
            ] || ReadTaggedRemote(connect) is not { } socket
                || socket.Call.Arguments is not [Expr.Variable ipArg, Expr.Variable portArg, var options]
                || !AttributeLiteral.TryNormalizeConstant(options, out Term socketOptions)
                || send.Arguments is not [Expr.Variable sendSocket, Expr.Variable sendData]
                || close.Arguments is not [Expr.Variable closeSocket]
                || ReadRemote(send) is null || ReadRemote(close) is null
                || ReadRemote(fallback) is not { Arguments: [Expr.Literal fallbackText] } report
                || !Reads(addressText, address) || !Reads(portText, port) || !Reads(messageText, message)
                || parseAddress.DynamicRemote || parsePort.DynamicRemote
                || ipArg.Name != ip.Name || portArg.Name != number.Name || sendData.Name != payload.Name
                || sendSocket.Name != socket.Binding.Name || closeSocket.Name != socket.Binding.Name
                || !DistinctBindings(
                    address,
                    port,
                    message,
                    ip,
                    number,
                    payload,
                    socket.Binding
                ))
            {
                return false;
            }

            int other = Label();
            int badSend = Label();
            int badConnect = Label();
            for (int register = 1; register <= 3; register++)
            {
                owner.Op(IsNonemptyList, LabelTag, other);
                owner.Operand(XRegisterTag, 0);
                owner.Op(GetList, XRegisterTag, 0);
                owner.Operand(XRegisterTag, register);
                owner.Operand(XRegisterTag, 0);
            }
            owner.Op(IsNil, LabelTag, other);
            owner.Operand(XRegisterTag, 0);
            owner.Op(Allocate, BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.MoveRegister(
                XRegisterTag,
                3,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                1
            );
            owner.MoveToX0(XRegisterTag, 1);
            var previous = location;
            Invoke(addressText);
            Invoke(parseAddress);
            SwapY(1);
            Invoke(portText);
            Invoke(parsePort);
            SwapY(0);
            Invoke(messageText);
            SwapY(1);
            Load(0, 1);
            owner.EmitConstant(socketOptions, 2);
            owner.instructions.WriteByte(InitYRegisters);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 0);
            Invoke(socket.Call);
            owner.Op(IsTaggedTuple, LabelTag, badConnect);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(AtomTag, owner.Intern("ok"));
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Operand(YRegisterTag, 0);
            Load(1, 1);
            owner.MoveRegister(
                YRegisterTag,
                0,
                YRegisterTag,
                1
            );
            Trim(1);
            Load(0);
            Invoke(send);
            owner.Op(ExactEqualTest, LabelTag, badSend);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(sentTag);
            Load(0);
            Trim(0);
            Invoke(close);
            owner.EmitConstant(result.Value, 0);
            owner.EmitFrameReturn(0);

            Mark(other);
            owner.EmitConstant(fallbackText.Value, 0);
            owner.EmitCallLine(report, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(report.Module!, report.Function, 1));
            Fail(badSend, sent);
            Fail(badConnect, socket.Match);

            return true;

            static bool Reads(Expr.Call call, Pattern.Variable binding) =>
                !call.DynamicRemote && call.Arguments is [Expr.Variable variable] && variable.Name == binding.Name;

            void SwapY(int slot)
            {
                owner.Op(Swap, YRegisterTag, slot);
                owner.Operand(XRegisterTag, 0);
            }

            void Trim(int remaining)
            {
                owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, remaining);
            }

            void Invoke(Expr.Call call)
            {
                owner.EmitCallLine(call, previous);
                Call(call.Module, call.Function, call.Arguments.Count);
                previous = CallLocation(call, previous);
            }

            void Fail(int label, Expr.Match match)
            {
                Mark(label);
                owner.EmitLine(match.SourceLine == 0 ? location : new(match.PreprocessorFile, match.SourceLine));
                owner.Op(BadMatch, XRegisterTag, 0);
            }
        }
    }
}
