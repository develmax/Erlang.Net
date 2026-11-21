using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private Dictionary<int, int> EmitSharedNestedTupleProjections(
            IEnumerable<NestedTupleLeaf> leaves,
            int tupleRegister,
            int tagRegister
        )
        {
            int[] fields = leaves.SelectMany(leaf => leaf.Projections)
                .Where(projection => projection.Source == tupleRegister)
                .Select(projection => projection.Field).Distinct().Order().ToArray();
            var registers = new Dictionary<int, int>();
            owner.EmitTuplePart(tupleRegister, 0, tagRegister);
            for (int index = 0; index < fields.Length; index++)
            {
                // The tag path dispatches all subsequent reads through these field values.
                int register = index == fields.Length - 1 ? tupleRegister : tagRegister + index + 1;
                owner.EmitTuplePart(tupleRegister, fields[index], register);
                registers.Add(fields[index], register);
            }

            return registers;
        }

        private static NestedTupleLeaf PlaceSharedNestedTupleInputs(
            NestedTupleLeaf leaf,
            int outerRegister,
            IReadOnlyDictionary<int, int> outer,
            IReadOnlyDictionary<int, int> inner
        )
        {
            var assigned = new Dictionary<int, int>();
            var deferred = new List<NestedTupleProjection>();
            int nextRegister = outer.Values.DefaultIfEmpty(-1).Max() + 1;
            foreach (NestedTupleProjection projection in leaf.Projections)
            {
                var fields = projection.Source == outerRegister ? outer : inner;
                if (fields.TryGetValue(projection.Field, out int register))
                {
                    assigned.Add(projection.Destination, register);
                }
                else
                {
                    int destination = Math.Max(nextRegister, projection.Destination);
                    nextRegister = destination + 1;
                    assigned.Add(projection.Destination, destination);
                    deferred.Add(projection with { Destination = destination });
                }
            }

            return leaf with
            {
                Inputs = leaf.Inputs.ToDictionary(binding => binding.Key,
                    binding => assigned.GetValueOrDefault(binding.Value, binding.Value), StringComparer.Ordinal),
                Projections = deferred.ToArray(),
                Guard = leaf.Guard is null ? null : Remap(leaf.Guard)
            };

            ForwardGuard Remap(ForwardGuard guard) => guard switch
            {
                ForwardGuard.Test test => test with { Register = assigned.GetValueOrDefault(test.Register, test.Register) },
                ForwardGuard.Conjunction both => both with { Left = Remap(both.Left), Right = Remap(both.Right) },
                ForwardGuard.Disjunction either => either with { Left = Remap(either.Left), Right = Remap(either.Right) },
                _ => guard
            };
        }

        private bool TryEmitLateNestedProjectionReturn(NestedTupleLeaf leaf)
        {
            if (leaf is not { Projections: [var projection], Clause.Body: Expr.Tuple tuple }
                || leaf.Inputs.Values.Contains(0))
            {
                return false;
            }

            var inputs = leaf.Inputs.ToDictionary(binding => binding.Key,
                binding => binding.Value == projection.Destination ? 0 : binding.Value, StringComparer.Ordinal);
            if (ReadCallArgumentConstructions(
                [tuple],
                -1,
                new Dictionary<string, int>(),
                inputs: inputs
            ) is not { } construction)
            {
                return false;
            }

            int words = construction.Constructions.Sum(item => item.HeapWords);
            owner.Op(TestHeap, BeamWire.UnsignedTag, words);
            owner.Operand(BeamWire.UnsignedTag, Math.Max(construction.Live, projection.Source + 1));
            owner.EmitTuplePart(projection.Source, projection.Field, 0);
            EmitCallArgumentConstructions(construction, -1, heapChecked: true);
            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}
