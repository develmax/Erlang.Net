using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private FunctionDefinition FoldSingletonEntryCase(FunctionDefinition function)
        {
            if (!CanUseEntryTypeFacts()
                || function.Clauses is not
                [
                {
                    Guard: null, Body: Expr.Case
                    {
                        Value: Expr.Call call,
                        Clauses: [{ Guard: null, Patterns: [Pattern.Literal { Value: Atom key }], Body: Expr.Call tail }]
                    }
                } clause
                ] || !HasUnconditionalParameters(clause))
            {
                return function;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            if (ReadEntryCall(call, parameters) is not { Module: null } producer
                || !ReturnsLocalCallConstant(producer, key)
                || ReadEntryCall(tail, [], parameters) is null)
            {
                return function;
            }

            // The selected arm is unconditional after a successful call. The call can still raise or have effects.
            return function with { Clauses = [clause with { Body = new Expr.Sequence([call, tail]) }] };
        }

        private bool TryEmitFiniteAtomEntryCallTails(FunctionDefinition function)
        {
            if (!CanUseEntryTypeFacts() || function.Clauses is not
                [
                { Guard: null, Body: Expr.Call immediate } first,
                {
                    Guard: null, Body: Expr.Sequence
                    {
                        Items: [Expr.Match
                        {
                            Pattern: Pattern.Variable { Name: not VariableScopeNames.Wildcard } bound,
                            Value: Expr.Call producer
                        }, Expr.Call continuation]
                    }
                } second
                ])
            {
                return false;
            }

            int selector = Enumerable.Range(0, function.Arity).FirstOrDefault(index =>
                first.Patterns[index] is Pattern.Literal { Value: Atom }
                && second.Patterns[index] is Pattern.Literal { Value: Atom }, -1);
            if (selector < 0 || !ReadParameters(first, selector, out string[] firstInputs)
                || !ReadParameters(second, selector, out string[] secondInputs)
                || secondInputs.Contains(bound.Name, StringComparer.Ordinal)
                || ReadEntryCall(immediate, firstInputs) is not { } direct
                || ReadEntryCall(producer, secondInputs) is not { Module: null } local
                || !localFunctions.TryGetValue((producer.Function, producer.Arguments.Count), out var definition)
                || ReadSingletonResult(definition) is not Atom result || !ReturnsLocalCallConstant(local, result)
                || !continuation.Arguments.OfType<Expr.Variable>().Any(item => item.Name == bound.Name))
            {
                return false;
            }

            var firstKey = (Atom)((Pattern.Literal)first.Patterns[selector]).Value;
            var secondKey = (Atom)((Pattern.Literal)second.Patterns[selector]).Value;
            if (firstKey.Equals(secondKey) || TermOrder.Compare(firstKey, secondKey, exact: true) <= 0
                || ReadFiniteAtomArguments(function, selector, admitGuardFacts: true) is not { Count: 2 } values
                || !values.Contains(firstKey) || !values.Contains(secondKey))
            {
                return false;
            }

            var specialized = continuation with
            {
                Arguments = continuation.Arguments.Select(item => item is Expr.Variable variable && variable.Name == bound.Name
                    ? new Expr.Literal(result)
                    : item).ToArray()
            };
            if (ReadEntryCall(specialized, [], secondInputs) is not { Module: null } tail)
            {
                return false;
            }

            int alternative = Label();
            owner.Op(ExactEqualTest, LabelTag, alternative);
            owner.Operand(XRegisterTag, selector);
            owner.LiteralOperand(firstKey);
            EmitRegisterTailCall(direct);
            Mark(alternative);
            EmitEntryCallChain(
                local,
                secondInputs,
                [(tail, null)],
                relocateBeforeTrim: true
            );

            return true;

            static bool ReadParameters(Clause clause, int selector, out string[] parameters)
            {
                parameters = new string[clause.Patterns.Count];
                var names = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < clause.Patterns.Count; index++)
                {
                    if (index == selector)
                    {
                        parameters[index] = VariableScopeNames.Wildcard;
                    }
                    else if (clause.Patterns[index] is Pattern.Variable variable
                        && (variable.Name == VariableScopeNames.Wildcard || names.Add(variable.Name)))
                    {
                        parameters[index] = variable.Name;
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private bool CanUseEntryTypeFacts() =>
            !owner.ModuleOptimizationDisabled && owner.SsaTypeInferenceEnabled && owner.OnLoadFunction is null;
    }
}
