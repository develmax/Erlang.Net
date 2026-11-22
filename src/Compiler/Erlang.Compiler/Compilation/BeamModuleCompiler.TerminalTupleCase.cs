using static Erlang.Compiler.BeamEmissionFormat;
using AstPattern = Erlang.Pattern;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const int TailPhiCopyCostPerPredecessor = 3;
        private const int TailPhiCopyCostBase = 2;

        private sealed record TerminalTupleCaseEntry(int Input, Atom Tag);

        private bool TryEmitPlannedTerminalTupleCase(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || recursiveClosure is { SelfName: not null } or { GuardFailure: not null }
                || function.Clauses.Count is < 1 or > 2
                || function.Clauses[0] is not { Guard: null, Body: Expr.Tuple result } selected)
            {
                return false;
            }

            Clause? alternate = function.Clauses.Count == 2 ? function.Clauses[1] : null;
            if (alternate is not null && (alternate.Guard is not null || !HasUnconditionalParameters(alternate)))
            {
                return false;
            }

            int projectedInput = -1;
            Atom? entryTag = null;
            var parameters = new string[function.Arity];
            var bindings = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < parameters.Length; index++)
            {
                AstPattern pattern = selected.Patterns[index];
                if (alternate is not null && projectedInput < 0
                    && pattern is AstPattern.Tuple
                    {
                        Items: [
                            AstPattern.Literal { Value: Atom tag },
                            AstPattern.Variable { Name: not VariableScopeNames.Wildcard } payload
                        ]
                    }
                    && alternate.Patterns[index] is AstPattern.Variable { Name: not VariableScopeNames.Wildcard } original
                    && alternate.Body is Expr.Variable returned && original.Name == returned.Name)
                {
                    entryTag = tag;
                    projectedInput = index;
                    pattern = payload;
                }

                if (pattern is not AstPattern.Variable binding
                    || binding.Name != VariableScopeNames.Wildcard && !bindings.Add(binding.Name))
                {
                    return false;
                }

                parameters[index] = binding.Name;
            }

            if (alternate is not null && projectedInput < 0
                || ReadTailTupleCase(result, bindings) is not
                { Value: Expr.Variable map, Clauses: [{ Patterns: [MapPattern] }, _] } decision)
            {
                return false;
            }

            int selector = Array.IndexOf(parameters, map.Name);
            if (selector < 0)
            {
                return false;
            }

            TerminalTupleCaseEntry? entry = entryTag is null ? null : new(projectedInput, entryTag);

            return TryEmitPlannedTerminalMapCase(
                function,
                decision,
                parameters,
                selector,
                terminalTupleResult: true,
                tupleEntry: entry
            );
        }

        private static Expr.Case? ReadTailTupleCase(Expr.Tuple result, HashSet<string> bindings)
        {
            int item = -1;
            Expr.Case? decision = null;
            for (int index = 0; index < result.Items.Count; index++)
            {
                Expr operand = result.Items[index];
                if (operand is Expr.Case { Clauses.Count: 2 } choice && decision is null)
                {
                    item = index;
                    decision = choice;
                }
                else if (operand is Expr.Variable variable ? !bindings.Contains(variable.Name)
                    : !AttributeLiteral.TryNormalizeConstant(operand, out _))
                {
                    return null;
                }
            }

            if (decision is null)
            {
                return null;
            }

            // The only shared terminal operation is put_tuple. Substitute the
            // case value from each normal predecessor before placing frames.
            var branches = new Clause[decision.Clauses.Count];
            int copyCost = 0;
            for (int index = 0; index < branches.Length; index++)
            {
                Clause branch = decision.Clauses[index];
                Expr[] operands = result.Items.ToArray();
                operands[item] = branch.Body;
                var constructed = result with { Items = operands };
                if (!AttributeLiteral.TryNormalizeConstant(constructed, out _))
                {
                    copyCost += operands.Length + 1;
                }

                branches[index] = branch with { Body = constructed };
            }

            int budget = TailPhiCopyCostPerPredecessor * branches.Length + TailPhiCopyCostBase;

            return copyCost <= budget ? decision with { Clauses = branches } : null;
        }
    }
}
