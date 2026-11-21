using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProjectedTaggedLiteralTail(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable argument],
                    Body: Expr.Case
                    {
                        Value: Expr.Variable source,
                        Clauses:
                                [
                                {
                                    Guard: null,
                                    Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable value, var field] }],
                                    Body: Expr.Call
                                    {
                                        DynamicRemote: false, Module: null, Arguments: [Expr.Variable forwarded, var constant]
                                    } call
                                },
                                {
                                    Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                    Body: Expr.Call { Arguments: [Expr.Literal] } thrown
                                }
                                ]
                    }
                }
                ]
                || !DistinctBindings(argument, value) || source.Name != argument.Name || forwarded.Name != value.Name
                || ReadClosedTuplePatternLiteral(field) is not { } exact
                || !AttributeLiteral.TryNormalizeConstant(constant, out Term extra)
                || !entries.TryGetValue((call.Function, call.Arguments.Count), out int entry)
                || ReadStackFreeThrow(thrown, requireCallerHandler: true) is not { } raised)
            {
                return false;
            }

            // Keep the tuple in the entry register until the only surviving
            // field is needed by the tail call. The closed pattern column is
            // an exact literal test, including fully constant nested tuples.
            int failure = Label();
            EmitDetailRecordTest(
                0,
                3,
                tag,
                failure
            );
            owner.EmitTuplePart(0, 2, 1);
            owner.Op(ExactEqualTest, LabelTag, failure);
            owner.Operand(XRegisterTag, 1);
            owner.LiteralOperand(exact);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitConstant(extra, 1);
            EmitTailLocal(entry, call.Arguments.Count, null);

            Mark(failure);
            owner.EmitConstant(((Expr.Literal)raised.Arguments[1]).Value, 1);
            owner.EmitConstant(Nil.Value, 2);
            owner.EmitConstant(Term.A(ErlangExceptionClasses.Throw), 0);
            owner.EmitCallLine(raised, location);
            EmitRegisterTailTarget(raised, raised.Arguments.Count);

            return true;
        }

        private static Term? ReadClosedTuplePatternLiteral(Pattern pattern)
        {
            switch (pattern)
            {
                case Pattern.Literal literal:
                    return literal.Value;
                case BitPattern { Segments.Count: 0 }:
                    return new BitString([]);
                case Pattern.Tuple tuple:
                    {
                        var fields = new Term[tuple.Items.Count];
                        for (int index = 0; index < tuple.Items.Count; index++)
                        {
                            if (ReadClosedTuplePatternLiteral(tuple.Items[index]) is not { } field)
                            {
                                return null;
                            }

                            fields[index] = field;
                        }

                        return Term.Tuple(fields);
                    }
                default:
                    return null;
            }
        }
    }
}
