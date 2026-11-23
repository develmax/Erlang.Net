using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitDirectGuardedRecordAccess(FunctionDefinition function)
        {
            if (function.Clauses is not [
                { Patterns: [Pattern.Variable input], Guard: { } guard, Body: Expr.Literal accepted },
                { Patterns: [Pattern.Variable { }], Guard: null, Body: Expr.Literal rejected }
            ])
            {
                return false;
            }

            var (access, expected) = guard switch
            {
                Expr.RecordAccess field => (field, "true"),
                Expr.Binary
                {
                    Operator: ErlangOperators.ExactEqual, Left: Expr.RecordAccess field,
                    Right: Expr.Literal { Value: Atom atom }
                } => (field, atom.Name),
                _ => (null, null)
            };
            if (access?.Value is not Expr.Variable source || source.Name != input.Name)
            {
                return false;
            }

            int failure = Label();
            EmitDetailRecordTest(
                0,
                access.Arity,
                Term.A(access.Name),
                failure
            );
            owner.EmitTuplePart(0, access.Index, 0);
            owner.EmitAtomTest(0, expected!, failure);
            owner.EmitConstant(accepted.Value, 0);
            owner.instructions.WriteByte(Return);
            Mark(failure);
            owner.EmitConstant(rejected.Value, 0);
            owner.instructions.WriteByte(Return);

            return true;
        }

        private Dictionary<string, (int Slot, string Tag, int Arity)> checkedRecordBindings = new(StringComparer.Ordinal);

        private void RememberCheckedRecord(Pattern pattern)
        {
            if (pattern is AliasPattern alias && TryReadPatternAlias(alias, out var binding, out var shape)
                && shape is Erlang.Pattern.Tuple tuple)
            {
                if (HasTupleTag(tuple, out Atom tag) && variables.TryGetValue(binding.Name, out int slot))
                {
                    checkedRecordBindings[binding.Name] = (slot, tag.Name, tuple.Items.Count);
                }

                pattern = tuple;
            }

            // Pattern emission has already checked every member on this edge.
            // Keep the nested aliases in the same arm-local record scope.
            if (owner.SsaTypeInferenceEnabled && !owner.ModuleOptimizationDisabled && pattern is Erlang.Pattern.Tuple matched)
            {
                foreach (Pattern field in matched.Items)
                {
                    if (field is AliasPattern or Erlang.Pattern.Tuple)
                    {
                        RememberCheckedRecord(field);
                    }
                }
            }
        }

        private void RememberRecordGuard(Expr? guard)
        {
            if (owner.SsaTypeInferenceEnabled && !owner.ModuleOptimizationDisabled && guard is not null
                && owner.ReadRecordTest(guard) is { Value: Expr.Variable variable } record
                && SavedRecordSlot(variable) is not null)
            {
                RememberRecordShape(variable.Name, record.Name.Name, record.Arity);
            }
        }

        private void RememberRecordAccesses(Expr expression)
        {
            switch (expression)
            {
                case Expr.RecordAccess access:
                    RememberRecordAccesses(access.Value);
                    if (access.Value is Expr.Variable record)
                    {
                        RememberRecordShape(record.Name, access.Name, access.Arity);
                    }

                    break;
                case Expr.Match match:
                    RememberRecordAccesses(match.Value);
                    break;
                case Expr.Block block:
                    RememberRecordAccesses(block.Body);
                    break;
                case Expr.Call call:
                    foreach (Expr argument in call.Arguments)
                    {
                        RememberRecordAccesses(argument);
                    }

                    break;
            }
        }

        private void RememberRecordShape(string name, string tag, int arity)
        {
            if (variables.TryGetValue(name, out int slot)
                && (!checkedRecordBindings.TryGetValue(name, out var proof) || proof != (slot, tag, arity)))
            {
                checkedRecordBindings = new(checkedRecordBindings, StringComparer.Ordinal) { [name] = (slot, tag, arity) };
            }
        }

        private int? CheckedRecordSlot(Expr source, string tag, int arity) =>
            source is Expr.Variable variable
            && checkedRecordBindings.TryGetValue(variable.Name, out var proof)
            && proof.Tag == tag && proof.Arity == arity
            && variables.TryGetValue(variable.Name, out int slot) && slot == proof.Slot
                ? slot
                : null;

        private int? SavedRecordSlot(Expr source) => source is Expr.Variable variable
            && !deferredPairMembers.ContainsKey(variable.Name)
            && variables.TryGetValue(variable.Name, out int slot)
                ? slot : null;

        private int? PrepareRecordOperand(Expr value, int? guardFailure)
        {
            int? slot = SavedRecordSlot(value);
            if (slot is null)
            {
                Expression(value, guardFailure);
            }

            return slot;
        }

        private void EmitRecordAccess(Expr.RecordAccess access, int destination, int? guardFailure = null)
        {
            if (CheckedRecordSlot(access.Value, access.Name, access.Arity) is int recordSlot)
            {
                EmitSavedRecordField(recordSlot, access.Index, destination);

                return;
            }

            int? savedRecord = PrepareRecordOperand(access.Value, guardFailure);

            if (guardFailure is int failure)
            {
                owner.EmitRecordTest(
                    failure,
                    savedRecord.HasValue ? YRegisterTag : XRegisterTag,
                    savedRecord.GetValueOrDefault(),
                    Term.A(access.Name),
                    access.Arity
                );
            }
            else
            {
                EmitCheckedRecord(
                    access.Name,
                    access.Arity,
                    savedRecord,
                    new(access.PreprocessorFile, access.SourceLine)
                );
            }

            if (savedRecord is int savedSlot)
            {
                EmitSavedRecordField(savedSlot, access.Index, destination);
            }
            else
            {
                owner.EmitTuplePart(0, access.Index, destination);
            }
        }

        private bool TryLoadComputedCheckedRecordArguments(
            IReadOnlyList<Expr> expressions,
            out BeamSourceLocation previous
        )
        {
            previous = location;
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || owner.receiveScratchRegisters is not null
                || expressions.Count >= FirstReservedXRegister || expressions.FirstOrDefault() is not Expr.RecordAccess)
            {
                return false;
            }

            Expr.Call? producer = null;
            Expr.RecordAccess? producerField = null;
            int computed = -1;
            var fields = new List<(int Destination, int Slot, Expr.RecordAccess Field)>();
            var stack = new List<(int Destination, int Source)>();
            var constants = new List<(int Destination, Term Value)>();
            for (int destination = 0; destination < expressions.Count; destination++)
            {
                Expr expression = expressions[destination];
                if (expression is Expr.RecordAccess field
                    && CheckedRecordSlot(field.Value, field.Name, field.Arity) is int fieldSlot)
                {
                    fields.Add((destination, fieldSlot, field));
                }
                else if (expression is Expr.Call
                {
                    Module: null, DynamicRemote: false, Arguments: [Expr.RecordAccess input]
                } unary && producer is null && entries.ContainsKey((unary.Function, 1))
                    && CheckedRecordSlot(input.Value, input.Name, input.Arity) is not null)
                {
                    producer = unary;
                    producerField = input;
                    computed = destination;
                }
                else if (SavedRecordSlot(expression) is int saved)
                {
                    stack.Add((destination, saved));
                }
                else if (AttributeLiteral.TryNormalizeConstant(expression, out Term value))
                {
                    constants.Add((destination, value));
                }
                else
                {
                    return false;
                }
            }

            if (producer is null || producerField is null || computed <= 0 || fields.Count != 2
                || producerField.Value is not Expr.Variable record || fields.Any(projection =>
                    projection.Field.Value is not Expr.Variable alias || alias.Name != record.Name
                    || projection.Field.Name != producerField.Name || projection.Field.Arity != producerField.Arity)
                || CheckedRecordSlot(record, producerField.Name, producerField.Arity) is not int carrier)
            {
                return false;
            }

            // All other operands are already bound or checked. Project the unary
            // input at its original call and leave the other two reads until return.
            EmitSavedRecordField(carrier, producerField.Index, 0);
            owner.EmitCallLine(producer, location);
            Call(null, producer.Function, 1);
            previous = CallLocation(producer, location);
            int scratch = stack.Count == 0 ? expressions.Count : stack[0].Destination;
            foreach (var (destination, slot, field) in fields)
            {
                EmitSavedRecordField(slot, field.Index, destination == 0 ? scratch : destination);
            }

            owner.EmitOrderedTailMoves([(0, scratch), (computed, 0)], constants, stack);

            return true;
        }

        private bool TryLoadRecordFieldArguments(IReadOnlyList<Expr> expressions)
        {
            if (owner.receiveScratchRegisters is not null || expressions.Count > FirstReservedXRegister
                || expressions.Count(expression => expression is Expr.RecordAccess) < 2)
            {
                return false;
            }

            var operands = new (int Slot, Term? Constant, Expr.RecordAccess? Field)[expressions.Count];
            for (int index = 0; index < expressions.Count; index++)
            {
                Expr expression = expressions[index];
                var field = expression as Expr.RecordAccess;
                if ((field?.Value ?? expression) is Expr.Variable variable
                    && variables.TryGetValue(variable.Name, out int slot)
                    && !deferredPairMembers.ContainsKey(variable.Name))
                {
                    operands[index] = (slot, null, field);
                }
                else if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant))
                {
                    operands[index] = (0, constant, null);
                }
                else
                {
                    return false;
                }
            }

            // Only record checks can fail here. Keep their order and read from the saved
            // inputs so checks and projections leave earlier X arguments intact.
            var incomingRecords = checkedRecordBindings;
            for (int index = 0; index < operands.Length; index++)
            {
                if (operands[index] is not (var slot, _, { } field))
                {
                    continue;
                }

                if (CheckedRecordSlot(field.Value, field.Name, field.Arity) is null)
                {
                    EmitCheckedRecord(
                        field.Name,
                        field.Arity,
                        slot,
                        new(field.PreprocessorFile, field.SourceLine)
                    );
                    RememberRecordAccesses(field);
                }

                EmitSavedRecordField(slot, field.Index, index);
            }
            checkedRecordBindings = incomingRecords;

            for (int index = 0; index < operands.Length; index++)
            {
                var (slot, constant, field) = operands[index];
                if (constant is not null)
                {
                    owner.EmitConstant(constant, index);
                }
                else if (field is null)
                {
                    Load(slot, index);
                }
            }

            return true;
        }

        private void EmitSavedRecordField(int slot, int field, int destination)
        {
            owner.Op(GetTupleElement, YRegisterTag, slot);
            owner.Operand(BeamWire.UnsignedTag, field);
            owner.Operand(XRegisterTag, destination);
        }

        private void EmitCheckedRecord(
            string name,
            int arity,
            int? slot,
            BeamSourceLocation? failureLocation = null
        )
        {
            int failure = Label();
            int valid = Label();
            int tag = slot.HasValue ? YRegisterTag : XRegisterTag;
            int source = slot.GetValueOrDefault();
            owner.EmitRecordTest(
                failure,
                tag,
                source,
                Term.A(name),
                arity
            );
            JumpTo(valid);
            Mark(failure);
            if (failureLocation is { Line: > 0 } at)
            {
                BeamSourceLocation previous = owner.lastEmittedLine;
                owner.EmitLine(at);
                owner.lastEmittedLine = previous;
            }

            owner.Op(BadRecord, tag, source);
            Mark(valid);
        }

        private const int RecordUpdateArityLimit = 512;

        private bool TryEmitWrappedCheckedRecordUpdate(Expr.Tuple tuple)
        {
            if (tuple.Items.Count < 2 || tuple.Items[^1] is not Expr.RecordUpdate update
                || update.Fields.Count == 0
                || CheckedRecordSlot(update.Value, update.Name, update.Arity) is null)
            {
                return false;
            }

            var prefix = new Term[tuple.Items.Count - 1];
            for (int index = 0; index < prefix.Length; index++)
            {
                if (!AttributeLiteral.TryNormalizeConstant(tuple.Items[index], out prefix[index]))
                {
                    return false;
                }
            }

            EmitCheckedRecordUpdate(update, prefix);

            return true;
        }

        private void EmitCheckedRecordUpdate(Expr.RecordUpdate update, Term[]? tuplePrefix = null)
        {
            int operandBase = nextSlot;
            Expr? lastValue = update.Fields.Count == 0 ? null : update.Fields[^1].Value;
            bool nestedReplacement = tuplePrefix is not null && !owner.ModuleOptimizationDisabled
                && owner.SsaTypeInferenceEnabled && lastValue is Expr.RecordUpdate nested
                // Emitting the inner update checks a saved source before it returns the resulting tuple.
                && SavedRecordSlot(nested.Value) is not null;
            bool registerReplacement = tuplePrefix is not null && (lastValue is Expr.Call || nestedReplacement);
            int?[] values = new int?[update.Fields.Count];
            for (int index = 0; index < values.Length; index++)
            {
                if (registerReplacement && index == values.Length - 1)
                {
                    Expression(update.Fields[index].Value);
                }
                else
                {
                    values[index] = GuardSlot(update.Fields[index].Value, null);
                }
            }
            var source = ReadRecordUpdateSource(update);
            int? checkedSlot = CheckedRecordSlot(update.Value, update.Name, update.Arity);
            int? sourceSlot = checkedSlot ?? SavedRecordSlot(update.Value);

            var fields = update.Fields
                .Select((field, index) => (Field: field, Slot: values[index], Equal: ReadRecordFieldEquality(source, field)))
                .Where(entry => entry.Equal != true)
                .OrderBy(entry => entry.Field.Index)
                .ToArray();
            if (sourceSlot is null)
            {
                Expression(update.Value);
            }

            if (source is null && checkedSlot is null)
            {
                EmitCheckedRecord(
                    update.Name,
                    update.Arity,
                    sourceSlot,
                    new(update.FailureSourceFile, update.FailureSourceLine)
                );
            }

            if (fields.Length != 0)
            {
                string hint = fields.Length == 1 && fields[0].Equal != false && update.Arity < RecordUpdateArityLimit
                    && !PrefersCopyRecordOperand(fields[0].Field)
                    ? "reuse"
                    : "copy";
                owner.Op(TestHeap, BeamWire.UnsignedTag, update.Arity + 1 + (tuplePrefix is null ? 0 : tuplePrefix.Length + 2));
                owner.Operand(
                    BeamWire.UnsignedTag,
                    Math.Max(sourceSlot.HasValue && !registerReplacement ? 0 : 1, tuplePrefix is null ? 0 : ReceiveGuardLiveRegisters)
                );
                owner.Op(UpdateRecord, AtomTag, owner.Intern(hint));
                owner.Operand(BeamWire.UnsignedTag, update.Arity);
                owner.EmitTypedRegister(sourceSlot ?? 0, TupleTypeBits, registerTag: sourceSlot.HasValue ? YRegisterTag : XRegisterTag);
                owner.Operand(XRegisterTag, 0);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, fields.Length * 2);
                foreach (var entry in fields)
                {
                    owner.Operand(BeamWire.UnsignedTag, entry.Field.Index + 1);
                    if (nestedReplacement && ReferenceEquals(entry.Field, update.Fields[^1]))
                    {
                        owner.EmitTypedRegister(0, TupleTypeBits);
                    }
                    else if (registerReplacement && ReferenceEquals(entry.Field, update.Fields[^1]))
                    {
                        owner.Operand(XRegisterTag, 0);
                    }
                    else
                    {
                        GuardOperand(entry.Field.Value, entry.Slot);
                    }
                }
            }
            else if (sourceSlot is int slot)
            {
                Load(slot);
            }

            if (tuplePrefix is not null)
            {
                EmitRecordUpdateWrapper(tuplePrefix);
            }

            ReleaseOperandSlots(operandBase);
        }

        private void EmitRecordUpdateWrapper(Term[] prefix)
        {
            owner.EmitTupleHeader(XRegisterTag, prefix.Length + 1);
            foreach (Term item in prefix)
            {
                owner.LiteralOperand(item);
            }

            owner.Operand(XRegisterTag, 0);
        }


        private static IReadOnlyList<Expr>? ReadRecordUpdateSource(Expr.RecordUpdate update)
        {
            var items = update.Value switch
            {
                Expr.RecordConstruction record => RecordExpressions.Create(record).Items,
                Expr.Tuple tuple => tuple.Items,
                Expr.Literal { Value: TupleTerm tuple } => tuple.Items.Select(item => (Expr)new Expr.Literal(item)).ToArray(),
                _ => null
            };

            return items?.Count == update.Arity && items[0] is Expr.Literal { Value: Atom tag } && tag.Name == update.Name
                ? items
                : null;
        }

        private static bool? ReadRecordFieldEquality(IReadOnlyList<Expr>? source, RecordUpdateField field)
        {
            if (source is null)
            {
                return null;
            }

            Expr original = source[field.Index];
            if (original is Expr.Variable originalName && field.Value is Expr.Variable replacementName
                && originalName.Name == replacementName.Name)
            {
                return true;
            }

            return AttributeLiteral.TryNormalizeConstant(original, out Term oldValue)
                && AttributeLiteral.TryNormalizeConstant(field.Value, out Term newValue)
                ? oldValue.Equals(newValue)
                : null;
        }
    }
}
