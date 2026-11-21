using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitCheckedRecordConstruction(Clause clause)
        {
            if (!owner.SsaTypeInferenceEnabled || clause.Guard is not null
                || clause.Patterns is not [global::Erlang.Pattern.Variable input]
                || input.Name == VariableScopeNames.Wildcard
                || clause.Body is not (Expr.Tuple or Expr.List))
            {
                return false;
            }

            Expr.RecordAccess? first = null;
            var fields = new SortedDictionary<int, string>();
            if (Read(clause.Body) is not { } result || first is null || fields.Count < 2
                || fields.Count >= FirstReservedXRegister)
            {
                return false;
            }

            // A single successful tagged-tuple test proves every checked field access.
            // Allocate before projecting, while the record is the only live X register.
            var inputs = new Dictionary<string, int>(StringComparer.Ordinal);
            int register = 1;
            foreach (var field in fields)
            {
                inputs.Add(field.Value, register == fields.Count ? 0 : register);
                register++;
            }

            if (ReadCallArgumentConstructions(
                [result],
                -1,
                new Dictionary<string, int>(StringComparer.Ordinal),
                inputs: inputs
            ) is not { Constants.Count: 0, Loads.Count: 0, Moves.Count: 0 } plan)
            {
                return false;
            }

            int failure = Label();
            owner.EmitRecordTest(
                failure,
                XRegisterTag,
                0,
                Term.A(first.Name),
                first.Arity
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, plan.Constructions.Sum(item => item.HeapWords));
            owner.Operand(BeamWire.UnsignedTag, 1);
            foreach (var field in fields)
            {
                owner.EmitTuplePart(0, field.Key, inputs[field.Value]);
            }

            EmitCallArgumentConstructions(
                plan,
                -1,
                heapChecked: true
            );
            owner.instructions.WriteByte(Return);

            Mark(failure);
            if (first.SourceLine > 0)
            {
                BeamSourceLocation previous = owner.lastEmittedLine;
                var at = new BeamSourceLocation(first.PreprocessorFile ?? location.File, first.SourceLine);
                if (at != previous)
                {
                    owner.EmitLine(at);
                    owner.lastEmittedLine = previous;
                }
            }

            owner.Op(BadRecord, XRegisterTag, 0);

            return true;

            Expr? Read(Expr expression)
            {
                if (AttributeLiteral.TryNormalizeConstant(expression, out Term value))
                {
                    return new Expr.Literal(value);
                }

                switch (expression)
                {
                    case Expr.RecordAccess { Value: Expr.Variable record } access
                        when record.Name == input.Name
                            && (first is null || (access.Name == first.Name && access.Arity == first.Arity)):
                        first ??= access;
                        if (!fields.TryGetValue(access.Index, out string? name))
                        {
                            // The planner uses a name unavailable to source variables for each distinct field.
                            name = FormattableString.Invariant($"$record:{access.Index}");
                            fields.Add(access.Index, name);
                        }

                        return new Expr.Variable(name);
                    case Expr.Tuple tuple when ReadItems(tuple.Items) is { } items:
                        return tuple with { Items = items };
                    case Expr.List list when ReadItems(list.Items) is { } heads:
                        if (list.Tail is null)
                        {
                            return list with { Items = heads };
                        }

                        return Read(list.Tail) is { } tail ? list with { Items = heads, Tail = tail } : null;
                    default:
                        return null;
                }
            }

            Expr[]? ReadItems(IReadOnlyList<Expr> expressions)
            {
                var items = new Expr[expressions.Count];
                for (int index = 0; index < items.Length; index++)
                {
                    if (Read(expressions[index]) is not { } item)
                    {
                        return null;
                    }

                    items[index] = item;
                }

                return items;
            }
        }
    }
}
