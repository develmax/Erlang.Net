using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static bool ContainsComputedMap(Expr expression) =>
        expression is Expr.MapComprehension
        || expression is Expr.Map map && !AttributeLiteral.TryNormalizeConstant(map, out _)
        || expression is not Expr.Fun { SelfName: not null }
            && LocalFunctionEntries.Children(expression).Any(ContainsComputedMap);

    private static readonly Comparer<Term> MapExpressionKeyOrder =
        Comparer<Term>.Create((left, right) => TermOrder.Compare(left, right, exact: true));

    private sealed partial class GeneralEmitter
    {
        private void EmitMap(Expr.Map map, int? guardFailure)
        {
            int operandBase = nextSlot;
            int? baseSlot = map.Base is null ? null : GuardSlot(map.Base, guardFailure);
            var fields = new List<(MapField Field, Term? Key, int? KeySlot, int? ValueSlot)>();
            foreach (MapField field in map.Fields)
            {
                int? keySlot = GuardSlot(field.Key, guardFailure);
                int? valueSlot = GuardSlot(field.Value, guardFailure);
                AttributeLiteral.TryNormalizeConstant(field.Key, out Term key);
                fields.Add((field, keySlot is null ? key : null, keySlot, valueSlot));
            }

            int? invalidMap = null;
            if (map.Base is { } source)
            {
                if (baseSlot is int register)
                {
                    Load(register);
                }
                else
                {
                    Expression(source, guardFailure);
                }

                invalidMap = guardFailure ?? Label();
                EmitMapTypeTest(0, invalidMap.Value);
            }

            int index = 0;
            while (index < fields.Count)
            {
                var first = fields[index];
                var batch = new List<(MapField Field, Term? Key, int? KeySlot, int? ValueSlot)> { first };
                int end = index + 1;
                if (first.Key is not null)
                {
                    while (end < fields.Count && fields[end].Key is not null && fields[end].Field.Exact == first.Field.Exact)
                    {
                        batch.Add(fields[end++]);
                    }
                    batch = batch.GroupBy(field => field.Key!).Select(group => group.Last())
                        .OrderBy(field => field.Key!, MapExpressionKeyOrder).ToList();
                }

                bool emptySource = map.Base is null && index == 0;
                EmitMapInstruction(first.Field.Exact, guardFailure ?? 0, emptySource ? null : 0, emptySource ? 0 : 1, batch.Count);
                foreach (var field in batch)
                {
                    GuardOperand(field.Field.Key, field.KeySlot);
                    GuardOperand(field.Field.Value, field.ValueSlot);
                }
                index = end;
            }

            if (invalidMap is int invalid && guardFailure is null)
            {
                int done = Label();
                JumpTo(done);
                Mark(invalid);
                EmitBadMapTuple(0);
                Call(ErlangModuleNames.Module, ErlangModuleNames.Error, 1);
                Mark(done);
            }
            ReleaseOperandSlots(operandBase);
        }

        private bool TryEmitDirectMap(Clause clause)
        {
            if (clause.Body is not Expr.Map map || !HasUnconditionalParameters(clause)
                || AttributeLiteral.TryNormalizeConstant(map, out _)
                || map.Base is not (null or Expr.Variable)
                || map.Fields.Select(field => field.Exact).Distinct().Skip(1).Any())
            {
                return false;
            }

            var fields = new SortedDictionary<Term, Expr>(MapExpressionKeyOrder);
            foreach (MapField field in map.Fields)
            {
                if (!AttributeLiteral.TryNormalizeConstant(field.Key, out Term key) || !IsCallOperand(field.Value))
                {
                    return false;
                }
                fields[key] = field.Value;
            }

            int? source = map.Base is Expr.Variable variable ? ArgumentRegister(clause, variable.Name) : null;
            int? failure = null;
            if (source is int register)
            {
                failure = Label();
                EmitMapTypeTest(register, failure.Value);
            }

            if (fields.Count != 0)
            {
                int live = Math.Max(source ?? -1, fields.Values.OfType<Expr.Variable>()
                    .Select(value => ArgumentRegister(clause, value.Name)).DefaultIfEmpty(-1).Max()) + 1;
                EmitMapInstruction(map.Fields[0].Exact, 0, source, live, fields.Count);
                foreach (var (key, value) in fields)
                {
                    owner.LiteralOperand(key);
                    EmitDirectOperand(clause, value);
                }
            }
            else if (source is int forwarded && forwarded != 0)
            {
                owner.MoveRegister(XRegisterTag, forwarded, XRegisterTag, 0);
            }

            owner.instructions.WriteByte(Return);
            if (failure is int invalid)
            {
                Mark(invalid);
                EmitBadMapTuple(source!.Value);
                owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangModuleNames.Error, 1));
            }

            return true;
        }

        private void EmitMapTypeTest(int register, int failure)
        {
            owner.Op(IsMap, LabelTag, failure);
            owner.Operand(XRegisterTag, register);
        }

        private void EmitMapInstruction(
            bool exact,
            int failure,
            int? source,
            int live,
            int count,
            int destination = 0
        )
        {
            owner.Op(exact ? PutMapExact : PutMapAssoc, LabelTag, exact ? failure : 0);
            if (source is int register)
            {
                owner.Operand(XRegisterTag, register);
            }
            else
            {
                owner.LiteralOperand(new MapTerm([]));
            }
            owner.Operand(XRegisterTag, destination);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, count * 2);
        }

        private void EmitBadMapTuple(int register)
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, register + 1);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(ErlangErrorReasons.BadMap)), (XRegisterTag, register));
        }
    }
}
