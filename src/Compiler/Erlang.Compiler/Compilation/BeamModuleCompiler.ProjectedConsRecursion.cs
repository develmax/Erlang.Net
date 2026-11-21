using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitProjectedConsRecursion(FunctionDefinition function, int failure)
        {
            if (function.Clauses is not
                [
                { Patterns: [Erlang.Pattern.List { Items: [Erlang.Pattern.Tuple head], Tail: Erlang.Pattern.Variable tail }, Erlang.Pattern.Variable key] } first,
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.List { Items: [Erlang.Pattern.Variable ignored], Tail: Erlang.Pattern.Variable rest }, Erlang.Pattern.Variable nextKey],
                    Body: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable nextRest, Expr.Variable next] } recurse
                },
                    _
                ]
                || !DistinctBindings(tail, key)
                || ignored.Name != VariableScopeNames.Wildcard
                || !DistinctBindings(rest, nextKey)
                || rest.Name != nextRest.Name || nextKey.Name != next.Name
                || recurse.Function != function.Name
                || ReadEntryListReturn(function.Clauses) is not { Input: 0 } exhausted
                || !entries.TryGetValue((function.Name, 2), out int entry))
            {
                return false;
            }

            int nonCons = 0;
            var fields = head.Items.Select((pattern, index) => (pattern, index))
                .Where(field => field.pattern is not Erlang.Pattern.Variable { Name: VariableScopeNames.Wildcard })
                .ToArray();
            if (first is { Guard: null, Body: Expr.Variable result }
                && fields is [{ pattern: Erlang.Pattern.Variable fieldA }, { pattern: Erlang.Pattern.Variable fieldB }]
                && fieldA.Name != fieldB.Name
                && (fieldA.Name == key.Name && fieldB.Name == result.Name
                    || fieldB.Name == key.Name && fieldA.Name == result.Name)
                && result.Name != tail.Name)
            {
                int fallback = EmitPrefix();
                bool firstMatchesKey = fieldA.Name == key.Name;
                owner.EmitTuplePart(2, fields[firstMatchesKey ? 0 : 1].index, 3);
                owner.Op(ExactEqualTest, LabelTag, fallback);
                owner.Operand(XRegisterTag, 1);
                owner.Operand(XRegisterTag, 3);
                owner.EmitTuplePart(2, fields[firstMatchesKey ? 1 : 0].index, 0);
                owner.instructions.WriteByte(Return);
                EmitRest(fallback);

                return true;
            }

            if (fields is not [{ pattern: Erlang.Pattern.Tuple nested }]
                || nested.Items is not [Erlang.Pattern.Literal { Value: Atom tag }, Erlang.Pattern.Variable counter]
                || !DistinctBindings(counter, key, tail)
                || first is not
                {
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.GreaterOrEqual,
                        Left: Expr.Variable testedCounter, Right: Expr.Variable testedKey
                    },
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments:
                            [
                                Expr.Variable recurTail,
                                Expr.Binary
                            {
                                Operator: ErlangOperators.Plus,
                                Left: Expr.Variable incremented, Right: Expr.Literal { Value: Integer step }
                            } addition
                            ]
                    } increment
                }
                || increment.Function != function.Name || recurTail.Name != tail.Name
                || testedCounter.Name != counter.Name || incremented.Name != counter.Name || testedKey.Name != key.Name
                || exhausted is not { Result: 1, Literal: null }
                || step.Value <= 0 || ReadCounterEntryLowerBound(function) is not long lower)
            {
                return false;
            }

            int remaining = EmitPrefix();
            owner.EmitTuplePart(2, fields[0].index, 2);
            EmitDetailRecordTest(
                2,
                nested.Items.Count,
                tag,
                remaining
            );
            owner.EmitTuplePart(2, 1, 2);
            owner.Op(IsGreaterOrEqual, LabelTag, remaining);
            owner.Operand(XRegisterTag, 2);
            owner.EmitTypedRegister(1, NumberTypeBits, lower: lower);

            var previous = location;
            owner.EmitArithmeticLine(addition.SourceLine, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, addition.Operator, 2));
            owner.EmitTypedRegister(2, AnyTypeBits, lower: lower);
            owner.LiteralOperand(step);
            owner.Operand(XRegisterTag, 1);
            EmitTail();
            EmitRest(remaining);

            return true;

            int EmitPrefix()
            {
                int fallback = Label();
                nonCons = Label();
                owner.EmitNonemptyList(0, nonCons);
                owner.EmitListParts(
                    0,
                    XRegisterTag,
                    2,
                    XRegisterTag,
                    0
                );
                EmitTupleShapeTest(
                    XRegisterTag,
                    2,
                    head.Items.Count,
                    fallback
                );

                return fallback;
            }

            void EmitTail()
            {
                owner.Op(TailLocalCall, BeamWire.UnsignedTag, 2);
                owner.Operand(LabelTag, entry);
            }

            void EmitRest(int fallback)
            {
                Mark(fallback);
                EmitTail();
                EmitEmptyListReturn(
                    exhausted,
                    nonCons,
                    failure,
                    properList: false
                );
            }
        }

        private long? ReadCounterEntryLowerBound(FunctionDefinition function)
        {
            var signature = (function.Name, function.Arity);
            if (owner.exportedFunctions.Contains(signature) || owner.IsDeclaredNative(signature))
            {
                return null;
            }

            long? minimum = null;
            foreach (var other in localFunctions.Values)
            {
                foreach (Expr expression in LocalFunctionEntries.ClauseExpressions(other.Clauses).SelectMany(LocalFunctionEntries.Walk))
                {
                    if (expression is Expr.FunctionReference reference && (reference.Name, reference.Arity) == signature)
                    {
                        return null;
                    }

                    if (other != function
                        && expression is Expr.Call { Module: null, DynamicRemote: false } call
                        && (call.Function, call.Arguments.Count) == signature)
                    {
                        if (call.Arguments[1] is not Expr.Literal { Value: Integer initial }
                            || initial.Value < long.MinValue || initial.Value > long.MaxValue)
                        {
                            return null;
                        }

                        long value = (long)initial.Value;
                        minimum = Math.Min(minimum ?? value, value);
                    }
                }
            }

            return minimum;
        }

        private bool TryEmitSingletonListTupleChain(FunctionDefinition function, int failure)
        {
            if (function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [Erlang.Pattern.List { Items: [Erlang.Pattern.Variable head], Tail: null }],
                    Body: var body
                }]
                || head.Name == VariableScopeNames.Wildcard)
            {
                return false;
            }

            var constants = new Dictionary<string, Term>(StringComparer.Ordinal);
            if (body is Expr.Sequence sequence)
            {
                foreach (Expr expression in sequence.Items.Take(sequence.Items.Count - 1))
                {
                    if (expression is Expr.Literal)
                    {
                        continue;
                    }

                    if (expression is not Expr.Match { Pattern: Erlang.Pattern.Variable bind, Value: var value }
                        || bind.Name == VariableScopeNames.Wildcard || bind.Name == head.Name
                        || !ReadConstant(value, out Term constant) || !constants.TryAdd(bind.Name, constant))
                    {
                        return false;
                    }
                }

                body = sequence.Items[^1];
            }

            var tuples = new List<Term?[]>();
            while (body is Expr.Tuple tuple)
            {
                var operands = new Term?[tuple.Items.Count];
                Expr? dynamic = null;
                for (int index = 0; index < tuple.Items.Count; index++)
                {
                    Expr item = tuple.Items[index];
                    if (ReadConstant(item, out Term constant))
                    {
                        operands[index] = constant;
                    }
                    else if (dynamic is null)
                    {
                        dynamic = item;
                    }
                    else
                    {
                        return false;
                    }
                }

                if (dynamic is null)
                {
                    return false;
                }

                tuples.Add(operands);
                body = dynamic;
            }

            if (tuples.Count < 2 || body is not Expr.Variable result || result.Name != head.Name
                || !tuples.SelectMany(operands => operands).Any(operand => operand is TupleTerm))
            {
                return false;
            }

            owner.EmitNonemptyList(0, failure);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.EmitNilTest(XRegisterTag, 2, failure);
            owner.Op(TestHeap, BeamWire.UnsignedTag, tuples.Sum(operands => operands.Length + 1));
            owner.Operand(BeamWire.UnsignedTag, 2);
            int source = 1;
            for (int index = tuples.Count - 1; index >= 0; index--)
            {
                var operands = tuples[index];
                owner.Op(PutTuple, XRegisterTag, 0);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, operands.Length);
                foreach (Term? operand in operands)
                {
                    if (operand is null)
                    {
                        owner.Operand(XRegisterTag, source);
                    }
                    else
                    {
                        owner.LiteralOperand(operand);
                    }
                }

                source = 0;
            }

            owner.instructions.WriteByte(Return);

            return true;

            bool ReadConstant(Expr expression, out Term value)
            {
                return expression is Expr.Variable variable
                    ? constants.TryGetValue(variable.Name, out value!)
                    : AttributeLiteral.TryNormalizeConstant(expression, out value, constants);
            }
        }
    }
}
