using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGroupedRecordFieldGuardReturn(Clause clause, int input, int failure)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || clause.Guard is not Expr.Binary
                {
                    Operator: ErlangOperators.NumericEqual or ErlangOperators.ExactEqual
                } equality || clause.Body is not Expr.Tuple result)
            {
                return false;
            }

            Expr field = equality.Left;
            Expr record = equality.Right;
            if (field is Expr.RecordAccess)
            {
                (field, record) = (record, field);
            }

            if (field is not Expr.Variable key || record is not Expr.RecordAccess
                {
                    Value: Expr.Variable tuple,
                    Arity: var width,
                    Index: var member
                } access || member <= 0 || member >= width
                || ReadFlatTaggedEntryArm(clause with { Guard = null }, input, key.Name) is not { } arm
                || !arm.Arguments.TryGetValue(tuple.Name, out int recordRegister)
                || arm.Fields.FirstOrDefault(candidate => candidate.Binding == key.Name) is not { } keyField
                || arm.Fields.Any(candidate => candidate.Literal is not null))
            {
                return false;
            }

            var bindings = arm.Arguments.ToDictionary(
                binding => binding.Key,
                binding => (binding.Value, (int?)null),
                StringComparer.Ordinal
            );
            foreach (TaggedEntryField projected in arm.Fields)
            {
                bindings.Add(projected.Binding!, (input, projected.Index));
            }

            if (ReadFiniteEffectArguments(result.Items, bindings) is not { } values)
            {
                return false;
            }

            var names = values.Select(value => value.Binding).OfType<string>().Distinct(StringComparer.Ordinal).ToArray();
            int scratch = clause.Patterns.Count;
            int live = names.Select(name => bindings[name].Item1).DefaultIfEmpty(-1).Max() + 1;
            int[] fields = names.Select(name => bindings[name].Item2).OfType<int>().Order().ToArray();
            bool keepMatched = fields.Contains(keyField.Index);
            if (scratch + Math.Max(1, fields.Length) >= FirstReservedXRegister)
            {
                return false;
            }

            // Both input terms stay in their entry registers until the guard
            // accepts, so all tests can reject to the ordinary clause fallback.
            owner.EmitTuplePart(input, 0, scratch);
            owner.EmitAtomTest(scratch, arm.Tag.Name, failure);
            owner.Op(IsTaggedTuple, LabelTag, failure);
            owner.Operand(XRegisterTag, recordRegister);
            owner.Operand(BeamWire.UnsignedTag, width);
            owner.Operand(AtomTag, owner.Intern(access.Name));
            owner.EmitTuplePart(input, keyField.Index, scratch);
            owner.EmitTuplePart(recordRegister, member, scratch + 1);
            owner.Op(equality.Operator == ErlangOperators.NumericEqual ? NumericEqualTest : ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, scratch);
            owner.Operand(XRegisterTag, scratch + 1);

            int required = keepMatched ? scratch + 1 : live;
            owner.Op(TestHeap, BeamWire.UnsignedTag, result.Items.Count + 1);
            owner.Operand(BeamWire.UnsignedTag, required);
            var projectedRegisters = new Dictionary<int, int>();
            int next = scratch;
            if (keepMatched)
            {
                projectedRegisters.Add(keyField.Index, next++);
            }

            foreach (int index in fields.Where(index => index != keyField.Index))
            {
                int destination = index == fields[^1] ? input : next++;
                owner.EmitTuplePart(input, index, destination);
                projectedRegisters.Add(index, destination);
            }

            owner.EmitTupleHeader(XRegisterTag, result.Items.Count);
            foreach ((string? name, Term? literal) in values)
            {
                if (name is null)
                {
                    owner.LiteralOperand(literal!);
                    continue;
                }

                var (register, position) = bindings[name];
                owner.Operand(XRegisterTag, position is int index ? projectedRegisters[index] : register);
            }

            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}
