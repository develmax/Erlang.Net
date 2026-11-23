using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitBoundedNameTableBitList(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || recursiveClosure is not null
                || owner.OnLoadFunction is not null || owner.IsDeclaredNative((function.Name, 4))
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [
                            Pattern.List { Items.Count: 0, Tail: null } or Pattern.Literal { Value: Nil },
                            Pattern.Variable ignoredTable,
                            Pattern.Variable ignoredIndex,
                            Pattern.Variable output
                        ],
                    Body: Expr.Call
                    {
                        Module: ListModuleNames.Module, Function: ListModuleNames.Reverse, DynamicRemote: false,
                        Arguments: [Expr.Variable reverse]
                    } finish
                },
                {
                    Guard: null,
                    Patterns: [
                            Pattern.List { Items: [Pattern.Variable selectedBit], Tail: Pattern.Variable selectedRest },
                            Pattern.List
                    {
                        Items: [Pattern.Tuple { Items: [Pattern.Variable name, Pattern.Variable numbered] }],
                        Tail: Pattern.Variable remainingNames
                    },
                            Pattern.Variable position,
                            Pattern.Variable accumulator
                        ],
                    Body: Expr.If
                    {
                        Clauses: [
                        {
                            Patterns.Count: 0,
                            Guard: Expr.Binary
                            {
                                Operator: ErlangOperators.ExactEqual, Left: Expr.Variable bit,
                                Right: Expr.Literal { Value: Integer zero }
                            },
                            Body: Expr.Call skipName
                        },
                        {
                            Patterns.Count: 0,
                            Guard: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } },
                            Body: Expr.Call includeName
                        }
                            ]
                    }
                },
                    var one,
                    var none
                ]
                || !DistinctBindings(
                    selectedBit,
                    selectedRest,
                    name,
                    position,
                    remainingNames,
                    accumulator
                )
                || !DistinctBindings(output) || output.Name == ignoredTable.Name || output.Name == ignoredIndex.Name
                || ignoredTable.Name == ignoredIndex.Name && ignoredTable.Name != VariableScopeNames.Wildcard
                || reverse.Name != output.Name || selectedBit.Name != bit.Name || !zero.Value.IsZero
                || numbered.Name != position.Name
                || !ReadStep(
                    skipName,
                    selectedRest,
                    remainingNames,
                    position,
                    accumulator,
                    out var skip,
                    out var omitted
                )
                || omitted is not null
                || !ReadStep(
                    includeName,
                    selectedRest,
                    remainingNames,
                    position,
                    accumulator,
                    out var add,
                    out var included
                )
                || included is not Expr.Variable label || label.Name != name.Name
                || !ReadFallback(
                    one,
                    bitValue: 1,
                    out var unnamed,
                    out var includeUnknown
                )
                || includeUnknown is not Expr.Tuple
                {
                    Items: [Expr.Literal { Value: Atom unknownTag }, Expr.Variable index]
                }
                || one.Patterns[2] is not Pattern.Variable unknownPosition || unknownPosition.Name != index.Name
                || !ReadFallback(
                    none,
                    bitValue: 0,
                    out var noName,
                    out var ignoredBit
                ) || ignoredBit is not null
                || !entries.TryGetValue((function.Name, 4), out int entry))
            {
                return false;
            }

            long maximum = -1;
            bool hasEntry = false;
            if (!HasOnlyLocalCalls(
                function,
                4,
                (caller, call) =>
            {
                if (ReferenceEquals(caller, function))
                {
                    return ReferenceEquals(call, skip) || ReferenceEquals(call, add)
                        || ReferenceEquals(call, unnamed) || ReferenceEquals(call, noName);
                }

                if (call.Arguments is not [_, var table, Expr.Literal { Value: Integer initial }, var seed]
                    || !initial.Value.IsZero || !AttributeLiteral.TryNormalizeConstant(seed, out Term empty)
                    || empty is not Nil || !ProvesNameTable(
                        caller,
                        call,
                        table,
                        []
                    ))
                {
                    return false;
                }

                hasEntry = true;

                return true;
            }
            ) || !hasEntry || maximum < 0)
            {
                return false;
            }

            int failed = owner.nextLabel - 2;
            int namedBit = Label();
            int unnamedBit = Label();
            int set = Label();
            int clear = Label();
            int done = Label();
            EmitList(
                0,
                4,
                5,
                done
            );
            EmitList(
                1,
                6,
                7,
                unnamedBit
            );
            owner.EmitTuplePart(6, 1, 8);
            owner.Op(ExactEqualTest, LabelTag, unnamedBit);
            owner.EmitTypedRegister(2, IntegerTypeBits, lower: 0);
            owner.EmitTypedRegister(
                8,
                IntegerTypeBits,
                lower: 0,
                upper: maximum
            );
            owner.Op(ExactEqualTest, LabelTag, namedBit);
            owner.Operand(XRegisterTag, 4);
            owner.LiteralOperand(zero);
            Increment(
                (Expr.Binary)skip.Arguments[2],
                8,
                2,
                9,
                maximum
            );
            ForwardNamed();

            Mark(namedBit);
            Increment(
                (Expr.Binary)add.Arguments[2],
                8,
                2,
                9,
                maximum
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 8);
            owner.EmitTuplePart(6, 0, 0);
            Prepend(0);
            ForwardNamed();

            Mark(unnamedBit);
            EmitAritySelect(4, 2, failed);
            owner.LiteralOperand(zero);
            owner.Operand(LabelTag, clear);
            owner.Operand(BeamWire.IntegerTag, 1);
            owner.Operand(LabelTag, set);
            Mark(set);
            Increment(
                (Expr.Binary)unnamed.Arguments[2],
                2,
                0,
                6
            );
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords + 3);
            owner.Operand(BeamWire.UnsignedTag, 6);
            owner.EmitTupleHeader(XRegisterTag, 2, 2);
            owner.LiteralOperand(unknownTag);
            owner.Operand(XRegisterTag, 2);
            Prepend(2);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            Forward();
            Mark(clear);
            Increment(
                (Expr.Binary)noName.Arguments[2],
                2,
                2,
                6
            );
            Forward();
            Mark(done);
            owner.EmitNilTest(XRegisterTag, 0, failed);
            owner.MoveToX0(XRegisterTag, 3);
            owner.EmitCallLine(finish, location);
            owner.Op(TailExternalCall, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ListModuleNames.Module, finish.Function, 1));

            return true;

            bool ProvesNameTable(
                FunctionDefinition caller,
                Expr.Call consumer,
                Expr value,
                HashSet<(string Name, int Arity, int Index)> active
            )
            {
                if (AttributeLiteral.TryNormalizeConstant(value, out Term term))
                {
                    while (term is Cons item)
                    {
                        if (item.Head is not TupleTerm { Items: [Atom, Integer ordinal] }
                            || ordinal.Value.Sign < 0 || ordinal.Value > long.MaxValue)
                        {
                            return false;
                        }

                        maximum = Math.Max(maximum, (long)ordinal.Value);
                        term = item.Tail;
                    }

                    return term is Nil;
                }

                if (value is not Expr.Variable forwarded || owner.IsDeclaredNative((caller.Name, caller.Arity)))
                {
                    return false;
                }

                var boundClause = caller.Clauses.FirstOrDefault(clause => IsTerminalTableCall(clause.Body, consumer));
                if (boundClause is null)
                {
                    return false;
                }

                int argument = Array.FindIndex(boundClause.Patterns.ToArray(),
                    pattern => pattern is Pattern.Variable variable && variable.Name == forwarded.Name);
                if (argument < 0 || !active.Add((caller.Name, caller.Arity, argument)))
                {
                    return false;
                }

                bool proven = HasOnlyLocalCalls(caller, caller.Arity, (source, call) =>
                    ProvesNameTable(
                        source,
                        call,
                        call.Arguments[argument],
                        active
                    ));
                active.Remove((caller.Name, caller.Arity, argument));

                return proven;
            }

            static bool IsTerminalTableCall(Expr expression, Expr.Call consumer)
            {
                if (ReferenceEquals(expression, consumer))
                {
                    return true;
                }

                return expression switch
                {
                    Expr.Block block => IsTerminalTableCall(block.Body, consumer),
                    Expr.Sequence { Items: [.., var last] } => IsTerminalTableCall(last, consumer),
                    Expr.Case choice => choice.Clauses.Any(clause => IsTerminalTableCall(clause.Body, consumer)),
                    _ => false
                };
            }

            bool ReadFallback(
                Clause branch,
                int bitValue,
                out Expr.Call call,
                out Expr? added
            )
            {
                call = null!;
                added = null;

                return branch is
                {
                    Guard: null,
                    Patterns: [
                        Pattern.List { Items: [Pattern.Literal { Value: Integer value }], Tail: Pattern.Variable tail },
                        Pattern.Variable table,
                        Pattern.Variable offset,
                        Pattern.Variable target
                    ]
                } && value.Value == bitValue && DistinctBindings(
                    tail,
                    table,
                    offset,
                    target
                )
                    && ReadStep(
                        branch.Body,
                        tail,
                        table,
                        offset,
                        target,
                        out call,
                        out added
                    );
            }

            bool ReadStep(
                Expr expression,
                Pattern.Variable tail,
                Pattern.Variable table,
                Pattern.Variable offset,
                Pattern.Variable target,
                out Expr.Call call,
                out Expr? added
            )
            {
                call = null!;
                added = null;
                if (expression is not Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [
                            Expr.Variable next, Expr.Variable remaining,
                            Expr.Binary
                        {
                            Operator: ErlangOperators.Plus, Left: Expr.Variable current,
                            Right: Expr.Literal { Value: Integer increment }
                        },
                            var outputExpr
                        ]
                    } recur || recur.Function != function.Name || !increment.Value.IsOne
                    || next.Name != tail.Name || remaining.Name != table.Name || current.Name != offset.Name)
                {
                    return false;
                }

                call = recur;
                if (outputExpr is Expr.Variable unchanged && unchanged.Name == target.Name)
                {
                    return true;
                }

                if (outputExpr is Expr.List { Items: [var extra], Tail: Expr.Variable carried }
                    && carried.Name == target.Name)
                {
                    added = extra;

                    return true;
                }

                return false;
            }

            void EmitList(
                int source,
                int head,
                int tail,
                int failure
            )
            {
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(XRegisterTag, source);
                owner.Op(GetList, XRegisterTag, source);
                owner.Operand(XRegisterTag, head);
                owner.Operand(XRegisterTag, tail);
            }

            void Increment(
                Expr.Binary arithmetic,
                int source,
                int output,
                int live,
                long? upper = null
            )
            {
                var previous = location;
                owner.EmitArithmeticLine(arithmetic.SourceLine, ref previous);
                owner.Op(GcBifTwo, LabelTag, 0);
                owner.Operand(BeamWire.UnsignedTag, live);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, arithmetic.Operator, 2));
                owner.EmitTypedRegister(
                    source,
                    IntegerTypeBits,
                    lower: 0,
                    upper: upper
                );
                owner.Operand(BeamWire.IntegerTag, 1);
                owner.Operand(XRegisterTag, output);
            }

            void ForwardNamed()
            {
                owner.MoveRegister(
                    XRegisterTag,
                    7,
                    XRegisterTag,
                    1
                );
                Forward();
            }

            void Forward()
            {
                owner.MoveToX0(XRegisterTag, 5);
                EmitTailLocal(entry, 4, null);
            }

            void Prepend(int source)
            {
                owner.Op(PutList, XRegisterTag, source);
                owner.Operand(XRegisterTag, 3);
                owner.Operand(XRegisterTag, 3);
            }
        }
    }
}
