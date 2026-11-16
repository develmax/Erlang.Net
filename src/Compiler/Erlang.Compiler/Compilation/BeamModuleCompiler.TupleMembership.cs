using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed record TupleMembershipGroup(Atom Key, (Atom Key, Integer[] Values)[] Members);

    private bool TryEmitTupleMembership(FunctionDefinition function)
    {
        if (function.Arity != 1 || function.Clauses.Count < 2)
        {
            return false;
        }

        var keys = new HashSet<(Atom First, Atom Second, Integer Value)>();
        Term? result = null;
        foreach (Clause clause in function.Clauses)
        {
            if (clause is not
                {
                    Guard: null,
                    Patterns: [Pattern.Tuple { Items: [
                        Pattern.Literal { Value: Atom first }, Pattern.Literal { Value: Atom second },
                        Pattern.Literal { Value: Integer value }
                    ] }],
                    Body: Expr.Literal literal
                }
                || result is not null && !result.Equals(literal.Value)
                || !keys.Add((first, second, value)))
            {
                return false;
            }

            result = literal.Value;
        }

        var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
        var groups = keys.GroupBy(key => key.First).OrderByDescending(group => group.Key, order)
            .Select(group => new TupleMembershipGroup(group.Key, group.GroupBy(key => key.Second)
                .OrderBy(member => member.Key, order)
                .Select(member => (member.Key, member.Select(key => key.Value).OrderBy(value => value.Value).ToArray()))
                .ToArray())).ToArray();
        if (groups.Length < 2 || groups.Any(group => group.Members.Length < 2))
        {
            return false;
        }

        // Each integer test can be shared by all pairs with the same set of final values.
        static string Key(Integer[] values) => string.Join(",", values.Select(value => value.Value.ToString(
            System.Globalization.CultureInfo.InvariantCulture)));
        var sets = groups.SelectMany((group, index) => group.Members.Select(member => (Index: index, member.Values)))
            .GroupBy(member => Key(member.Values)).ToDictionary(group => group.Key,
                group => (LastGroup: group.Max(member => member.Index), group.First().Values), StringComparer.Ordinal);
        var groupLabels = new Dictionary<Atom, int>();
        var valueLabels = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int index = 0; index < groups.Length; index++)
        {
            var group = groups[index];
            groupLabels.Add(group.Key, nextLabel++);
            foreach (var member in group.Members.DistinctBy(member => Key(member.Values)).Reverse())
            {
                string key = Key(member.Values);
                if (member.Values.Length > 1 && sets[key].LastGroup == index && !valueLabels.ContainsKey(key))
                {
                    valueLabels.Add(key, nextLabel++);
                }
            }
        }

        var singles = sets.Where(set => set.Value.Values.Length == 1)
            .OrderByDescending(set => set.Value.Values[0].Value).ToArray();
        foreach (var single in singles)
        {
            valueLabels.Add(single.Key, nextLabel++);
        }

        int success = nextLabel++;
        int failure = groupLabels[groups[0].Key] - 2;
        EmitTupleCheck(0, 3, failure);
        for (int index = 0; index < 3; index++)
        {
            EmitTuplePart(0, index, index + 1);
        }
        EmitMembershipSelect(1, failure, groups.Reverse().Select(group => ((Term)group.Key, groupLabels[group.Key])));

        for (int index = 0; index < groups.Length; index++)
        {
            var group = groups[index];
            Op(Label, BeamWire.UnsignedTag, groupLabels[group.Key]);
            EmitMembershipSelect(2, failure, group.Members.Select(member => ((Term)member.Key, valueLabels[Key(member.Values)])));
            foreach (var member in group.Members.DistinctBy(member => Key(member.Values)).Reverse())
            {
                string key = Key(member.Values);
                if (member.Values.Length > 1 && sets[key].LastGroup == index)
                {
                    Op(Label, BeamWire.UnsignedTag, valueLabels[key]);
                    EmitMembershipSelect(3, failure, member.Values.Select(value => ((Term)value, success)));
                }
            }
        }

        foreach (var single in singles)
        {
            int label = valueLabels[single.Key];
            Op(Label, BeamWire.UnsignedTag, label);
            Op(ExactEqualTest, LabelTag, failure);
            Operand(XRegisterTag, 3);
            LiteralOperand(single.Value.Values[0]);
            if (label != success - 1)
            {
                Op(Jump, LabelTag, success);
            }
        }

        Op(Label, BeamWire.UnsignedTag, success);
        EmitConstant(result!, 0);
        instructions.WriteByte(Return);

        return true;
    }

    private void EmitMembershipSelect(
        int register,
        int failure,
        IEnumerable<(Term Key, int Label)> entries,
        int selectorType = 0,
        (long Lower, long Upper)? bounds = null
    )
    {
        var table = entries.ToArray();
        instructions.WriteByte(SelectValue);
        if (selectorType == 0)
        {
            Operand(XRegisterTag, register);
        }
        else
        {
            EmitTypedRegister(register, selectorType, bounds?.Lower, bounds?.Upper);
        }
        Operand(LabelTag, failure);
        instructions.WriteByte(ExtendedListOperand);
        Operand(BeamWire.UnsignedTag, table.Length * 2);
        foreach (var (key, label) in table)
        {
            LiteralOperand(key);
            Operand(LabelTag, label);
        }
    }
}
