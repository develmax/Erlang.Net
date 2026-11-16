using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitLiteralTupleClosures(FunctionDefinition function, int functionInfo)
        {
            if (function.Arity != 1 || function.Clauses.Count < 2
                || function.Clauses[0] is not { Guard: null, Patterns: [Erlang.Pattern.Tuple first], Body: Expr.Fun }
                || first.Items.Count < 2 || first.Items.Any(item => item is not Erlang.Pattern.Literal))
            {
                return false;
            }

            Clause? fallback = function.Clauses[^1] is { Guard: null, Patterns: [Erlang.Pattern.Variable] } last
                ? last : null;
            Expr.Fun? fallbackClosure = fallback?.Body as Expr.Fun;
            Term? fallbackValue = null;
            if (fallbackClosure is not null)
            {
                var binding = (Erlang.Pattern.Variable)fallback!.Patterns[0];
                if (binding.Name != VariableScopeNames.Wildcard
                    && ClosureCaptures.Capture(fallbackClosure, new Dictionary<string, Term>
                    {
                        [binding.Name] = Nil.Value
                    }).Count != 0)
                {
                    return false;
                }
            }
            else if (fallback is not null && !AttributeLiteral.TryNormalizeConstant(fallback.Body, out fallbackValue))
            {
                return false;
            }

            int selector = -1;
            var branches = new List<(Erlang.Pattern.Tuple Tuple, Expr.Fun Closure)>();
            var prefix = first.Items.Cast<Erlang.Pattern.Literal>().Select(item => item.Value).ToArray();
            foreach (Clause clause in function.Clauses.Take(function.Clauses.Count - (fallback is null ? 0 : 1)))
            {
                if (clause is not { Guard: null, Patterns: [Erlang.Pattern.Tuple tuple], Body: Expr.Fun closure }
                    || tuple.Items.Count != prefix.Length)
                {
                    return false;
                }

                for (int field = 0; field < tuple.Items.Count; field++)
                {
                    if (tuple.Items[field] is not Erlang.Pattern.Literal literal)
                    {
                        return false;
                    }

                    if (prefix[field].Equals(literal.Value))
                    {
                        continue;
                    }

                    if (selector >= 0 && field != selector
                        || prefix[field] is not (Integer or Atom)
                        || (prefix[field] is Integer) != (literal.Value is Integer)
                        || literal.Value is not (Integer or Atom))
                    {
                        return false;
                    }

                    selector = field;
                }

                branches.Add((tuple, closure));
            }

            if (selector < 0)
            {
                return false;
            }

            var keys = new SortedDictionary<Term, Expr.Fun>(Comparer<Term>.Create(
                (left, right) => TermOrder.Compare(left, right, exact: true)
            ));
            foreach (var (tuple, closure) in branches)
            {
                if (!keys.TryAdd(((Erlang.Pattern.Literal)tuple.Items[selector]).Value, closure))
                {
                    return false;
                }
            }

            var labels = new Dictionary<Term, int>();
            foreach (var key in keys.Keys.Reverse())
            {
                labels.Add(key, Label());
            }

            int failure = fallback is null ? functionInfo : Label();
            bool tagged = selector != 0 && prefix[0] is Atom;
            if (tagged)
            {
                EmitDetailRecordTest(
                    0,
                    prefix.Length,
                    (Atom)prefix[0],
                    failure
                );
            }
            else
            {
                EmitTupleShapeTest(
                    XRegisterTag,
                    0,
                    prefix.Length,
                    failure
                );
            }

            for (int field = tagged ? 1 : 0; field < prefix.Length; field++)
            {
                if (field == selector)
                {
                    continue;
                }

                owner.EmitTuplePart(0, field, 1);
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, 1);
                owner.LiteralOperand(prefix[field]);
            }

            int destination = fallback is null ? 1 : 0;
            owner.EmitTuplePart(0, selector, destination);
            EmitAritySelect(destination, keys.Count, failure);
            foreach (var key in keys.Keys)
            {
                owner.LiteralOperand(key);
                owner.Operand(LabelTag, labels[key]);
            }

            variables = new(StringComparer.Ordinal);
            foreach (var (key, closure) in keys.Reverse())
            {
                Mark(labels[key]);
                EmitGeneralClosure(closure);
                owner.instructions.WriteByte(Return);
            }

            if (fallback is not null)
            {
                Mark(failure);
                if (fallbackClosure is not null)
                {
                    EmitGeneralClosure(fallbackClosure);
                }
                else
                {
                    owner.EmitConstant(fallbackValue!, 0);
                }

                owner.instructions.WriteByte(Return);
            }

            return true;
        }
    }
}
