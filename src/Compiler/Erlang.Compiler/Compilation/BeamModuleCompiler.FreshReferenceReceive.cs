using AstPattern = Erlang.Pattern;
using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool ReceiveOptimizationEnabled => sourceModule?.DisableReceiveOptimization != true
        && sourceModule?.DisableModuleOptimization != true;

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitFreshReferenceReceive(
            Expr.Call producer,
            AstPattern.Variable reference,
            Expr.Sequence continuation,
            IReadOnlyList<AstPattern> parameters
        )
        {
            if (!owner.ReceiveOptimizationEnabled
                || producer is not { Function: AutoImportNames.MakeRef, Arguments: [], DynamicRemote: false }
                || !owner.ResolvesErlangBif(producer)
                || reference.Name == VariableScopeNames.Wildcard
                || variables.ContainsKey(reference.Name)
                || continuation.Items[^1] is not Expr.Receive
                {
                    Timeout: null,
                    After: null,
                    Clauses: [{ Guard: null, Patterns: [var pattern], Body: Expr.Variable or Expr.Literal }]
                } receive
                || !MatchesFreshReference(pattern, reference.Name)
                || ReadFreshReceiveBindingBoundary(continuation, reference.Name, parameters) is not int bindAt)
            {
                return false;
            }

            if (continuation.Items is [Expr.Binary { Operator: ErlangOperators.Send } directSend, Expr.Receive]
                && TryEmitFreshReferenceRegisterSend(
                    producer,
                    reference.Name,
                    directSend,
                    receive
                ))
            {
                return true;
            }

            if (TryEmitFreshReferenceMaterializedSend(producer, reference.Name, continuation))
            {
                return true;
            }

            int marker = Slot();
            owner.Op(ReceiveMarkerReserve, YRegisterTag, marker);
            Expression(producer);

            // OTP inserts the bind at the end of the successful definition block.
            // Only checked non-branching setup can run before the bind in this route.
            bool bindImmediately = bindAt == 0 && continuation.Items[0] is not Expr.Binary { Operator: ErlangOperators.Send };
            if (bindImmediately)
            {
                owner.Op(ReceiveMarkerBind, YRegisterTag, marker);
                owner.Operand(XRegisterTag, 0);
            }

            int referenceSlot = Save();
            variables.Add(reference.Name, referenceSlot);
            for (int index = 0; index + 1 < continuation.Items.Count; index++)
            {
                Expr item = continuation.Items[index];
                if (!bindImmediately && index == bindAt)
                {
                    if (item is Expr.Binary { Operator: ErlangOperators.Send } send)
                    {
                        EmitSend(send, EmitBinding);
                        continue;
                    }

                    EmitBinding();
                }

                DiscardSequenceItem(
                    continuation.Items,
                    index,
                    null,
                    reuseCallSlots: true
                );
                RememberRecordAccesses(item);
            }

            if (!bindImmediately && bindAt == continuation.Items.Count - 1)
            {
                EmitBinding();
            }

            EmitReceive(receive, terminal: true, freshReferenceSlot: referenceSlot);

            return true;

            void EmitBinding()
            {
                owner.Op(ReceiveMarkerBind, YRegisterTag, marker);
                owner.Operand(YRegisterTag, referenceSlot);
            }
        }

        private bool TryEmitFreshReferenceRegisterSend(
            Expr.Call producer,
            string reference,
            Expr.Binary send,
            Expr.Receive receive
        )
        {
            if (send.Right is not Expr.Tuple)
            {
                return false;
            }

            // This operand is a temporary for the construction planner and cannot be a source binding.
            const string selfOperand = "\0receive_self";
            bool needsSelf = false;
            Expr? destination = RegisterOperand(send.Left);
            Expr? message = RegisterOperand(send.Right);
            var inputs = new Dictionary<string, int>(StringComparer.Ordinal) { [reference] = 0 };
            if (needsSelf)
            {
                inputs.Add(selfOperand, 1);
            }

            if (destination is not (Expr.Variable or Expr.Literal) || message is null
                || ReadCallArgumentConstructions(
                    [destination, message],
                    result: -1,
                    saved: variables,
                    inputs: inputs,
                    preserveX0: true
                ) is not { } construction)
            {
                return false;
            }

            HashSet<string> readBindings = ReadFreshReceiveBindings(receive);
            var retainedSlots = variables.Where(binding => readBindings.Contains(binding.Key))
                .Select(binding => binding.Value).ToHashSet();
            int[] deadSlots = variables.Values.Distinct().Where(slot => !retainedSlots.Contains(slot)).ToArray();
            var finalSources = construction.Loads.Select(load => load.Source).ToHashSet();

            int marker = Slot();
            int referenceSlot = deadSlots.Where(slot => !finalSources.Contains(slot)).DefaultIfEmpty(marker).Max();
            owner.Op(ReceiveMarkerReserve, YRegisterTag, marker);
            Expression(producer);
            owner.Op(TestHeap, BeamWire.UnsignedTag, construction.Constructions.Sum(item => item.HeapWords));
            owner.Operand(BeamWire.UnsignedTag, 1);
            if (needsSelf)
            {
                owner.Op(BifZero, BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, AutoImportNames.Self, 0));
                owner.Operand(XRegisterTag, 1);
            }

            EmitCallArgumentConstructions(
                construction,
                -1,
                heapChecked: true,
                afterConstruction: BindAndKeepReference
            );
            foreach (int slot in deadSlots.Where(finalSources.Contains))
            {
                owner.InitStackSlot(slot);
            }

            var sendLocation = location;
            owner.EmitArithmeticLine(send.SourceLine, ref sendLocation);
            owner.instructions.WriteByte(Send);

            foreach (string discarded in variables.Keys.Where(name => !readBindings.Contains(name)).ToArray())
            {
                variables.Remove(discarded);
            }

            variables.Add(reference, referenceSlot);
            EmitReceive(receive, terminal: true, freshReferenceSlot: referenceSlot);

            return true;

            Expr? RegisterOperand(Expr value)
            {
                if (value is Expr.Call { Function: AutoImportNames.Self, Arguments: [], DynamicRemote: false } self
                    && owner.ResolvesErlangBif(self))
                {
                    needsSelf = true;

                    return new Expr.Variable(selfOperand);
                }

                if (value is Expr.Tuple nested)
                {
                    var fields = new List<Expr>(nested.Items.Count);
                    foreach (Expr item in nested.Items)
                    {
                        if (RegisterOperand(item) is not { } field)
                        {
                            return null;
                        }

                        fields.Add(field);
                    }

                    return nested with { Items = fields };
                }

                return value is Expr.Literal or Expr.Variable ? value : null;
            }

            void BindAndKeepReference()
            {
                owner.Op(ReceiveMarkerBind, YRegisterTag, marker);
                owner.Operand(XRegisterTag, 0);
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    YRegisterTag,
                    referenceSlot
                );

                if (referenceSlot != marker)
                {
                    owner.InitStackSlot(marker);
                }

                foreach (int slot in deadSlots.Where(slot => slot != referenceSlot && !finalSources.Contains(slot)))
                {
                    owner.InitStackSlot(slot);
                }
            }
        }

        private static HashSet<string> ReadFreshReceiveBindings(Expr.Receive receive)
        {
            var matched = receive.Clauses[0];
            var readBindings = new HashSet<string>(StringComparer.Ordinal);
            if (matched.Patterns[0] is AstPattern.Tuple tuple)
            {
                readBindings.UnionWith(tuple.Items.OfType<AstPattern.Variable>().Select(pattern => pattern.Name));
            }
            else if (matched.Patterns[0] is AstPattern.Variable pattern)
            {
                readBindings.Add(pattern.Name);
            }

            if (matched.Body is Expr.Variable returned)
            {
                readBindings.Add(returned.Name);
            }

            return readBindings;
        }

        private static bool MatchesFreshReference(AstPattern pattern, string reference) => pattern switch
        {
            AstPattern.Variable variable => variable.Name == reference,
            AstPattern.Tuple tuple => tuple.Items.Any(item => item is AstPattern.Variable field && field.Name == reference)
                && tuple.Items.All(item => item is AstPattern.Variable or AstPattern.Literal or AstPattern.Any),
            _ => false
        };

        private int? ReadFreshReceiveBindingBoundary(
            Expr.Sequence continuation,
            string reference,
            IReadOnlyList<AstPattern> parameters
        )
        {
            var bound = new HashSet<string>(variables.Keys, StringComparer.Ordinal) { reference };
            bound.UnionWith(parameters.OfType<AstPattern.Variable>().Select(parameter => parameter.Name));
            int boundary = 0;
            for (int index = 0; index + 1 < continuation.Items.Count; index++)
            {
                Expr item = continuation.Items[index];
                if (item is Expr.Match { Pattern: AstPattern.Variable binding, Value: var value })
                {
                    // An existing binding performs a match and can fail before the receive.
                    if (binding.Name == VariableScopeNames.Wildcard || !bound.Add(binding.Name))
                    {
                        return null;
                    }

                    if (IsFreshReceiveValue(value))
                    {
                        if (boundary == index)
                        {
                            boundary++;
                        }

                        continue;
                    }

                    if (!IsFreshReceiveCall(value))
                    {
                        return null;
                    }

                    continue;
                }

                if (item is Expr.Binary { Operator: ErlangOperators.Send } send
                    ? !IsFreshReceiveValue(send.Left) || !IsFreshReceiveValue(send.Right)
                    : !IsFreshReceiveCall(item))
                {
                    return null;
                }
            }

            return boundary;
        }

        private static bool IsFreshReceiveCall(Expr expression) =>
            expression is Expr.Call { DynamicRemote: false } call && call.Arguments.All(IsFreshReceiveTerm);

        private bool IsFreshReceiveValue(Expr expression) => expression switch
        {
            Expr.Call { Function: AutoImportNames.Self, Arguments: [], DynamicRemote: false } self =>
                owner.ResolvesErlangBif(self),
            Expr.Tuple tuple => tuple.Items.All(IsFreshReceiveValue),
            _ => IsFreshReceiveTerm(expression)
        };

        private static bool IsFreshReceiveTerm(Expr expression) => expression switch
        {
            Expr.Literal or Expr.Variable => true,
            Expr.Tuple tuple => tuple.Items.All(IsFreshReceiveTerm),
            _ => false
        };

        private void EmitSend(Expr.Binary send, Action? beforeSend = null)
        {
            int sendOperandBase = nextSlot;
            int[] operands = Values([send.Left, send.Right], null);
            Load(operands[0]);
            Load(operands[1], 1);
            var sendLocation = location;
            owner.EmitArithmeticLine(send.SourceLine, ref sendLocation);
            beforeSend?.Invoke();
            owner.instructions.WriteByte(Send);
            ReleaseOperandSlots(sendOperandBase);
        }
    }
}
