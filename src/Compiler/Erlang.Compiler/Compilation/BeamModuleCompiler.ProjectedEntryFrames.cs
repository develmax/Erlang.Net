using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private readonly record struct TerminalField(int Value, byte Opcode, int Index);
        private readonly record struct TerminalProjection(
            int Input,
            int Tag,
            int Register,
            byte Opcode,
            int Index,
            int InputTag = XRegisterTag
        );

        private bool TryEmitPlannedProjectedEntry(FunctionDefinition function, int functionInfo)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Clauses.Count is < 1 or > 2 || function.Clauses.Any(clause => clause.Guard is not null))
            {
                return false;
            }

            Clause first = function.Clauses[0];
            Clause? second = function.Clauses.Count == 2 ? function.Clauses[1] : null;

            if (second is not null && TryEmitPlannedSingletonListEntry(first, second, functionInfo))
            {
                return true;
            }

            if ((ReadProjectedEntry(first, second) ?? (second is null ? null : ReadReversedProjectedEntry(second, first))) is not { } plan)
            {
                return false;
            }

            var deferred = PlanDeferredEntryTupleFields(plan.Matched, plan.Input, plan.Fields);
            if (second is null && deferred is null)
            {
                return false;
            }

            const int dispatch = 0;
            const int accepted = 1;
            const int remaining = 2;
            var frames = BeamFramePlacement.Place(
                dispatch,
                [
                    new(
                        dispatch,
                        [remaining, accepted],
                        [],
                        RequiresFrame: false
                    ),
                    new(
                        accepted,
                        [],
                        [],
                        RequiresFrame: plan.Matched.RequiresFrame
                    ),
                    new(
                        remaining,
                        [],
                        [],
                        RequiresFrame: plan.Unmatched?.RequiresFrame == true
                    )
                ]
            );
            if (frames is null || frames.Owners[dispatch] is not null)
            {
                return false;
            }

            TerminalCaseBranch? matched = AllocateTerminalCaseRegisters(
                plan.Matched,
                plan.Parameters,
                plan.Input,
                plan.Fields,
                deferred
            );
            if (matched is null && deferred is not null && second is not null)
            {
                matched = AllocateTerminalCaseRegisters(
                    plan.Matched,
                    plan.Parameters,
                    plan.Input,
                    plan.Fields
                );
            }

            if (matched is null)
            {
                return false;
            }

            TerminalCaseBranch? fallback = plan.Unmatched is null ? null
                : AllocateTerminalCaseRegisters(plan.Unmatched, plan.FallbackParameters);
            if (plan.Unmatched is not null && fallback is null)
            {
                return false;
            }

            int miss = fallback is null ? functionInfo : Label();
            if (plan.Tag is { } tag)
            {
                owner.Op(IsTaggedTuple, LabelTag, miss);
                owner.Operand(XRegisterTag, plan.Input);
                owner.Operand(BeamWire.UnsignedTag, plan.TupleArity);
                owner.LiteralOperand(tag);
            }
            else if (plan.TupleArity != 0)
            {
                EmitTupleShapeTest(
                    XRegisterTag,
                    plan.Input,
                    plan.TupleArity,
                    miss
                );
            }
            else
            {
                owner.Op(IsNonemptyList, LabelTag, miss);
                owner.Operand(XRegisterTag, plan.Input);
            }

            EmitTerminalCaseArm(matched, frames.Owners[accepted] is not null);
            if (fallback is null)
            {
                return true;
            }

            Mark(miss);
            if (plan.TestNil && !(owner.layoutProbeSource ?? owner).HasListEntryFactWithCallResults(
                (function.Name, function.Arity),
                plan.Input
            ))
            {
                owner.Op(IsNil, LabelTag, functionInfo);
                owner.Operand(XRegisterTag, plan.Input);
            }

            if (plan.TestNil && plan.Input == 0
                && fallback is { Calls.Length: 0, ReturnConstructions.Schedule.Length: 0, Result.Literal: Nil })
            {
                // The only successful false edge is the empty list, already in X0.
                owner.instructions.WriteByte(Return);
            }
            else
            {
                EmitTerminalCaseArm(fallback, frames.Owners[remaining] is not null);
            }

            return true;
        }

        private sealed record ProjectedTerminalEntry(
            int Input,
            Atom? Tag,
            int TupleArity,
            bool TestNil,
            string[] Parameters,
            TerminalField[] Fields,
            VirtualTerminalCaseArm Matched,
            string[] FallbackParameters,
            VirtualTerminalCaseArm? Unmatched
        );

        private ProjectedTerminalEntry? ReadReversedProjectedEntry(Clause matched, Clause remaining) =>
            ReadProjectedEntry(matched, remaining) is { TestNil: true } disjoint ? disjoint : null;

        private ProjectedTerminalEntry? ReadProjectedEntry(Clause matched, Clause? remaining)
        {
            int input = -1;
            int arity = matched.Patterns.Count;
            int tupleArity = 0;
            Atom? tag = null;
            bool testNil = false;
            string? nilAlias = null;
            var parameters = new List<string>(arity);
            var alternate = new List<string>(arity);
            var fields = new List<(string Name, byte Opcode, int Index)>();
            for (int index = 0; index < arity; index++)
            {
                var (pattern, alias) = UnwrapTerminalPattern(matched.Patterns[index]);
                var (other, fallbackAlias) = remaining is null
                    ? (new Erlang.Pattern.Variable(VariableScopeNames.Wildcard), (string?)null)
                    : UnwrapTerminalPattern(remaining.Patterns[index]);
                if (pattern is Pattern.Variable source && other is Pattern.Variable target)
                {
                    parameters.Add(source.Name);
                    alternate.Add(target.Name);
                    continue;
                }

                if (input >= 0)
                {
                    return null;
                }

                input = index;
                parameters.Add(alias ?? VariableScopeNames.Wildcard);
                switch (pattern)
                {
                    case Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail }:
                        fields.Add((head.Name, GetHead, 0));
                        fields.Add((tail.Name, GetTail, 1));
                        if (other is Erlang.Pattern.Variable unmatched)
                        {
                            alternate.Add(unmatched.Name);
                        }
                        else if (other is Pattern.Literal { Value: Nil } or Pattern.List { Items.Count: 0, Tail: null })
                        {
                            testNil = true;
                            nilAlias = fallbackAlias;
                            alternate.Add(nilAlias ?? VariableScopeNames.Wildcard);
                        }
                        else
                        {
                            return null;
                        }

                        break;
                    case Pattern.Tuple { Items.Count: > 0 } tuple when other is Pattern.Variable otherInput:
                        tag = (tuple.Items[0] as Erlang.Pattern.Literal)?.Value as Atom;
                        int firstField = tag is null ? 0 : 1;
                        if (tuple.Items.Skip(firstField).Any(member => member is not Erlang.Pattern.Variable))
                        {
                            return null;
                        }

                        tupleArity = tuple.Items.Count;
                        for (int field = firstField; field < tupleArity; field++)
                        {
                            fields.Add((((Pattern.Variable)tuple.Items[field]).Name, GetTupleElement, field));
                        }

                        alternate.Add(otherInput.Name);
                        break;
                    default:
                        return null;
                }
            }

            var projections = new List<TerminalField>();
            foreach (var field in fields.Where(field => field.Name != VariableScopeNames.Wildcard))
            {
                projections.Add(new(parameters.Count, field.Opcode, field.Index));
                parameters.Add(field.Name);
            }

            if (input < 0 || !HasDistinctTerminalBindings(parameters) || !HasDistinctTerminalBindings(alternate)
                || parameters.Count >= FirstReservedXRegister
                || LowerTerminalCaseArm(null, matched.Body, parameters.ToArray()) is not { } body)
            {
                return null;
            }

            VirtualTerminalCaseArm? fallback = remaining is null ? null : LowerTerminalCaseArm(
                null,
                remaining.Body,
                alternate.ToArray(),
                nilAlias,
                Nil.Value
            );
            if (remaining is not null && fallback is null || !body.RequiresFrame && fallback?.RequiresFrame != true)
            {
                return null;
            }

            return new(
                input,
                tag,
                tupleArity,
                testNil,
                parameters.ToArray(),
                projections.ToArray(),
                body,
                alternate.ToArray(),
                fallback
            );
        }

        private static bool HasDistinctTerminalBindings(IEnumerable<string> parameters)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);

            return parameters.All(name => name == VariableScopeNames.Wildcard || names.Add(name));
        }

        private static (Pattern Shape, string? Alias) UnwrapTerminalPattern(Pattern pattern) =>
            pattern is AliasPattern binding && TryReadPatternAlias(binding, out var variable, out var shape)
                && shape is not Erlang.Pattern.Variable ? (shape, variable.Name) : (pattern, null);
    }
}
