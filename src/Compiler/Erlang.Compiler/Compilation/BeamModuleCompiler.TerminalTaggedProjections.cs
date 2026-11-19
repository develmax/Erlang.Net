using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TerminalTaggedProjection(
            Atom Tag,
            int Arity,
            int Field,
            int NestedArity = 0,
            int NestedField = 0
        )
        {
            internal bool ReturnsSelector => Arity == 2 && Field == 1 && NestedArity == 0;
        }

        private bool TryEmitTerminalTaggedProjections(Expr.Case decision, int frame)
        {
            var arms = new List<TerminalTaggedProjection>(decision.Clauses.Count);
            foreach (Clause branch in decision.Clauses)
            {
                if (ReadTerminalTaggedProjection(branch) is not { } arm)
                {
                    return false;
                }

                arms.Add(arm);
            }

            if (arms.Where(arm => arm.Arity > 2).ToArray() is not [{ NestedArity: 0 } wide])
            {
                return false;
            }

            var pairs = arms.Where(arm => arm.Arity == 2).ToArray();
            if (pairs.Where(arm => !arm.ReturnsSelector).ToArray() is not [{ NestedArity: > 0 } nested])
            {
                return false;
            }

            var aliases = pairs.Where(arm => arm.ReturnsSelector).ToArray();
            if (aliases.Length < 2 || aliases.Select(arm => arm.Tag).Distinct().Count() != aliases.Length
                || Array.FindIndex(pairs, arm => arm.ReturnsSelector && arm.Tag.Equals(nested.Tag)) <= Array.IndexOf(pairs, nested))
            {
                return false;
            }

            // Allocate the value branches before the common tuple return and the
            // case error so labels follow their block order after OTP cleanup.
            int wideLabel = Label();
            int pairsLabel = Label();
            int nestedLabel = Label();
            int aliasLabel = Label();
            int failure = Label();
            owner.EmitTupleArityDispatch(failure, [(2, pairsLabel), (wide.Arity, wideLabel)]);

            Mark(wideLabel);
            EmitCaseTupleTest(
                wide.Arity,
                wide.Tag,
                failure,
                arityKnown: true
            );
            EmitProjectedTuple(wide.Tag, 0, wide.Field);

            Mark(pairsLabel);
            var tags = aliases.OrderBy(arm => arm.Tag, Comparer<Atom>.Create((left, right) =>
                TermOrder.Compare(left, right, exact: true)))
                .Select(arm => (arm.Tag, arm.Tag.Equals(nested.Tag) ? nestedLabel : aliasLabel)).ToArray();
            EmitRegisterCaseTagSelection(tags, failure);

            Mark(nestedLabel);
            owner.EmitTuplePart(0, nested.Field, 1);
            EmitTupleShapeTest(
                XRegisterTag,
                1,
                nested.NestedArity,
                aliasLabel
            );
            EmitProjectedTuple(nested.Tag, 1, nested.NestedField);

            Mark(aliasLabel);
            owner.EmitFrameReturn(frame);
            var failureLocation = decision.SourceLine == 0
                ? location : new BeamSourceLocation(decision.PreprocessorFile ?? location.File, decision.SourceLine);
            EmitBooleanCaseFailure(failure, failureLocation);

            return true;

            void EmitProjectedTuple(Atom tag, int input, int field)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, input + 1);
                owner.EmitTuplePart(input, field, 0);
                owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(tag.Name)), (XRegisterTag, 0));
                owner.EmitFrameReturn(frame);
            }
        }

        private TerminalTaggedProjection? ReadTerminalTaggedProjection(Clause branch)
        {
            if (branch is not
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.Tuple],
                    Body: Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom resultTag }, Expr.Variable selected]
                    }
                } || selected.Name == VariableScopeNames.Wildcard)
            {
                return null;
            }

            var pattern = (Erlang.Pattern.Tuple)OmitUnusedCaseBindings(branch, null, null).Patterns[0];
            if (ReadSingleTupleField(pattern) is not { } access || !access.Tag.Equals(resultTag))
            {
                return null;
            }

            if (access.Field is Erlang.Pattern.Variable variable && variable.Name == selected.Name)
            {
                return new TerminalTaggedProjection(access.Tag, pattern.Items.Count, access.Index);
            }

            if (access.Field is not Erlang.Pattern.Tuple nested)
            {
                return null;
            }

            var fields = nested.Items.Select((field, index) => (field, index))
                .Where(item => item.field is not Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard }).ToArray();
            if (fields is not [(Erlang.Pattern.Variable child, var projection)] || child.Name != selected.Name)
            {
                return null;
            }

            return new TerminalTaggedProjection(
                access.Tag,
                pattern.Items.Count,
                access.Index,
                nested.Items.Count,
                projection
            );
        }
    }
}
