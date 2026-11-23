using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record IntegerMatrixRow(Integer? Key, Atom Value);

        private bool TryEmitClosedIntegerPatternMatrix(FunctionDefinition function, int failure)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Arity != 2
                || function.Clauses.Count < 4 || owner.IsDeclaredNative((function.Name, function.Arity)))
            {
                return false;
            }

            var groups = new Dictionary<Integer, List<IntegerMatrixRow>>();
            foreach (Clause clause in function.Clauses)
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns: [Pattern.Literal { Value: Integer first }, var second],
                        Body: Expr.Literal { Value: Atom result }
                    })
                {
                    return false;
                }

                if (!groups.TryGetValue(first, out var group))
                {
                    groups.Add(first, group = []);
                }

                if (second is Pattern.Literal { Value: Integer key }
                    && group.All(row => row.Key is not null && !row.Key.Equals(key)))
                {
                    group.Add(new IntegerMatrixRow(key, result));
                }
                else if (second is Pattern.Variable && group.Count == 0)
                {
                    group.Add(new IntegerMatrixRow(null, result));
                }
                else
                {
                    return false;
                }
            }

            if (groups.Count < 3 || !groups.Values.Any(rows => rows.Count >= 3)
                || groups.Values.Any(rows => rows[0].Key is not null && rows.Count < 3))
            {
                return false;
            }

            var targets = new Dictionary<Integer, int>();
            var blocks = new List<(IntegerMatrixRow[] Rows, int Label)>();
            var outputs = new HashSet<Atom>();
            foreach (var (key, group) in groups.OrderByDescending(entry => entry.Key.Value))
            {
                var rows = group.OrderBy(row => row.Key?.Value).ToArray();
                int same = blocks.FindIndex(block => block.Rows.SequenceEqual(rows));
                if (same >= 0)
                {
                    targets.Add(key, blocks[same].Label);
                    continue;
                }

                if (rows.Any(row => !outputs.Add(row.Value)))
                {
                    return false;
                }

                // Assign labels only after the whole source matrix has been checked.
                targets.Add(key, blocks.Count);
                blocks.Add((rows, blocks.Count));
            }

            int[] labels = blocks.Select(_ => Label()).ToArray();
            owner.EmitMembershipSelect(
                0,
                failure,
                targets.OrderBy(entry => entry.Key.Value).Select(entry => ((Term)entry.Key, labels[entry.Value]))
            );
            foreach (var (rows, block) in blocks)
            {
                Mark(labels[block]);
                if (rows is [{ Key: null, Value: var result }])
                {
                    EmitResult(result);
                    continue;
                }

                // OTP emits the switch table in ascending value order and the
                // physical labels in reverse postorder.
                var branches = rows.Reverse().Select(row => (Row: row, Label: Label())).ToArray();
                owner.EmitMembershipSelect(
                    1,
                    failure,
                    branches.Reverse().Select(branch => ((Term)branch.Row.Key!, branch.Label))
                );
                foreach (var (row, label) in branches)
                {
                    Mark(label);
                    EmitResult(row.Value);
                }
            }

            return true;

            void EmitResult(Atom value)
            {
                owner.EmitConstant(value, 0);
                owner.instructions.WriteByte(Return);
            }
        }
    }
}
