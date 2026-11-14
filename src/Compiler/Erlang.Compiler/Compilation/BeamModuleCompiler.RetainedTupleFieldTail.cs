using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitRetainedTupleFieldTail(FunctionDefinition function)
        {
            if (function.Clauses is not [{ Guard: null, Patterns: [AliasPattern alias] } clause]
                || EntryShape(alias) is not Erlang.Pattern.Tuple tuple
                || (alias.Left as Erlang.Pattern.Variable ?? alias.Right as Erlang.Pattern.Variable) is not { } retained
                || ReadSingleTupleField(tuple) is not { Field: Erlang.Pattern.Variable field } projection
                || !DistinctBindings(retained, field))
            {
                return false;
            }

            Expr.Call producer;
            Expr.Call tail;
            string resultName;
            if (clause.Body is Expr.Call outer && outer.Arguments.OfType<Expr.Call>().ToArray() is [var inner])
            {
                const string nestedResult = "\0retained-tuple-result";
                producer = inner;
                resultName = nestedResult;
                tail = outer with
                {
                    Arguments = outer.Arguments.Select(argument =>
                        ReferenceEquals(argument, inner) ? new Expr.Variable(nestedResult) : argument).ToArray()
                };
            }
            else if (clause.Body is Expr.Sequence
                {
                    Items:
                    [
                        Expr.Match { Pattern: Erlang.Pattern.Variable result, Value: Expr.Call innerCall },
                        Expr.Call outerCall
                    ]
                } && DistinctBindings(retained, field, result))
            {
                producer = innerCall;
                tail = outerCall;
                resultName = result.Name;
            }
            else
            {
                return false;
            }

            if (producer.Arguments is not [Expr.Variable projected] || projected.Name != field.Name
                || ReadEntryCall(producer, [field.Name]) is not { } first
                || ReadEntryCall(tail, [resultName], [retained.Name], allowControlTransfer: true) is not { SavedInputs.Count: > 0 } last)
            {
                return false;
            }

            EmitDetailRecordTest(0, tuple.Items.Count, projection.Tag, owner.nextLabel - 2);
            EmitFrameAllocation(1, 1);
            owner.MoveRegister(XRegisterTag, 0, YRegisterTag, 0);
            owner.EmitTuplePart(0, projection.Index, 0);
            EmitEntryCall(first);
            owner.EmitOrderedTailMoves(last.Moves, last.Constants, last.SavedInputs);
            EmitPreparedFrameTail(last, CallLocation(producer, location), 1);

            return true;
        }
    }
}
