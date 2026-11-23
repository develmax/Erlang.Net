using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitGuardedTupleAliasComprehension(FunctionDefinition function)
        {
            Clause match = function.Clauses[0];
            if (!owner.SsaTypeInferenceEnabled || !function.IsRecursiveListMapper || function.IsStrictRecursiveListMapper
                || match is not
                {
                    Guard: { } guard,
                    Patterns: [Erlang.Pattern.List { Items: [AliasPattern item], Tail: Erlang.Pattern.Variable tail }, ..],
                    Body: Expr.List { Items: [Expr.Variable mapped], Tail: Expr.Call { Module: null } recurse }
                }
                || recurse.Function != function.Name || recurse.Arguments.Count != function.Arity
                || !TryReadPatternAlias(item, out var alias, out var shape) || shape is not Erlang.Pattern.Tuple tuple
                || alias.Name != mapped.Name || !entries.TryGetValue((function.Name, function.Arity), out int entry))
            {
                return false;
            }

            int head = function.Arity;
            var inputs = new List<Pattern> { tail };
            inputs.AddRange(match.Patterns.Skip(1));
            inputs.Add(alias);
            var names = match.Patterns.SelectMany(Semantics.Variables).ToArray();
            if (names.Distinct(StringComparer.Ordinal).Count() != names.Length
                || inputs.Count >= FirstReservedXRegister || inputs.Any(input => input is not Erlang.Pattern.Variable))
            {
                return false;
            }

            var guardNames = LocalFunctionEntries.Walk(guard).OfType<Expr.Variable>()
                .Select(variable => variable.Name).ToHashSet(StringComparer.Ordinal);
            var projections = new List<(int Source, int Field, int Register, Pattern Pattern)>();
            var guardProjections = new List<(int Source, int Field, int Register)>();
            if (!ReadProjections(tuple, head))
            {
                return false;
            }

            var guardClause = match with { Patterns = inputs };
            var atomKeys = new List<Atom>();
            int atomRegister = guardNames.Count == 1
                ? inputs.FindIndex(input => input is Erlang.Pattern.Variable variable && guardNames.Contains(variable.Name)) : -1;
            bool atomSelection = atomRegister >= 0 && ReadAtomGuardKeys(guard, guardNames.Single(), atomKeys)
                && atomKeys.Distinct().Count() > 1;
            if (!atomSelection && !CanEmitGuard(guard))
            {
                return false;
            }

            int empty = Label();
            int skip = Label();
            owner.EmitNonemptyList(0, empty);
            owner.EmitListParts(
                0,
                XRegisterTag,
                head,
                XRegisterTag,
                0
            );
            EmitShape(tuple, head);
            foreach (var (source, field, register, pattern) in projections)
            {
                owner.EmitTuplePart(source, field, register);
                if (pattern is Erlang.Pattern.Tuple nested)
                {
                    EmitShape(nested, register);
                }
                else if (pattern is Erlang.Pattern.Literal literal)
                {
                    owner.Op(ExactEqualTest, LabelTag, skip);
                    owner.Operand(XRegisterTag, register);
                    owner.LiteralOperand(literal.Value);
                }
            }

            foreach (var (source, field, register) in guardProjections)
            {
                owner.EmitTuplePart(source, field, register);
            }

            if (atomSelection)
            {
                int accepted = Label();
                var order = Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true));
                owner.EmitMembershipSelect(atomRegister, skip,
                    atomKeys.Distinct().Order(order).Select(key => ((Term)key, accepted)));
                Mark(accepted);
            }
            else
            {
                EmitGuard(guard, skip);
            }

            // The skip edge already has the tail and captures in its recursive argument registers.
            // Only the successful branch needs the whole tuple after the recursive call.
            EmitFrameAllocation(head + 1, 1);
            owner.MoveRegister(
                XRegisterTag,
                head,
                YRegisterTag,
                0
            );
            owner.EmitRecursiveCons(entry, function.Arity);
            Mark(skip);
            EmitRecursiveListMappingRejection(function, entry, head);
            EmitRecursiveListMappingEnd(function, empty);

            return true;

            bool ReadProjections(Erlang.Pattern.Tuple current, int source)
            {
                int first = HasTupleTag(current, out _) ? 1 : 0;
                for (int field = first; field < current.Items.Count; field++)
                {
                    Pattern pattern = current.Items[field];
                    if (pattern is Erlang.Pattern.Any || pattern is Erlang.Pattern.Variable variable && !guardNames.Contains(variable.Name))
                    {
                        continue;
                    }

                    if (inputs.Count == FirstReservedXRegister
                        || pattern is not (Erlang.Pattern.Variable or Erlang.Pattern.Tuple or Erlang.Pattern.Literal { Value: Atom or Integer or Nil }))
                    {
                        return false;
                    }

                    int register = inputs.Count;
                    inputs.Add(pattern as Erlang.Pattern.Variable ?? new Erlang.Pattern.Variable(VariableScopeNames.Wildcard));
                    if (pattern is Erlang.Pattern.Variable)
                    {
                        guardProjections.Add((source, field, register));
                        continue;
                    }

                    projections.Add((source, field, register, pattern));
                    if (pattern is Erlang.Pattern.Tuple nested && !ReadProjections(nested, register))
                    {
                        return false;
                    }
                }

                return true;
            }

            void EmitShape(Erlang.Pattern.Tuple current, int register)
            {
                if (HasTupleTag(current, out Atom tag))
                {
                    EmitDetailRecordTest(
                        register,
                        current.Items.Count,
                        tag,
                        skip
                    );
                }
                else
                {
                    EmitTupleShapeTest(
                        XRegisterTag,
                        register,
                        current.Items.Count,
                        skip
                    );
                }
            }

            bool CanEmitGuard(Expr condition) => condition is Expr.Binary
            { Operator: ErlangOperators.OrElse or ErlangOperators.AndAlso } composite
                ? CanEmitGuard(composite.Left) && CanEmitGuard(composite.Right)
                : CanEmitSeparateEntryGuard(guardClause, condition);

            void EmitGuard(Expr condition, int failure)
            {
                if (CanEmitSeparateEntryGuard(guardClause, condition))
                {
                    _ = EmitEntryGuardPrefix(guardClause, condition, failure);
                }
                else if (condition is Expr.Binary { Operator: ErlangOperators.AndAlso } conjunction)
                {
                    EmitGuard(conjunction.Left, failure);
                    EmitGuard(conjunction.Right, failure);
                }
                else if (condition is Expr.Binary { Operator: ErlangOperators.OrElse } alternative)
                {
                    // The admitted leaf tests are total, so only the boolean false edge reaches the right test.
                    int otherwise = Label();
                    int accepted = Label();
                    EmitGuard(alternative.Left, otherwise);
                    JumpTo(accepted);
                    Mark(otherwise);
                    EmitGuard(alternative.Right, failure);
                    Mark(accepted);
                }
            }
        }
    }
}
