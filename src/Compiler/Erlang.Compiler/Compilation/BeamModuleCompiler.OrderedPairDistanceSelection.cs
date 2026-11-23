using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitOrderedPairDistanceSelection(FunctionDefinition function, int functionInfo)
        {
            var signature = (function.Name, function.Arity);
            BeamModuleCompiler facts = owner.layoutProbeSource ?? owner;
            if (!CanUseEntryTypeFacts() || recursiveClosure is not null || function.Arity != 5
                || function.Clauses is not [var bound, var search, var empty]
                || !MatchesDistanceSelection(
                    bound,
                    search,
                    empty,
                    function.Name
                ) || !entries.TryGetValue(signature, out int entry)
                || !facts.HasTuplePairEntryFact(signature, 4) || !facts.HasNumericEntryFact(signature, 2))
            {
                return false;
            }

            int farther = Label();
            int distance = Label();
            int nonCons = Label();
            int previous = Label();

            owner.EmitNonemptyList(3, nonCons);
            owner.EmitListParts(
                3,
                XRegisterTag,
                5,
                XRegisterTag,
                6
            );
            EmitTupleShapeTest(
                XRegisterTag,
                5,
                2,
                functionInfo,
                0
            );
            owner.EmitTuplePart(5, 0, 3);

            // Erlang orders all numbers before non-numeric terms. Reaching
            // the next block proves that the checked head key is numeric.
            owner.Op(IsGreaterOrEqual, LabelTag, previous);
            owner.EmitTypedRegister(2, NumberTypeBits);
            owner.Operand(XRegisterTag, 3);
            owner.Op(NumericEqualTest, LabelTag, farther);
            owner.Operand(XRegisterTag, 1);
            owner.EmitTypedRegister(3, NumberTypeBits);
            ReturnCurrentPair();

            Mark(farther);
            owner.Op(IsLess, LabelTag, distance);
            owner.EmitTypedRegister(3, NumberTypeBits);
            owner.Operand(XRegisterTag, 1);
            owner.MoveRegister(
                XRegisterTag,
                5,
                XRegisterTag,
                4
            );
            owner.MoveRegister(
                XRegisterTag,
                6,
                XRegisterTag,
                3
            );
            owner.Op(TailLocalCall, BeamWire.UnsignedTag, 5);
            owner.Operand(LabelTag, entry);

            // Both failed comparisons imply N < K. Guard arithmetic failure
            // still selects the prior pair, including nonnumeric prior keys.
            Mark(distance);
            int subtract = owner.Import(ErlangModuleNames.Module, ErlangOperators.Minus, 2);
            owner.Op(GcBifTwo, LabelTag, previous);
            owner.Operand(BeamWire.UnsignedTag, 6);
            owner.Operand(BeamWire.UnsignedTag, subtract);
            owner.EmitTypedRegister(3, NumberTypeBits);
            owner.EmitTypedRegister(1, NumberTypeBits);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTuplePart(4, 0, 2);
            owner.Op(GcBifTwo, LabelTag, previous);
            owner.Operand(BeamWire.UnsignedTag, 6);
            owner.Operand(BeamWire.UnsignedTag, subtract);
            owner.EmitTypedRegister(1, NumberTypeBits);
            owner.Operand(XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            owner.Op(IsLess, LabelTag, previous);
            owner.EmitTypedRegister(0, NumberTypeBits);
            owner.EmitTypedRegister(1, NumberTypeBits);
            ReturnCurrentPair();

            Mark(nonCons);
            owner.EmitNilTest(XRegisterTag, 3, functionInfo);
            Mark(previous);
            owner.MoveRegister(
                XRegisterTag,
                4,
                XRegisterTag,
                0
            );
            owner.instructions.WriteByte(Return);

            return true;

            void ReturnCurrentPair()
            {
                owner.MoveRegister(
                    XRegisterTag,
                    5,
                    XRegisterTag,
                    0
                );
                owner.instructions.WriteByte(Return);
            }
        }

        private static bool MatchesDistanceSelection(
            Clause bound,
            Clause search,
            Clause empty,
            string self
        )
        {
            if (bound is not
                {
                    Patterns: [
                        global::Erlang.Pattern.Variable,
                        global::Erlang.Pattern.Variable,
                        global::Erlang.Pattern.Variable max,
                        global::Erlang.Pattern.List
                    {
                        Items: [global::Erlang.Pattern.Tuple { Items: [global::Erlang.Pattern.Variable boundKey, global::Erlang.Pattern.Variable] }],
                        Tail: global::Erlang.Pattern.Variable
                    },
                        global::Erlang.Pattern.Variable boundPair
                    ],
                    Guard: { } above,
                    Body: Expr.Variable rejected
                }
                || !IsDistanceComparison(
                    above,
                    ErlangOperators.Greater,
                    boundKey.Name,
                    max.Name
                )
                || rejected.Name != boundPair.Name || !HasIndependentDistanceBindings(bound)
                || search is not
                {
                    Patterns: [
                        global::Erlang.Pattern.Variable min,
                        global::Erlang.Pattern.Variable wanted,
                        global::Erlang.Pattern.Variable maximum,
                        global::Erlang.Pattern.List
                    {
                        Items: [var currentPairPattern],
                        Tail: global::Erlang.Pattern.Variable tail
                    },
                        var previousPairPattern
                    ],
                    Guard: null,
                    Body: Expr.If { Clauses: [var equal, var lower, var closer, var higher] }
                }
                || ReadDistancePairPattern(currentPairPattern) is not { } currentPair
                || ReadDistancePairPattern(previousPairPattern) is not { } previousPair
                || !HasIndependentDistanceBindings(search)
                || !DistinctBindings(
                    min,
                    wanted,
                    maximum,
                    tail
                )
                || equal.Guard is not { } equals || !IsDistanceComparison(
                    equals,
                    ErlangOperators.NumericEqual,
                    wanted.Name,
                    currentPair.Key
                )
                || !IsDistancePair(equal.Body, currentPair)
                || lower.Guard is not { } below || !IsDistanceComparison(
                    below,
                    ErlangOperators.Greater,
                    wanted.Name,
                    currentPair.Key
                )
                || lower.Body is not Expr.Call
                {
                    Module: null, DynamicRemote: false,
                    Arguments: [Expr.Variable a, Expr.Variable b, Expr.Variable c, Expr.Variable rest, var next]
                } recur
                || recur.Function != self || a.Name != min.Name || b.Name != wanted.Name || c.Name != maximum.Name
                || rest.Name != tail.Name || !IsDistancePair(next, currentPair)
                || closer.Guard is not Expr.Binary
                {
                    Operator: ErlangOperators.AndAlso,
                    Left: var belowCurrent,
                    Right: Expr.Binary { Operator: ErlangOperators.Less, Left: var left, Right: var right }
                }
                || !IsDistanceComparison(
                    belowCurrent,
                    ErlangOperators.Less,
                    wanted.Name,
                    currentPair.Key
                )
                || !IsDistanceComparison(
                    left,
                    ErlangOperators.Minus,
                    currentPair.Key,
                    wanted.Name
                )
                || !IsDistanceComparison(
                    right,
                    ErlangOperators.Minus,
                    wanted.Name,
                    previousPair.Key
                )
                || !IsDistancePair(closer.Body, currentPair)
                || higher.Guard is not { } higherGuard || !IsDistanceComparison(
                    higherGuard,
                    ErlangOperators.Less,
                    wanted.Name,
                    currentPair.Key
                )
                || !IsDistancePair(higher.Body, previousPair))
            {
                return false;
            }

            return empty is
            {
                Guard: null,
                Patterns: [
                        global::Erlang.Pattern.Variable,
                        global::Erlang.Pattern.Variable,
                        global::Erlang.Pattern.Variable,
                        global::Erlang.Pattern.List { Items: [], Tail: null } or global::Erlang.Pattern.Literal { Value: Nil },
                        global::Erlang.Pattern.Variable emptyPair
                    ],
                Body: Expr.Variable result
            } && result.Name == emptyPair.Name && HasIndependentDistanceBindings(empty);
        }

        private static bool HasIndependentDistanceBindings(Clause clause)
        {
            var bindings = clause.Patterns.SelectMany(Semantics.Variables)
                .Where(name => name != VariableScopeNames.Wildcard).ToArray();

            return bindings.Distinct(StringComparer.Ordinal).Count() == bindings.Length;
        }

        private static bool IsDistanceComparison(
            Expr expression,
            string operation,
            string left,
            string right
        ) =>
            expression is Expr.Binary { Left: Expr.Variable lhs, Right: Expr.Variable rhs } test
            && test.Operator == operation && lhs.Name == left && rhs.Name == right;

        private static (string Key, string Value, string? Alias)? ReadDistancePairPattern(global::Erlang.Pattern pattern)
        {
            string? alias = null;
            if (pattern is AliasPattern matched)
            {
                if (!TryReadPatternAlias(matched, out var name, out var shape)
                    || name.Name == VariableScopeNames.Wildcard)
                {
                    return null;
                }

                pattern = shape;
                alias = name.Name;
            }

            return pattern is global::Erlang.Pattern.Tuple
            {
                Items: [
                        global::Erlang.Pattern.Variable { Name: not VariableScopeNames.Wildcard } key,
                        global::Erlang.Pattern.Variable { Name: not VariableScopeNames.Wildcard } value
                    ]
            } ? (key.Name, value.Name, alias) : null;
        }

        private static bool IsDistancePair(Expr expression, (string Key, string Value, string? Alias) pair) =>
            expression is Expr.Variable alias && alias.Name == pair.Alias
            || expression is Expr.Tuple { Items: [Expr.Variable first, Expr.Variable second] }
                && first.Name == pair.Key && second.Name == pair.Value;
    }
}
