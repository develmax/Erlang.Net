using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitEntryConstructionSequence(Clause clause)
        {
            if (clause.Guard is not null || !HasUnconditionalParameters(clause))
            {
                return false;
            }

            IReadOnlyList<Expr> items = clause.Body is Expr.Sequence sequence ? sequence.Items : [clause.Body];
            if (items.Count == 0)
            {
                return false;
            }

            bool preserveOrder = items[^1] is Expr.Map
                || items[^1] is Expr.Call tail && tail.Arguments.Any(argument => argument is Expr.Map)
                || items.Any(item => item is Expr.Match { Value: Expr.Map });
            if (!preserveOrder && (items.Count < 2 || items[^1] is Expr.Call))
            {
                return false;
            }

            var inputs = clause.Patterns.Select((pattern, register) => (((Pattern.Variable)pattern).Name, register))
                .Where(input => input.Name != VariableScopeNames.Wildcard)
                .ToDictionary(input => input.Name, input => input.register, StringComparer.Ordinal);
            var bindings = new Dictionary<string, Expr>(StringComparer.Ordinal);
            var consumed = new HashSet<string>(StringComparer.Ordinal);
            var scheduled = new List<(string Name, Expr Value)>();
            foreach (Expr item in items.SkipLast(1))
            {
                if (item is not Expr.Match { Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding, Value: var source }
                    || inputs.ContainsKey(binding.Name) || ReadValue(source) is not { } value
                    || !bindings.TryAdd(binding.Name, value))
                {
                    return false;
                }

                if (preserveOrder && value is Expr.Tuple or Expr.List or Expr.Map)
                {
                    scheduled.Add((binding.Name, value));
                    bindings[binding.Name] = new Expr.Variable(binding.Name);
                }
            }

            Expr.Call? target = null;
            IReadOnlyList<Expr> arguments;
            if (items[^1] is Expr.Call call)
            {
                if (ReadItems(call.Arguments) is not { } values
                    || !values.Any(value => value is Expr.Map) && !scheduled.Any(binding => binding.Value is Expr.Map))
                {
                    return false;
                }

                var resolved = owner.ResolveImportedCall(call, entries);
                if (!TryResolveOrdinaryCall(
                    resolved with { Arguments = values.Select(_ => (Expr)new Expr.Literal(Nil.Value)).ToArray() },
                    out string? module))
                {
                    return false;
                }

                target = resolved with { Module = module };
                arguments = values;
            }
            else if (ReadValue(items[^1]) is { } result && (result is Expr.Tuple or Expr.List or Expr.Map
                || result is Expr.Variable && scheduled.Any(binding => binding.Value is Expr.Map)))
            {
                arguments = [result];
            }
            else
            {
                return false;
            }

            if (ReadCallArgumentConstructions(
                arguments,
                -1,
                new Dictionary<string, int>(),
                inputs: inputs,
                bindings: preserveOrder ? scheduled : null,
                allowMap: preserveOrder
            ) is not { } plan)
            {
                return false;
            }

            EmitCallArgumentConstructions(plan, -1);
            if (target is not null)
            {
                EmitRegisterTailTarget(target, arguments.Count);
            }
            else
            {
                owner.instructions.WriteByte(Return);
            }

            return true;

            Expr? ReadValue(Expr expression)
            {
                if (expression is Expr.Variable variable)
                {
                    if (inputs.ContainsKey(variable.Name))
                    {
                        return expression;
                    }

                    return bindings.TryGetValue(variable.Name, out var value)
                        && (value is not (Expr.Tuple or Expr.List) || consumed.Add(variable.Name))
                        ? value : null;
                }

                if (AttributeLiteral.TryNormalizeConstant(expression, out Term constant))
                {
                    return new Expr.Literal(constant);
                }

                switch (expression)
                {
                    case Expr.Tuple tuple when ReadItems(tuple.Items) is { } fields:
                        expression = tuple with { Items = fields };
                        break;
                    case Expr.List list when ReadItems(list.Items) is { } items:
                        Expr? tail = list.Tail is null ? null : ReadValue(list.Tail);
                        if (list.Tail is not null && tail is null)
                        {
                            return null;
                        }

                        expression = list with { Items = items, Tail = tail };
                        break;
                    case Expr.Map { Base: null } map when preserveOrder:
                        var mapFields = new MapField[map.Fields.Count];
                        for (int index = 0; index < mapFields.Length; index++)
                        {
                            MapField field = map.Fields[index];
                            if (field.Exact || !AttributeLiteral.TryNormalizeConstant(field.Key, out Term key)
                                || ReadValue(field.Value) is not { } value)
                            {
                                return null;
                            }

                            mapFields[index] = field with { Key = new Expr.Literal(key), Value = value };
                        }

                        expression = map with { Fields = mapFields };
                        break;
                    default:
                        return null;
                }

                return AttributeLiteral.TryNormalizeConstant(expression, out constant)
                    ? new Expr.Literal(constant) : expression;
            }

            Expr[]? ReadItems(IReadOnlyList<Expr> items)
            {
                var values = new Expr[items.Count];
                for (int index = 0; index < values.Length; index++)
                {
                    if (ReadValue(items[index]) is not { } value)
                    {
                        return null;
                    }

                    values[index] = value;
                }

                return values;
            }
        }
    }
}
