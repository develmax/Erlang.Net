using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSharedTaggedArityPredicate(FunctionDefinition function)
        {
            IReadOnlyList<Clause> clauses = function.Clauses;
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || function.Arity != 1 || clauses.Count < 3
                || clauses[^1] is not
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.Variable],
                    Body: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.False } }
                })
            {
                return false;
            }

            Atom? shared = null;
            var arities = new HashSet<int>();
            foreach (Clause candidate in clauses.SkipLast(1))
            {
                if (candidate is not
                    {
                        Guard: null,
                        Patterns: [Erlang.Pattern.Tuple { Items: [Erlang.Pattern.Literal { Value: Atom tag }, ..] } tuple],
                        Body: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } }
                    }
                    || tuple.Items.Count < 2 || shared is not null && !shared.Equals(tag) || !arities.Add(tuple.Items.Count)
                    || tuple.Items.Skip(1).Any(item => item is not Erlang.Pattern.Variable)
                    || !HasUnconditionalParameters(candidate with { Patterns = tuple.Items.Skip(1).ToArray() }))
                {
                    return false;
                }

                shared = tag;
            }

            int compare = Label();
            int rejected = Label();
            owner.EmitTupleArityDispatch(rejected, arities.Order().Select(arity => (arity, compare)).ToArray());
            Mark(compare);
            owner.EmitTuplePart(0, 0, 0);
            owner.Op(BifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.ExactEqual, 2));
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(shared!);
            owner.Operand(XRegisterTag, 0);
            owner.instructions.WriteByte(Return);

            Mark(rejected);
            owner.EmitConstant(Term.A(ErlangBooleanAtoms.False), 0);
            owner.instructions.WriteByte(Return);

            return true;
        }
    }
}
