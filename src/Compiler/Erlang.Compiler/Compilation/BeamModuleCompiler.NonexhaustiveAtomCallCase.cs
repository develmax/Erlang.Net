using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitNonexhaustiveAtomCallCase(Clause clause)
        {
            if (recursiveClosure is not null || owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || owner.OnLoadFunction is not null
                || clause.Guard is not null || !HasUnconditionalParameters(clause)
                || clause.Body is not Expr.Case { Value: Expr.Variable selected, Clauses.Count: 2 } decision
                || IsBooleanCase(decision))
            {
                return false;
            }

            string[] parameters = clause.Patterns.Cast<Pattern.Variable>().Select(parameter => parameter.Name).ToArray();
            int selector = Array.IndexOf(parameters, selected.Name);
            if (selector < 0)
            {
                return false;
            }

            var branches = new List<(Atom Key, EntryCall Tail, EntryCall? ConstantCall, int? SavedInput)>();
            foreach (Clause branch in decision.Clauses)
            {
                if (branch is not { Guard: null, Patterns: [Pattern.Literal { Value: Atom key }], Body: Expr.Call call }
                    || branches.Any(previous => previous.Key.Equals(key)))
                {
                    return false;
                }

                Expr.Call specialized = call with
                {
                    Arguments = call.Arguments.Select(argument => argument is Expr.Variable variable && variable.Name == selected.Name
                        ? new Expr.Literal(key)
                        : argument).ToArray()
                };
                if (ReadEntryCall(specialized, parameters) is { } direct)
                {
                    branches.Add((key, direct, null, null));
                    continue;
                }

                if (specialized.Arguments is not [Expr.Variable saved, Expr.Call { Arguments.Count: 0 } inner]
                    || Array.IndexOf(parameters, saved.Name) is not (>= 0 and var savedInput)
                    || ReadEntryCall(inner, []) is not { Module: null } producer
                    || !localFunctions.TryGetValue((inner.Function, 0), out var definition)
                    || ReadSingletonResult(definition) is not Atom result
                    || !ReturnsLocalCallConstant(producer, result))
                {
                    return false;
                }

                var continuation = specialized with { Arguments = [new Expr.Variable(saved.Name), new Expr.Literal(result)] };
                if (ReadEntryCall(continuation, [], [saved.Name]) is not { Module: null } tail)
                {
                    return false;
                }

                branches.Add((key, tail, producer, savedInput));
            }

            if (branches.Count(branch => branch.ConstantCall is null) != 1)
            {
                return false;
            }

            branches.Sort((left, right) => TermOrder.Compare(right.Key, left.Key, exact: true));
            int[] targets = branches.Select(_ => Label()).ToArray();
            int failure = Label();
            owner.EmitMembershipSelect(
                selector,
                failure,
                branches.Select((branch, index) => ((Term)branch.Key, targets[index])).Reverse()
            );
            for (int index = 0; index < branches.Count; index++)
            {
                Mark(targets[index]);
                var (_, tail, producer, saved) = branches[index];
                if (producer is null)
                {
                    EmitRegisterTailCall(tail);

                    continue;
                }

                int savedInput = saved!.Value;
                // The proven return value replaces the argument, while the producer still runs in this branch.
                EmitFrameAllocation(savedInput + 1, 1);
                owner.MoveRegister(
                    XRegisterTag,
                    savedInput,
                    YRegisterTag,
                    0
                );
                EmitEntryCall(producer);
                EmitEntryCallArguments(tail);
                owner.MoveRegister(
                    YRegisterTag,
                    0,
                    XRegisterTag,
                    0
                );
                EmitPreparedFrameTail(tail, CallLocation(producer.Call, location), 1);
            }

            Mark(failure);
            owner.EmitLine(CaseSourceLocation(decision) ?? location);
            owner.Op(CaseEnd, XRegisterTag, selector);

            return true;
        }
    }
}
