using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const int ProperListTypeBits = ConsTypeBits | (1 << 7);

        private bool TryEmitSingletonConsDispatch(FunctionDefinition function, int functionInfo)
        {
            if (function.Arity >= FirstReservedXRegister || function.Clauses.Count != 3
                || function.Clauses.Any(clause => clause.Guard is not null))
            {
                return false;
            }

            var empty = function.Clauses.FirstOrDefault(clause =>
                clause.Patterns.Count(pattern => ListEntryTest(pattern) == IsNil) == 1
                && clause.Patterns.All(pattern => pattern is Pattern.Variable || ListEntryTest(pattern) == IsNil));
            if (empty is null)
            {
                return false;
            }

            var cons = function.Clauses.Where(clause => !ReferenceEquals(clause, empty)).ToArray();
            int input = Array.FindIndex(empty.Patterns.ToArray(), pattern => ListEntryTest(pattern) == IsNil);
            if (cons[0].Patterns[input] is not Pattern.List { Items: [Pattern.Variable singleton], Tail: null }
                || cons[1].Patterns[input] is not Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail }
                || ReadEntryListReturn([.. cons, empty]) is not { } emptyResult
                || Parameters(cons[0], singleton, null) is not { } one
                || Parameters(cons[1], head, tail) is not { } many
                || ReadBody(cons[0].Body, one) is not { } emitSingleton
                || ReadBody(cons[1].Body, many) is not { } emitCons)
            {
                return false;
            }

            bool proper = HasProperListEntry(function, input, followForwarding: true);
            // The case arms can allocate their own labels before either list fallback.
            owner.instructions.WriteByte(IsNonemptyList);
            int noConsOperand = checked((int)owner.instructions.Position);
            owner.Operand(LabelTag, 0);
            owner.Operand(XRegisterTag, input);
            owner.EmitListParts(
                input,
                XRegisterTag,
                function.Arity,
                XRegisterTag,
                input
            );
            owner.instructions.WriteByte(IsNil);
            int multipleOperand = checked((int)owner.instructions.Position);
            owner.Operand(LabelTag, 0);
            if (proper)
            {
                owner.EmitTypedRegister(input, ProperListTypeBits);
            }
            else
            {
                owner.Operand(XRegisterTag, input);
            }

            emitSingleton();
            int multiple = Label();
            owner.deferredLabelOperands.Add((multipleOperand, multiple));
            Mark(multiple);
            emitCons();
            int noCons = Label();
            owner.deferredLabelOperands.Add((noConsOperand, noCons));
            EmitEmptyListReturn(
                emptyResult,
                noCons,
                functionInfo,
                proper
            );

            return true;

            string[]? Parameters(Clause clause, Pattern.Variable first, Pattern.Variable? rest)
            {
                var parameters = new string[function.Arity + 1];
                var names = new HashSet<string>(StringComparer.Ordinal);
                for (int register = 0; register < function.Arity; register++)
                {
                    Pattern.Variable? variable = register == input ? rest : clause.Patterns[register] as Pattern.Variable;
                    if (variable is null && register != input)
                    {
                        return null;
                    }

                    string name = variable?.Name ?? VariableScopeNames.Wildcard;
                    if (name != VariableScopeNames.Wildcard && !names.Add(name))
                    {
                        return null;
                    }

                    parameters[register] = name;
                }

                parameters[^1] = first.Name;

                return first.Name == VariableScopeNames.Wildcard || names.Add(first.Name) ? parameters : null;
            }

            Action? ReadBody(Expr body, string[] parameters)
            {
                if (body is Expr.Case && ReadEntryRegisterCase(
                    new Clause(parameters.Select(name => (Pattern)new Pattern.Variable(name)).ToArray(), null, body),
                    listParts: (function.Arity, input)
                ) is { } registerCase)
                {
                    return () => EmitEntryRegisterCase(registerCase);
                }

                if ((ReadEntryCallChain(body, parameters) ?? ReadStraightCallSequence(body, parameters)) is { } chain)
                {
                    return () => EmitEntryCallChain(
                        chain.First,
                        parameters,
                        chain.Continuation,
                        chain.RelocateBeforeTrim
                    );
                }

                if (ReadSavedCallSequence(body, parameters) is { } saved)
                {
                    return () => EmitSavedCallSequence(saved);
                }

                if (body is Expr.Call call)
                {
                    if (ReadEntryCall(owner.ResolveImportedCall(call, entries), parameters, allowControlTransfer: true) is { } target)
                    {
                        return () =>
                        {
                            EmitRegisterTailCall(target);
                        };
                    }

                    return ReadEntryTailConstruction(call, parameters, allowOrdinary: true) is { } constructed
                        ? () =>
                        {
                            EmitCallArgumentConstructions(constructed.Plan, -1);
                            EmitRegisterTailTarget(constructed.Call, constructed.Call.Arguments.Count);
                        }
                    : null;
                }

                if (AttributeLiteral.TryNormalizeConstant(body, out Term constant))
                {
                    return () =>
                    {
                        owner.EmitConstant(constant, 0);
                        owner.instructions.WriteByte(Return);
                    };
                }

                if (body is Expr.Variable result && result.Name != VariableScopeNames.Wildcard
                    && Array.IndexOf(parameters, result.Name) is >= 0 and var source)
                {
                    return () =>
                    {
                        owner.MoveRegister(
                            XRegisterTag,
                            source,
                            XRegisterTag,
                            0
                        );
                        owner.instructions.WriteByte(Return);
                    };
                }

                var inputs = parameters.Select((name, register) => (name, register))
                    .Where(binding => binding.name != VariableScopeNames.Wildcard)
                    .ToDictionary(binding => binding.name, binding => binding.register, StringComparer.Ordinal);

                return ReadCallArgumentConstructions(
                    [body],
                    -1,
                    new Dictionary<string, int>(),
                    inputs: inputs
                ) is { } resultPlan
                    ? () =>
                    {
                        EmitCallArgumentConstructions(resultPlan, -1);
                        owner.instructions.WriteByte(Return);
                    }
                : null;
            }
        }
    }
}
