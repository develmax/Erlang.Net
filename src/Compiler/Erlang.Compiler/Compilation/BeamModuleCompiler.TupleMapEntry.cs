using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitTupleMapEntry(
            Clause clause,
            int tupleInput,
            int failure,
            int baseSlot
        )
        {
            // The surrounding tuple-arity dispatch has already proved the tuple shape.
            if (owner.ModuleOptimizationDisabled || clause.Patterns.Count != 2 || clause.Guard is not null
                || clause.Body is not Expr.Sequence { Items: [_, .., Expr.Tuple] }
                || EntryShape(clause.Patterns[tupleInput]) is not Pattern.Tuple tuple)
            {
                return false;
            }

            int mapInput = 1 - tupleInput;
            Pattern mapPattern = clause.Patterns[mapInput];
            var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
            if (!ReadAlias(clause.Patterns[tupleInput], tupleInput, out _)
                || !ReadAlias(mapPattern, mapInput, out Pattern shape)
                || shape is not MapPattern map || ReadAtomMapFields(map) is not { Length: > 0 } fields)
            {
                return false;
            }

            var literalTests = new List<(int Position, Term Value)>();
            var tupleNames = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int index = 0; index < tuple.Items.Count; index++)
            {
                switch (tuple.Items[index])
                {
                    case Erlang.Pattern.Any { } or Pattern.Variable { Name: VariableScopeNames.Wildcard }:
                        break;
                    case Pattern.Literal literal:
                        literalTests.Add((index, literal.Value));
                        break;
                    case Pattern.Variable binding when !inputs.ContainsKey(binding.Name) && tupleNames.TryAdd(binding.Name, index):
                        break;
                    default:
                        return false;
                }
            }

            var lookups = new List<(Atom Key, int Register)>();
            int next = clause.Patterns.Count;
            foreach (var (key, binding) in fields.Reverse())
            {
                if (binding.Name != VariableScopeNames.Wildcard && !inputs.TryAdd(binding.Name, next))
                {
                    return false;
                }

                lookups.Add((key, next++));
            }

            string[] names = [.. inputs.Keys, .. tupleNames.Keys.Where(name => !inputs.ContainsKey(name))];
            var candidates = names.Select(name => new Pattern.Variable(name)).ToArray();
            if (ReadUsedEntryParameters(new Clause(candidates, null, clause.Body)) is not { } used)
            {
                return false;
            }

            var projections = new List<(int Position, int Register, int? Existing)>();
            foreach (var (name, position) in tupleNames)
            {
                if (inputs.TryGetValue(name, out int existing))
                {
                    projections.Add((position, next++, existing));
                }
                else if (used.Contains(name))
                {
                    projections.Add((position, next, null));
                    inputs.Add(name, next++);
                }
            }

            if (next >= FirstReservedXRegister)
            {
                return false;
            }

            foreach (var (position, value) in literalTests)
            {
                int scratch = clause.Patterns.Count;
                owner.EmitTuplePart(tupleInput, position, scratch);
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, scratch);
                owner.LiteralOperand(value);
            }

            EmitMapTest(mapInput, failure);
            owner.Op(GetMapElements, LabelTag, failure);
            owner.EmitTypedRegister(mapInput, MapTypeBits);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, lookups.Count * 2);
            foreach (var (key, register) in lookups)
            {
                owner.LiteralOperand(key);
                owner.Operand(XRegisterTag, register);
            }

            foreach (var (position, register, existing) in projections)
            {
                owner.EmitTuplePart(tupleInput, position, register);
                if (existing is int previouslyBound)
                {
                    owner.Op(ExactEqualTest, LabelTag, failure);
                    owner.Operand(XRegisterTag, previouslyBound);
                    owner.Operand(XRegisterTag, register);
                }
            }

            Expr remaining = clause.Body;
            (Expr.Binary Expression, int Receiver, int Payload)? directSend = null;
            if (clause.Body is Expr.Sequence
                {
                    Items: [Expr.Binary { Operator: ErlangOperators.Send, Left: Expr.Variable recipient, Right: Expr.Variable message } send,
                        Expr.Tuple continuation]
                } && inputs.TryGetValue(recipient.Name, out int destination)
                && inputs.TryGetValue(message.Name, out int payloadInput)
                && ReadUsedEntryParameters(new Clause(candidates, null, continuation)) is { } survivors)
            {
                directSend = (send, destination, payloadInput);
                used = survivors;
                remaining = continuation;
            }

            var saved = inputs.Where(pair => used.Contains(pair.Key)).OrderBy(pair => pair.Value).ToArray();
            int live = Math.Max(clause.Patterns.Count, saved.Select(pair => pair.Value + 1).DefaultIfEmpty(0).Max());
            if (directSend is { } outgoing)
            {
                live = Math.Max(live, Math.Max(outgoing.Receiver, outgoing.Payload) + 1);
            }
            nextSlot = baseSlot;
            variables = new(StringComparer.Ordinal);
            EmitFrameAllocation(live, frameSize);
            EmitFrameInitialization(saved.Length, frameSize);
            foreach (var (name, register) in saved)
            {
                int slot = baseSlot + saved.Length - 1 - (Slot() - baseSlot);
                owner.MoveRegister(
                    XRegisterTag,
                    register,
                    YRegisterTag,
                    slot
                );
                variables.Add(name, slot);
            }

            if (directSend is { } outgoingSend)
            {
                owner.EmitOrderedTailMoves([(0, outgoingSend.Receiver), (1, outgoingSend.Payload)], []);
                var previous = location;
                owner.EmitArithmeticLine(outgoingSend.Expression.SourceLine, ref previous);
                owner.instructions.WriteByte(Send);
            }

            if (!TailLocal(remaining))
            {
                Expression(remaining);
                owner.EmitFrameReturn(frameSize);
            }

            return true;

            bool ReadAlias(Pattern original, int register, out Pattern underlying)
            {
                underlying = original;
                if (original is not AliasPattern alias)
                {
                    return true;
                }

                if (!TryReadPatternAlias(alias, out var variable, out underlying))
                {
                    return false;
                }

                return variable.Name == VariableScopeNames.Wildcard || inputs.TryAdd(variable.Name, register);
            }
        }
    }
}
