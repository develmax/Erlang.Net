using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record DeferredListValue(Term? Literal, int? Slot, Expr.Call? Call);
        private sealed record DeferredListHead(DeferredListValue? Value, DeferredListValue[]? Tuple);

        private bool TryEmitDeferredTupleListArgument(Expr.List list)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || owner.receiveScratchRegisters is not null
                || list.Tail is not null || list.Items.Count < 2)
            {
                return false;
            }

            var heads = new List<DeferredListHead>();
            Expr.Call? producer = null;
            Expr.Call? intrinsic = null;
            long heapWords = (long)list.Items.Count * ConsHeapWords;
            foreach (Expr item in list.Items)
            {
                if (ReadValue(item, allowCall: false) is { } value)
                {
                    heads.Add(new DeferredListHead(value, null));
                    continue;
                }

                if (item is not Expr.Tuple { Items.Count: > 0 } tuple)
                {
                    return false;
                }

                var members = new List<DeferredListValue>();
                bool hasCall = false;
                foreach (Expr member in tuple.Items)
                {
                    if (ReadValue(member, allowCall: !hasCall) is not { } operand)
                    {
                        return false;
                    }

                    hasCall |= operand.Call is not null;
                    members.Add(operand);
                }

                heapWords += tuple.Items.Count + 1L;
                heads.Add(new DeferredListHead(null, members.ToArray()));
            }

            if (producer is null || intrinsic is null || heapWords > int.MaxValue)
            {
                return false;
            }

            // Keep the producer and inline BIF in source order. The other
            // operands are already rooted in slots or are literals.
            Call(ErlangModuleNames.Module, producer.Function, 0);
            owner.Op(TestHeap, BeamWire.UnsignedTag, heapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            bool producerLive = true;
            int? tail = null;
            for (int index = heads.Count - 1; index >= 0; index--)
            {
                var head = heads[index];
                int? tupleRegister = null;
                bool consumesProducer = false;
                if (head.Tuple is { } tuple)
                {
                    Expr.Call? call = tuple.FirstOrDefault(member => member.Call is not null)?.Call;
                    consumesProducer = ReferenceEquals(call, producer);
                    int register = consumesProducer ? 0 : Scratch();
                    tupleRegister = register;
                    if (call is not null && !consumesProducer)
                    {
                        EmitZeroGuardBif(call.Function, register);
                    }

                    owner.EmitTupleHeader(XRegisterTag, tuple.Length, register);
                    foreach (DeferredListValue member in tuple)
                    {
                        WriteValue(member, register);
                    }
                }

                int destination = index == 0 || consumesProducer ? 0 : tail ?? tupleRegister ?? Scratch();
                owner.instructions.WriteByte(PutList);
                if (tupleRegister is int prepared)
                {
                    owner.Operand(XRegisterTag, prepared);
                }
                else
                {
                    WriteValue(head.Value!, 0);
                }

                if (tail is int chain)
                {
                    owner.Operand(XRegisterTag, chain);
                }
                else
                {
                    owner.LiteralOperand(Nil.Value);
                }

                owner.Operand(XRegisterTag, destination);
                tail = destination;
                producerLive &= !consumesProducer;
            }

            return true;

            int Scratch()
            {
                int first = producerLive ? 1 : 0;

                return first == tail ? first + 1 : first;
            }

            DeferredListValue? ReadValue(Expr source, bool allowCall)
            {
                if (AttributeLiteral.TryNormalizeConstant(source, out Term literal))
                {
                    return new DeferredListValue(literal, null, null);
                }

                if (source is Expr.Variable variable && variables.TryGetValue(variable.Name, out int slot)
                    && slot >= 0 && !deferredPairMembers.ContainsKey(variable.Name))
                {
                    return new DeferredListValue(null, slot, null);
                }

                if (!allowCall || source is not Expr.Call { DynamicRemote: false, Arguments.Count: 0 } call
                    || !owner.ResolvesErlangBif(call) || !IsSafeExternalBif(call.Function, 0))
                {
                    return null;
                }

                if (call.Function == AutoImportNames.MakeRef && producer is null)
                {
                    producer = call;

                    return new DeferredListValue(null, null, call);
                }

                // Self and node use the existing inline zero-arity BIF policy.
                // Admit the read only after the producer and only once.
                if (producer is not null && intrinsic is null && IsZeroGuardBif(call))
                {
                    intrinsic = call;

                    return new DeferredListValue(null, null, call);
                }

                return null;
            }

            void WriteValue(DeferredListValue value, int register)
            {
                if (value.Literal is { } literal)
                {
                    owner.LiteralOperand(literal);
                }
                else if (value.Slot is int slot)
                {
                    owner.Operand(YRegisterTag, slot);
                }
                else
                {
                    owner.Operand(XRegisterTag, register);
                }
            }
        }
    }
}
