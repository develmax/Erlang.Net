using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGroupedTupleNilCase(Clause clause, int input, int failure)
        {
            if (clause.Patterns.Count >= FirstReservedXRegister || clause.Guard is not null
                || clause.Body is not Expr.Case { Value: Expr.Call, Clauses: [var empty, _] }
                || empty is not
                {
                    Guard: null,
                    Patterns: [Pattern.Literal { Value: Nil } or Pattern.List { Items: [], Tail: null }]
                }
                || clause.Patterns[input] is not Pattern.Tuple tuple || !HasTupleTag(tuple, out Atom tag)
                || tuple.Items.Skip(1).Any(item => item is not (Erlang.Pattern.Any or Pattern.Variable { Name: VariableScopeNames.Wildcard }))
                || clause.Patterns.Where((_, index) => index != input).Any(pattern => pattern is not Erlang.Pattern.Variable))
            {
                return false;
            }

            var parameters = clause.Patterns.ToArray();
            parameters[input] = new Pattern.Variable(VariableScopeNames.Wildcard);
            if (!DistinctBindings(parameters.Cast<Pattern.Variable>()
                .Where(parameter => parameter.Name != VariableScopeNames.Wildcard).ToArray())
                || ReadEntryRegisterCase(clause with { Patterns = parameters }, allowEmptyListArm: true) is not { } plan)
            {
                return false;
            }

            int scratch = parameters.Length;
            owner.EmitTuplePart(input, 0, scratch);
            owner.EmitAtomTest(scratch, tag.Name, failure);
            EmitEntryRegisterCase(plan);

            return true;
        }
    }
}
