using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGroupedRecordCarrierEntry(
            Clause clause,
            int input,
            int failure,
            int baseSlot
        )
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || owner.receiveScratchRegisters is not null || input != 0 || clause.Guard is not null
                || clause.Patterns is not [Pattern.Tuple eventShape, var carrier]
                || !ReadVariableTupleShape(eventShape, out Atom eventTag)
                || !ReadVariableTupleShape(EntryShape(carrier), out _)
                || LocalFunctionEntries.Walk(clause.Body).Any(expression =>
                    expression is Expr.Try or Expr.Receive or Expr.Catch or Expr.Maybe or Expr.Fun))
            {
                return false;
            }

            // The surrounding group owns the event arity. Check its remaining
            // tag in X before the ordinary entry planner checks the state shape.
            var context = new SharedNestedEntryContext(null, null, failure);
            HashSet<int> checkedEventArity = [];
            if (!TryEmitPlannedTupleEntry(
                clause,
                failure,
                checkedRootFields: checkedEventArity,
                validateOnly: true,
                sharedEntry: context
            ))
            {
                return false;
            }

            owner.EmitTuplePart(0, 0, clause.Patterns.Count);
            owner.EmitAtomTest(clause.Patterns.Count, eventTag.Name, failure);

            int savedSlot = nextSlot;
            var incoming = variables;
            nextSlot = baseSlot;
            bool emitted = TryEmitPlannedTupleEntry(
                clause,
                failure,
                checkedRootFields: checkedEventArity,
                sharedEntry: context
            );
            nextSlot = savedSlot;
            variables = incoming;

            return emitted;
        }

        private static bool ReadVariableTupleShape(Pattern pattern, out Atom tag)
        {
            if (pattern is Pattern.Tuple tuple && HasTupleTag(tuple, out tag)
                && tuple.Items.Skip(1).All(field => field is Erlang.Pattern.Variable or Erlang.Pattern.Any))
            {
                return true;
            }

            tag = null!;

            return false;
        }
    }
}
