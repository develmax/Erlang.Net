using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private int? EmitPartitionedTryResult(IReadOnlyList<Clause> clauses, int done)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || owner.receiveScratchRegisters is not null
                || clauses.Count < 2)
            {
                return null;
            }

            var atoms = new List<(Atom Key, Expr Body)>();
            var seen = new HashSet<Atom>();
            (Atom Tag, int Arity)? tuple = null;
            foreach (Clause clause in clauses)
            {
                if (clause.Guard is not null)
                {
                    return null;
                }

                if (clause.Patterns is [Pattern.Literal { Value: Atom key }] && seen.Add(key))
                {
                    atoms.Add((key, clause.Body));
                }
                else if (tuple is null && clause.Patterns is [AliasPattern alias] && clause.Body is Expr.Variable returned
                    && ReadIdentityTuple(alias.Left, alias.Right, returned.Name)
                        is { } leftShape)
                {
                    tuple = leftShape;
                }
                else if (tuple is null && clause.Patterns is [AliasPattern reversed] && clause.Body is Expr.Variable value
                    && ReadIdentityTuple(reversed.Right, reversed.Left, value.Name)
                        is { } rightShape)
                {
                    tuple = rightShape;
                }
                else
                {
                    return null;
                }
            }

            if (tuple is not { } shape || atoms.Count == 0)
            {
                return null;
            }

            int atomDispatch = Label();
            int failure = Label();
            owner.Op(IsTuple, LabelTag, atomDispatch);
            owner.Operand(XRegisterTag, 0);
            owner.Op(IsTaggedTuple, LabelTag, failure);
            owner.EmitTypedRegister(0, TupleTypeBits);
            owner.Operand(BeamWire.UnsignedTag, shape.Arity);
            owner.LiteralOperand(shape.Tag);
            JumpTo(done);

            Mark(atomDispatch);
            var scope = variables;
            if (atoms is [var atom])
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(XRegisterTag, 0);
                owner.LiteralOperand(atom.Key);
                EmitAtomArm(atom.Key, atom.Body);
            }
            else
            {
                var keys = atoms.OrderBy(arm => arm.Key, Comparer<Atom>.Create(
                    (left, right) => TermOrder.Compare(left, right, exact: true))).ToArray();
                int[] labels = keys.Select(_ => Label()).ToArray();
                owner.Op(SelectValue, XRegisterTag, 0);
                owner.Operand(LabelTag, failure);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, keys.Length * 2);
                for (int index = 0; index < keys.Length; index++)
                {
                    owner.LiteralOperand(keys[index].Key);
                    owner.Operand(LabelTag, labels[index]);
                }

                for (int index = keys.Length - 1; index >= 0; index--)
                {
                    Mark(labels[index]);
                    EmitAtomArm(keys[index].Key, keys[index].Body);
                }
            }

            variables = scope;

            return failure;

            void EmitAtomArm(Atom key, Expr body)
            {
                variables = new(scope, StringComparer.Ordinal);
                if (body is not Expr.Literal { Value: Atom returned } || !returned.Equals(key))
                {
                    Expression(body);
                }

                JumpTo(done);
            }

            (Atom Tag, int Arity)? ReadIdentityTuple(Pattern shape, Pattern ownerBinding, string returned)
            {
                if (ownerBinding is not Pattern.Variable alias || alias.Name != returned
                    || alias.Name == VariableScopeNames.Wildcard || variables.ContainsKey(alias.Name)
                    || shape is not Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, ..] } tagged)
                {
                    return null;
                }

                var names = new HashSet<string>(StringComparer.Ordinal) { alias.Name };
                foreach (Pattern field in tagged.Items.Skip(1))
                {
                    if (field is not Pattern.Variable binding || binding.Name != VariableScopeNames.Wildcard
                        && (variables.ContainsKey(binding.Name) || !names.Add(binding.Name)))
                    {
                        return null;
                    }
                }

                return (tag, tagged.Items.Count);
            }
        }
    }
}
