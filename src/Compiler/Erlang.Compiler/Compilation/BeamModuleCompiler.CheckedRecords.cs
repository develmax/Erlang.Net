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
                Expr.Binary { Operator: ErlangOperators.ExactEqual, Left: Expr.RecordAccess field,
                    Right: Expr.Literal { Value: Atom atom } } => (field, atom.Name),
                _ => (null, null)
            };
            if (access?.Value is not Expr.Variable source || source.Name != input.Name)
            {
                return false;
            }

            int failure = Label();
            EmitDetailRecordTest(0, access.Arity, Term.A(access.Name), failure);
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
                && shape is Pattern.Tuple tuple && HasTupleTag(tuple, out Atom tag)
                && variables.TryGetValue(binding.Name, out int slot))
            {
                checkedRecordBindings[binding.Name] = (slot, tag.Name, tuple.Items.Count);
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

        private void EmitRecordAccess(Expr.RecordAccess access, int destination, int? guardFailure = null)
        {
            if (CheckedRecordSlot(access.Value, access.Name, access.Arity) is int recordSlot)
            {
                EmitSavedRecordField(recordSlot, access.Index, destination);

                return;
            }

            Expression(access.Value, guardFailure);
            if (guardFailure is int failure)
            {
                EmitDetailRecordTest(
                    0,
                    access.Arity,
                    Term.A(access.Name),
                    failure
                );
            }
            else
            {
                EmitCheckedRecord(access.Name, access.Arity);
            }

            owner.EmitTuplePart(0, access.Index, destination);
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
                    EmitCheckedRecord(field.Name, field.Arity, slot);
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

        private void EmitCheckedRecord(string name, int arity, int? slot = null)
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
            owner.Op(BadRecord, tag, source);
            Mark(valid);
        }

        private const int RecordUpdateArityLimit = 512;

        private void EmitCheckedRecordUpdate(Expr.RecordUpdate update)
        {
            int operandBase = nextSlot;
            int?[] values = update.Fields.Select(field => GuardSlot(field.Value, null)).ToArray();
            var source = ReadRecordUpdateSource(update);
            int? sourceSlot = CheckedRecordSlot(update.Value, update.Name, update.Arity);

            var fields = update.Fields
                .Select((field, index) => (Field: field, Slot: values[index], Equal: ReadRecordFieldEquality(source, field)))
                .Where(entry => entry.Equal != true)
                .OrderBy(entry => entry.Field.Index)
                .ToArray();
            if (sourceSlot is null || fields.Length == 0)
            {
                Expression(update.Value);
            }

            if (source is null && sourceSlot is null)
            {
                EmitCheckedRecord(update.Name, update.Arity);
            }

            if (fields.Length != 0)
            {
                string hint = fields.Length == 1 && fields[0].Equal != false && update.Arity < RecordUpdateArityLimit
                    ? "reuse"
                    : "copy";
                owner.Op(TestHeap, BeamWire.UnsignedTag, update.Arity + 1);
                owner.Operand(BeamWire.UnsignedTag, sourceSlot.HasValue ? 0 : 1);
                owner.Op(UpdateRecord, AtomTag, owner.Intern(hint));
                owner.Operand(BeamWire.UnsignedTag, update.Arity);
                owner.EmitTypedRegister(sourceSlot ?? 0, TupleTypeBits, registerTag: sourceSlot.HasValue ? YRegisterTag : XRegisterTag);
                owner.Operand(XRegisterTag, 0);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, fields.Length * 2);
                foreach (var entry in fields)
                {
                    owner.Operand(BeamWire.UnsignedTag, entry.Field.Index + 1);
                    GuardOperand(entry.Field.Value, entry.Slot);
                }
            }

            ReleaseOperandSlots(operandBase);
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
