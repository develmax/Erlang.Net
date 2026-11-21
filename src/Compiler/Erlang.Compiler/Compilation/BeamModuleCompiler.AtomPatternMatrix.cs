using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record AtomMatrixResult(Atom? Constant, int Register);

        private sealed record AtomMatrixRow(Atom? Key, AtomMatrixResult Result);

        private bool TryEmitAtomPatternMatrix(FunctionDefinition function)
        {
            if (function.Arity != 2 || function.Clauses is not [_, _, .., { Guard: null } fallback]
                || fallback.Patterns is not [Pattern.Variable firstDefault, Pattern.Variable secondDefault]
                || firstDefault.Name == secondDefault.Name && firstDefault.Name != VariableScopeNames.Wildcard
                || fallback.Body is not Expr.Literal { Value: Atom defaultAtom })
            {
                return false;
            }

            var groups = new Dictionary<Atom, List<AtomMatrixRow>>();
            foreach (Clause clause in function.Clauses.SkipLast(1))
            {
                if (clause is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }, var value] })
                {
                    return false;
                }

                if (!groups.TryGetValue(key, out var rows))
                {
                    groups.Add(key, rows = []);
                }

                if (value is Pattern.Literal { Value: Atom secondKey }
                    && clause.Body is Expr.Literal { Value: Atom constant }
                    && rows.All(row => row.Key is not null && !row.Key.Equals(secondKey)))
                {
                    rows.Add(new AtomMatrixRow(secondKey, new AtomMatrixResult(constant, -1)));
                    continue;
                }

                if (rows.Count != 0 || value is not Pattern.Variable parameter
                    || parameter.Name == VariableScopeNames.Wildcard
                    || clause.Body is not Expr.Variable returned || returned.Name != parameter.Name)
                {
                    return false;
                }

                rows.Add(new AtomMatrixRow(null, new AtomMatrixResult(null, 1)));
            }

            if (groups.Count < 2 || groups.Values.All(rows => rows.Count == 1))
            {
                return false;
            }

            var atomOrder = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
            var arms = groups.OrderBy(entry => entry.Key, atomOrder).ToArray();
            var defaultResult = new AtomMatrixResult(defaultAtom, -1);
            foreach (var (_, rows) in arms)
            {
                if (rows.Count > 1)
                {
                    rows.Sort((left, right) => atomOrder.Compare(left.Key!, right.Key!));
                }
            }

            // beam_core_to_ssa groups the first pattern column before matching the
            // second. beam_ssa_share unifies identical returns; codegen visits the
            // default and ascending switch edges before reversing postorder.
            var defaultLabel = Label();
            var destinations = new Dictionary<AtomMatrixResult, int> { [defaultResult] = defaultLabel };
            var blocks = new List<(Atom? Key, List<AtomMatrixRow>? Rows, AtomMatrixResult? Result)>();
            var recorded = new HashSet<AtomMatrixResult> { defaultResult };
            blocks.Add((null, null, defaultResult));
            foreach (var (key, rows) in arms)
            {
                foreach (AtomMatrixRow row in rows)
                {
                    if (recorded.Add(row.Result))
                    {
                        destinations.Add(row.Result, Label());
                        blocks.Add((null, null, row.Result));
                    }
                }

                if (rows[0].Key is not null)
                {
                    blocks.Add((key, rows, null));
                }
            }

            var labels = arms.Where(group => group.Value[0].Key is not null).ToDictionary(group => group.Key, _ => Label());
            EmitAtomMatrixSelection(
                0,
                defaultLabel,
                arms.Select(group =>
                (group.Key, group.Value[0].Key is null ? destinations[group.Value[0].Result] : labels[group.Key]))
            );

            for (int index = blocks.Count - 1; index >= 0; index--)
            {
                var block = blocks[index];
                if (block.Result is { } result)
                {
                    Mark(destinations[result]);
                    if (result.Constant is { } value)
                    {
                        owner.EmitConstant(value, 0);
                    }
                    else
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            result.Register,
                            XRegisterTag,
                            0
                        );
                    }

                    owner.instructions.WriteByte(Return);
                    continue;
                }

                Mark(labels[block.Key!]);
                List<AtomMatrixRow> rows = block.Rows!;
                if (rows.Count > 1)
                {
                    EmitAtomMatrixSelection(1, defaultLabel, rows.Select(row => (row.Key!, destinations[row.Result])));
                    continue;
                }

                owner.Op(ExactEqualTest, LabelTag, defaultLabel);
                owner.Operand(XRegisterTag, 1);
                owner.Operand(AtomTag, owner.Intern(rows[0].Key!.Name));
                owner.Op(Jump, LabelTag, destinations[rows[0].Result]);
            }

            return true;
        }

        private void EmitAtomMatrixSelection(int register, int failure, IEnumerable<(Atom Key, int Target)> arms)
        {
            var ordered = arms.ToArray();
            owner.Op(SelectValue, XRegisterTag, register);
            owner.Operand(LabelTag, failure);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, ordered.Length * 2);
            foreach (var (key, target) in ordered)
            {
                owner.Operand(AtomTag, owner.Intern(key.Name));
                owner.Operand(LabelTag, target);
            }
        }
    }
}
