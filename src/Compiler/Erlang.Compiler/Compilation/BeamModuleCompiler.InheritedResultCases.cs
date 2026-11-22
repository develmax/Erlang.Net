using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitInheritedResultCase(Expr.Case decision)
        {
            // A layout probe already knows the high-water mark of this
            // active frame, even though the final allocation has not been placed.
            int frameWidth = owner.layoutProbeSource is null ? frameSize : Math.Max(frameSize, peakSlot);
            if (frameWidth <= 0 || owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || recursiveClosure is not null || owner.receiveScratchRegisters is not null || deferredPairMembers.Count != 0
                || parentFunction is not { } function || owner.IsDeclaredNative((function.Name, function.Arity))
                || function.Clauses.Any(clause => LocalFunctionEntries.Walk(clause.Body).Any(expression =>
                    expression is Expr.Try or Expr.Catch or Expr.Receive))
                || decision.Value is not Expr.Call selector || !TryResolveOrdinaryCallTarget(selector, out string? module)
                || module == ErlangModuleNames.Module
                || !selector.Arguments.All(argument => argument is Expr.Variable variable && variables.ContainsKey(variable.Name)
                    || AttributeLiteral.TryNormalizeConstant(argument, out _))
                || decision.Clauses is not [var first, var second] || first.Guard is not null || second.Guard is not null)
            {
                return false;
            }

            var matched = first;
            var alternate = second;
            if (matched.Patterns is [global::Erlang.Pattern.Literal { Value: Atom }])
            {
                (matched, alternate) = (alternate, matched);
            }

            if (matched.Patterns is not
                [
                    global::Erlang.Pattern.Tuple
                {
                    Items:
                        [
                            global::Erlang.Pattern.Literal { Value: Atom tag },
                            global::Erlang.Pattern.Tuple { Items: [var left, var right] }
                        ]
                }
                ] || alternate.Patterns is not [global::Erlang.Pattern.Literal { Value: Atom literal }])
            {
                return false;
            }

            var outer = variables.OrderBy(binding => binding.Key, StringComparer.Ordinal).ToArray();
            var names = outer.Select(binding => binding.Key).ToArray();
            int[] homeSlots = outer.Select(binding => binding.Value).ToArray();
            if (homeSlots.Any(slot => slot < 0 || slot >= frameWidth) || homeSlots.Distinct().Count() != homeSlots.Length
                || homeSlots.Length + 2 >= FirstReservedXRegister)
            {
                return false;
            }

            var fresh = names.ToHashSet(StringComparer.Ordinal);
            var fields = new[] { left, right };
            string[] withPayload = [.. names, VariableScopeNames.Wildcard, VariableScopeNames.Wildcard];
            for (int index = 0; index < fields.Length; index++)
            {
                if (fields[index] is global::Erlang.Pattern.Any)
                {
                    continue;
                }

                if (fields[index] is not global::Erlang.Pattern.Variable binding
                    || binding.Name != VariableScopeNames.Wildcard && !fresh.Add(binding.Name))
                {
                    return false;
                }

                withPayload[names.Length + index] = binding.Name;
            }

            if (LowerTerminalCaseArm(null, matched.Body, withPayload) is not { } accepted
                || LowerTerminalCaseArm(null, alternate.Body, names) is not { } rejected)
            {
                return false;
            }

            var existing = Enumerable.Range(0, homeSlots.Length)
                .ToDictionary(value => value, value => (Tag: YRegisterTag, Register: homeSlots[value]));
            var bound = new Dictionary<int, (int Tag, int Register)>(existing);
            var projections = new List<TerminalProjection>();
            var used = accepted.Operations.SelectMany(operation => operation.Uses).ToHashSet();
            for (int index = 0; index < fields.Length; index++)
            {
                int value = names.Length + index;
                if (used.Contains(value))
                {
                    int register = projections.Count;
                    bound.Add(value, (XRegisterTag, register));
                    projections.Add(new(
                        1,
                        XRegisterTag,
                        register,
                        GetTupleElement,
                        index
                    ));
                }
            }

            if (AllocateTerminalCaseRegisters(accepted, withPayload, inherited: new(frameWidth, bound)) is not { } success
                || AllocateTerminalCaseRegisters(rejected, names, inherited: new(frameWidth, existing)) is not { } fallback)
            {
                return false;
            }

            int testAtom = Label();
            int failure = Label();
            Expression(selector);
            owner.Op(IsTuple, LabelTag, testAtom);
            owner.Operand(XRegisterTag, 0);
            EmitDetailRecordTest(
                0,
                2,
                tag,
                failure,
                TupleTypeBits
            );
            owner.EmitTuplePart(0, 1, 1);
            EmitTupleShapeTest(
                XRegisterTag,
                1,
                fields.Length,
                failure
            );
            EmitTerminalCaseArm(
                success with { Projections = projections.ToArray() },
                frame: true,
                frameAlreadyOpen: true,
                startLine: CallLocation(selector, location)
            );
            Mark(testAtom);
            owner.EmitAtomTest(0, literal.Name, failure);
            EmitTerminalCaseArm(
                fallback,
                frame: true,
                frameAlreadyOpen: true,
                startLine: CallLocation(selector, location)
            );

            // Neither rejected check has overwritten the original case result.
            Mark(failure);
            EmitCaseEnd(CaseSourceLocation(decision));

            return true;
        }
    }
}
