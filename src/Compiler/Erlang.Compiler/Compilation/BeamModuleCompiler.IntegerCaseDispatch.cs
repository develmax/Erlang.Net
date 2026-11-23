using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record IntegerCase(
            Expr.Call Decode,
            Expr.Call Failure,
            List<Atom> Tags,
            SortedDictionary<int, Atom> Alternatives
        );

        private bool TryEmitIntegerCaseDispatch(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable tags],
                    Body: var body
                }
                ]
                || !DistinctBindings(input, tags)
                || ReadIntegerCase(body, name => name != input.Name && name != tags.Name) is not { } integerCase
                || integerCase.Decode.Arguments is not [Expr.Variable source, Expr.Variable options]
                || input.Name != source.Name || tags.Name != options.Name
                || !entries.TryGetValue((integerCase.Decode.Function, 2), out int target))
            {
                return false;
            }

            var decode = integerCase.Decode;
            EmitFrameAllocation(2, 0);
            owner.EmitCallLine(decode, location);
            owner.EmitFieldCall(2, target);
            var schedule = ScheduleIntegerChoices(integerCase);
            int fallback = Label();
            EmitIntegerSelect(schedule, fallback);
            foreach (var branch in schedule)
            {
                Mark(branch.Label);
                owner.EmitConstant(branch.Value, 0);
                owner.EmitFrameReturn(0);
            }

            Mark(fallback);
            EmitTaggedExit(
                integerCase.Failure,
                integerCase.Tags,
                CallLocation(decode, location),
                framed: true
            );

            return true;
        }

        private IntegerCase? ReadIntegerCase(Expr body, Func<string, bool> isUnbound)
        {
            if (body is not Expr.Case
                {
                    Value: Expr.Call { Module: null, DynamicRemote: false } decode,
                    Clauses: [_, _, .., { Guard: null, Patterns: [Pattern.Variable value] } rejected]
                } decision
                || value.Name == VariableScopeNames.Wildcard || !isUnbound(value.Name)
                || (InferPairFacts(decode) & PairFacts.Integer) == 0
                || ReadTaggedExit(rejected.Body, value.Name) is not { } failure
                || !owner.ResolvesErlangBif(failure.Call))
            {
                return null;
            }

            var branches = new SortedDictionary<int, Atom>();
            foreach (Clause clause in decision.Clauses.SkipLast(1))
            {
                if (clause is not
                    {
                        Guard: null, Patterns: [Pattern.Literal { Value: Integer key }],
                        Body: Expr.Literal { Value: Atom atom }
                    }
                    || key.Value < int.MinValue || key.Value > int.MaxValue || !branches.TryAdd((int)key.Value, atom))
                {
                    return null;
                }
            }

            return new(
                decode,
                failure.Call,
                failure.Tags,
                branches
            );
        }

        private (int Key, Atom Value, int Label)[] ScheduleIntegerChoices(IntegerCase plan) =>
            plan.Alternatives.Reverse().Select(branch => (branch.Key, branch.Value, Label())).ToArray();

        private void EmitIntegerSelect((int Key, Atom Value, int Label)[] schedule, int fallback)
        {
            owner.instructions.WriteByte(SelectValue);
            EmitRangeResultOperand(integerResult: true);
            owner.Operand(LabelTag, fallback);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, schedule.Length * 2);
            foreach (var branch in schedule.Reverse())
            {
                owner.Operand(BeamWire.IntegerTag, branch.Key);
                owner.Operand(LabelTag, branch.Label);
            }
        }
    }
}
