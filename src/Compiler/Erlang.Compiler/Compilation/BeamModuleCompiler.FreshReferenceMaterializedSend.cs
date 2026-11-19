using AstPattern = Erlang.Pattern;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitFreshReferenceMaterializedSend(Expr.Call producer, string reference, Expr.Sequence continuation)
        {
            if (continuation.Items is not
                [
                    Expr.Match
                {
                    Pattern: AstPattern.Variable sender,
                    Value: Expr.Call { Function: AutoImportNames.Self, DynamicRemote: false, Arguments: [] } self
                },
                    Expr.Match { Pattern: AstPattern.Variable payload, Value: Expr.Tuple message },
                    Expr.Match
                {
                    Pattern: AstPattern.Variable recipient,
                    Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [] } recipientCall
                },
                    Expr.Call
                {
                    Module: ErlangModuleNames.Module,
                    DynamicRemote: false,
                    Function: ErlangBasicBifNames.Send,
                    Arguments: [Expr.Variable actualRecipient, Expr.Variable actualPayload]
                } send,
                    Expr.Receive receive
                ]
                || !owner.ResolvesErlangBif(self) || recipient.Name != actualRecipient.Name || payload.Name != actualPayload.Name
                || !TryResolveOrdinaryCallTarget(recipientCall, out string? module) || module is not null
                || ReadCallArgumentConstructions(
                    [message],
                    result: -1,
                    saved: variables,
                    inputs: new Dictionary<string, int>(StringComparer.Ordinal) { [reference] = 0, [sender.Name] = 1 },
                    preserveX0: true
                )?.Constructions is not [{ IsTuple: true, MapLive: null } tuple])
            {
                return false;
            }

            HashSet<string> reads = ReadFreshReceiveBindings(receive);
            if (!reads.Contains(recipient.Name) || reads.Contains(payload.Name) || reads.Contains(sender.Name))
            {
                return false;
            }

            var retainedSlots = variables.Where(binding => reads.Contains(binding.Key)).Select(binding => binding.Value).ToHashSet();
            int[] deadSlots = variables.Values.Distinct().Where(slot => !retainedSlots.Contains(slot)).ToArray();
            if (deadSlots.Length == 0)
            {
                return false;
            }

            int payloadSlot = deadSlots.Max();
            int referenceSlot = Slot();
            owner.Op(ReceiveMarkerReserve, YRegisterTag, referenceSlot);
            Expression(producer);
            owner.Op(TestHeap, BeamWire.UnsignedTag, tuple.HeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Self, 0));
            owner.Operand(XRegisterTag, 1);
            owner.EmitTupleHeader(YRegisterTag, tuple.Operands.Length, payloadSlot);
            foreach (var operand in tuple.Operands)
            {
                if (operand.Literal is { } literal)
                {
                    owner.LiteralOperand(literal);
                }
                else
                {
                    owner.Operand(operand.Tag, operand.Register);
                }
            }

            owner.Op(ReceiveMarkerBind, YRegisterTag, referenceSlot);
            owner.Operand(XRegisterTag, 0);
            SaveCurrentRegister(referenceSlot);
            foreach (int slot in deadSlots.Where(slot => slot != payloadSlot))
            {
                owner.InitStackSlot(slot);
            }

            owner.EmitCallLine(recipientCall, CallLocation(producer, location));
            Call(null, recipientCall.Function, 0);
            Load(payloadSlot, 1);
            SaveCurrentRegister(payloadSlot);
            owner.EmitCallLine(send, CallLocation(recipientCall, location));
            Call(ErlangModuleNames.Module, ErlangBasicBifNames.Send, 2);

            foreach (string discarded in variables.Keys.Where(name => !reads.Contains(name)).ToArray())
            {
                variables.Remove(discarded);
            }

            variables.Add(reference, referenceSlot);
            variables.Add(recipient.Name, payloadSlot);
            EmitReceive(receive, terminal: true, freshReferenceSlot: referenceSlot);

            return true;

            void SaveCurrentRegister(int slot)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    slot
                );
            }
        }
    }
}
