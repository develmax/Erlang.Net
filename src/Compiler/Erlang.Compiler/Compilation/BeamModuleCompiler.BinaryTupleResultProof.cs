namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private string? ReadBinaryTupleProjection(Expr source, Pattern pattern, (int Arity, int Element) shape)
        {
            if (source is not Expr.Call { Module: null, DynamicRemote: false } call
                || pattern is not Pattern.Tuple tuple || tuple.Items.Count != shape.Arity
                || tuple.Items[shape.Element] is not Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding
                || owner.IsDeclaredNative((call.Function, call.Arguments.Count))
                || !localFunctions.TryGetValue((call.Function, call.Arguments.Count), out var provider)
                || !provider.Clauses.All(HasBinaryProjection))
            {
                return null;
            }

            return binding.Name;

            bool HasBinaryProjection(Clause clause)
            {
                var captures = new HashSet<string>(StringComparer.Ordinal);
                foreach (BitPattern binary in clause.Patterns.OfType<BitPattern>())
                {
                    AddBinaryCaptures(binary, captures);
                }

                Expr value = clause.Body;
                if (value is Expr.Sequence { Items: [.., var last] } sequence)
                {
                    foreach (var item in sequence.Items.Take(sequence.Items.Count - 1))
                    {
                        if (item is Expr.Match { Pattern: BitPattern binary })
                        {
                            AddBinaryCaptures(binary, captures);
                        }
                    }

                    value = last;
                }

                return value is Expr.Tuple result && result.Items.Count == shape.Arity
                    && result.Items[shape.Element] is Expr.Variable binaryResult && captures.Contains(binaryResult.Name);
            }

            static void AddBinaryCaptures(BitPattern pattern, HashSet<string> captures)
            {
                foreach (var segment in pattern.Segments)
                {
                    if (segment is
                        {
                            Value: Pattern.Variable { Name: not VariableScopeNames.Wildcard } binary,
                            Specification: { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit }
                        })
                    {
                        captures.Add(binary.Name);
                    }
                }
            }
        }
    }
}
