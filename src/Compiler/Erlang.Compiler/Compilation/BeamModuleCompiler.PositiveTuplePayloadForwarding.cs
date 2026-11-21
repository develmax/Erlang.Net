using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const string ListMaximumFunction = "max";

        private bool TryEmitPositiveTuplePayloadForwarding(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || owner.OnLoadFunction is not null || owner.IsDeclaredNative((function.Name, 3))
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [
                            Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable next },
                            Pattern.Variable option,
                            Pattern.Variable tag
                        ],
                        Body: Expr.Sequence
                        {
                            Items: [
                                Expr.Match
                            {
                                Pattern: Pattern.Variable collected,
                                Value: Expr.Call
                                {
                                    Module: null, DynamicRemote: false,
                                    Arguments: [
                                            Expr.List { Items: [Expr.Variable first], Tail: Expr.Variable tail },
                                            Expr.Variable positions,
                                            var initial
                                        ]
                                } collect
                            },
                                Expr.Match
                            {
                                Pattern: Pattern.Variable size,
                                Value: Expr.Binary
                                {
                                    Operator: ErlangOperators.Plus,
                                    Left: Expr.Call
                                    {
                                        Module: ListModuleNames.Module, Function: ListMaximumFunction,
                                        DynamicRemote: false, Arguments: [Expr.Variable selected]
                                    } maximum,
                                    Right: Expr.Literal { Value: Integer extra }
                                } measured
                            },
                                Expr.Match
                            {
                                Pattern: Pattern.Variable arranged,
                                Value: Expr.Call
                                {
                                    Module: null, DynamicRemote: false,
                                    Arguments: [
                                            Expr.Variable required, Expr.Variable allPositions,
                                            Expr.Literal { Value: Integer counter }
                                        ]
                                } arrange
                            },
                                Expr.Match
                            {
                                Pattern: Pattern.Tuple
                                {
                                    Items: [Pattern.Variable length, Pattern.Variable item, Pattern.Variable payload]
                                },
                                Value: Expr.Call
                                {
                                    Module: null, DynamicRemote: false, Arguments: [Expr.Variable ordered]
                                } encode
                            },
                                Expr.Call
                            {
                                Module: null, DynamicRemote: false,
                                Arguments: [
                                        Expr.Variable tags, Expr.List { Items: [Expr.Variable leading], Tail: Expr.Variable bytes },
                                        Expr.Binary
                                {
                                    Operator: ErlangOperators.Plus, Left: Expr.Variable count,
                                    Right: Expr.Literal { Value: Integer overhead }
                                } total
                                    ]
                            } finish
                            ]
                        }
                    }
                ]
                || !DistinctBindings(
                    head,
                    next,
                    option,
                    tag,
                    collected,
                    size,
                    arranged,
                    length,
                    item,
                    payload
                )
                || first.Name != head.Name || tail.Name != next.Name || positions.Name != option.Name
                || selected.Name != collected.Name || allPositions.Name != collected.Name || required.Name != size.Name
                || ordered.Name != arranged.Name || tags.Name != tag.Name || count.Name != length.Name
                || leading.Name != item.Name || bytes.Name != payload.Name || !extra.Value.IsOne || !counter.Value.IsZero
                || !AttributeLiteral.TryNormalizeConstant(initial, out Term seed) || seed is not Nil
                || !entries.TryGetValue((collect.Function, 3), out int collectEntry)
                || !entries.TryGetValue((arrange.Function, 3), out int arrangeEntry)
                || !entries.TryGetValue((encode.Function, 1), out int encodeEntry)
                || !entries.TryGetValue((finish.Function, 3), out int finishEntry)
                || !HasOnlyLocalCalls(
                    function,
                    3,
                    (caller, call) =>
                    call.Arguments[0] is Expr.Variable original && caller.Clauses.Any(clause =>
                        clause.Patterns is [AliasPattern alias, ..]
                        && TryReadPatternAlias(alias, out var binding, out var shape)
                        && binding.Name == original.Name && shape is Pattern.List { Items.Count: > 0 }
                        && ReferenceEquals(clause.Body, call))
                )
                || !ProvesPositiveTupleFirst(encode, new HashSet<string>(StringComparer.Ordinal), []))
            {
                return false;
            }

            EmitFrameAllocation(3, 2);
            owner.InitStackSlot(0);
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                1
            );
            owner.EmitConstant(Nil.Value, 2);
            var previous = location;
            CallLocal(collect, collectEntry);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(maximum, previous);
            previous = CallLocation(maximum, previous);
            Call(maximum.Module, maximum.Function, 1);
            owner.EmitArithmeticLine(measured.SourceLine, ref previous);
            owner.GcArithmetic(
                GcBifTwo,
                ErlangOperators.Plus,
                1,
                0,
                (XRegisterTag, 0, 0),
                (BeamWire.IntegerTag, 1, 0)
            );
            owner.EmitConstant(counter, 2);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            CallLocal(arrange, arrangeEntry);
            CallLocal(encode, encodeEntry);

            owner.EmitTuplePart(0, 0, 1);
            owner.EmitArithmeticLine(total.SourceLine, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, total.Operator, 2));
            owner.EmitTypedRegister(1, IntegerTypeBits, lower: 1);
            owner.LiteralOperand(overhead);
            owner.Operand(XRegisterTag, 1);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.EmitTuplePart(0, 1, 2);
            owner.EmitTuplePart(0, 2, 0);
            owner.Op(PutList, XRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                2
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.MoveToX0(YRegisterTag, 0);
            EmitTailLocal(finishEntry, 3, 1);

            return true;

            void CallLocal(Expr.Call call, int entry)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.EmitFieldCall(call.Arguments.Count, entry);
            }
        }

        private bool ProvesPositiveTupleFirst(
            Expr expression,
            IReadOnlySet<string> positives,
            HashSet<(string Name, string Arguments)> visiting
        )
        {
            if (expression is Expr.Sequence { Items: [.., var last] })
            {
                return ProvesPositiveTupleFirst(last, positives, visiting);
            }

            if (expression is Expr.Tuple { Items: [var length, _, _] })
            {
                return IsPositiveInteger(length, positives);
            }

            if (expression is not Expr.Call { Module: null, DynamicRemote: false } call)
            {
                return false;
            }

            var signature = (call.Function, call.Arguments.Count);
            if (owner.IsDeclaredNative(signature) || !localFunctions.TryGetValue(signature, out var target))
            {
                return false;
            }

            bool[] arguments = call.Arguments.Select(argument => IsPositiveInteger(argument, positives)).ToArray();
            var key = (call.Function, new string(arguments.Select(proven => proven ? '+' : '?').ToArray()));
            if (!visiting.Add(key))
            {
                return true;
            }

            bool valid = target.Clauses.All(clause =>
            {
                var known = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 0; index < arguments.Length; index++)
                {
                    if (arguments[index] && clause.Patterns[index] is Pattern.Variable variable)
                    {
                        known.Add(variable.Name);
                    }
                }

                return ProvesPositiveTupleFirst(clause.Body, known, visiting);
            });
            visiting.Remove(key);

            return valid;
        }

        private static bool IsPositiveInteger(Expr expression, IReadOnlySet<string> positives) => expression switch
        {
            Expr.Literal { Value: Integer value } => value.Value.Sign > 0,
            Expr.Variable variable => positives.Contains(variable.Name),
            Expr.Binary
            {
                Operator: ErlangOperators.Plus,
                Left: var value,
                Right: Expr.Literal { Value: Integer added }
            } => added.Value.Sign >= 0 && IsPositiveInteger(value, positives),
            _ => false
        };
    }
}
