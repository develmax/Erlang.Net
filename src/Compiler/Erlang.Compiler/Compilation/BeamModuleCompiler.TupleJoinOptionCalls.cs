using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record JoinedOption(
            Term? Literal,
            Atom? Key,
            int Register,
            bool FromJoin
        );

        private bool TryEmitTupleJoinOptionCalls(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || function.Clauses is not
                [
                {
                    Guard: null,
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match
                            {
                                Pattern: Pattern.Tuple { Items: [Pattern.Variable first, Pattern.Variable second] },
                                Value: Expr.If
                                {
                                    Clauses:
                                        [
                                        {
                                            Guard: Expr.Binary
                                            {
                                                Operator: ErlangOperators.ExactEqual or ErlangOperators.NumericEqual,
                                                Left: Expr.Variable tested, Right: Expr.Literal { Value: Atom key }
                                            },
                                            Body: Expr.Tuple { Items: [Expr.Call onMatch, Expr.Literal constant] }
                                        },
                                        {
                                            Guard: Expr.Literal { Value: Atom { Name: ErlangBooleanAtoms.True } },
                                            Body: Expr.Tuple { Items: [Expr.Call onMiss, Expr.Call nextMiss] }
                                        }
                                        ]
                                }
                            },
                                Expr.Match
                            {
                                Pattern: Pattern.Variable handle,
                                Value: Expr.Call { Arguments: [Expr.Variable receiver, Expr.List { Tail: null } options] } create
                            },
                                Expr.Case
                            {
                                Value: Expr.Call { Arguments: [Expr.Variable checkInput] } check,
                                Clauses:
                                    [
                                    {
                                        Guard: null, Patterns: [Pattern.Literal accepted],
                                        Body: var acceptedBody
                                    },
                                    {
                                        Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                        Body: Expr.Sequence { Items: [Expr.Call discard, Expr.Literal rejected] }
                                    }
                                    ]
                            }
                            ]
                    }
                } clause
                ]
                || !HasUnconditionalParameters(clause) || function.Arity < 3)
            {
                return false;
            }

            var parameters = clause.Patterns.Cast<Pattern.Variable>().ToArray();
            var names = parameters.Select(parameter => parameter.Name).ToArray();
            int selector = Array.IndexOf(names, tested.Name);
            if (selector <= 0 || names[0] != receiver.Name || checkInput.Name != handle.Name
                || !DistinctBindings([.. parameters, first, second, handle])
                || ReadEntryCall(onMatch, names) is not { } matchPlan
                || ReadPairJoinCall(onMiss, tested.Name) is not { Module: not null } miss
                || ReadPairJoinCall(nextMiss, tested.Name) is not { Module: not null } secondMiss
                || ReadPairJoinOptions(
                    options,
                    names,
                    first.Name,
                    second.Name,
                    selector
                ) is not { } items
                || !TryResolveOrdinaryCallTarget(create, out string? createModule) || createModule is null
                || ReadPairJoinCall(check, handle.Name) is not { Module: not null } probe
                || ReadPairJoinReply(acceptedBody, [.. names, first.Name, second.Name, handle.Name]) is not { } returned
                || ReadPairJoinCall(returned.Call, handle.Name) is not { Module: not null } success
                || ReadPairJoinCall(discard, handle.Name) is not { Module: not null } failure)
            {
                return false;
            }

            int frame = function.Arity;
            int selectorSlot = frame - 1 - selector;
            int alternative = Label();
            int joined = Label();
            int declined = Label();
            EmitFrameAllocation(frame, frame);
            for (int slot = 0; slot < frame; slot++)
            {
                owner.MoveRegister(
                    XRegisterTag,
                    frame - 1 - slot,
                    YRegisterTag,
                    slot
                );
            }
            owner.Op(ExactEqualTest, LabelTag, alternative);
            owner.Operand(XRegisterTag, selector);
            owner.LiteralOperand(key);
            owner.InitStackSlot(selectorSlot);
            EmitEntryCall(matchPlan);
            owner.EmitConstant(constant.Value, 1);
            JumpTo(joined);

            Mark(alternative);
            owner.MoveRegister(
                XRegisterTag,
                selector,
                XRegisterTag,
                0
            );
            EmitEntryCall(miss);
            owner.Op(Swap, YRegisterTag, selectorSlot);
            owner.Operand(XRegisterTag, 0);
            var previous = CallLocation(onMiss, location);
            EmitEntryCall(secondMiss, previous);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                YRegisterTag,
                selectorSlot,
                XRegisterTag,
                0
            );

            Mark(joined);
            EmitJoinedOptions(items);
            owner.MoveRegister(
                YRegisterTag,
                frame - 1,
                XRegisterTag,
                0
            );
            owner.EmitSlotKills(Enumerable.Range(0, frame).Where(slot => slot != selectorSlot).ToArray());
            owner.EmitCallLine(create, CallLocation(nextMiss, previous));
            Call(createModule, create.Function, 2);
            owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                frame - 1
            );
            previous = CallLocation(create, location);
            EmitEntryCall(probe, previous);

            owner.Op(ExactEqualTest, LabelTag, declined);
            owner.Operand(XRegisterTag, 0);
            owner.LiteralOperand(accepted.Value);
            EmitResultCall(success);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(returned.Tag);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(0);

            Mark(declined);
            EmitResultCall(failure);
            owner.EmitConstant(rejected.Value, 0);
            owner.EmitFrameReturn(0);

            return true;

            void EmitResultCall(EntryCall target)
            {
                owner.MoveRegister(
                    YRegisterTag,
                    frame - 1,
                    XRegisterTag,
                    0
                );
                owner.Op(TrimFrame, BeamWire.UnsignedTag, frame);
                owner.Operand(BeamWire.UnsignedTag, 0);
                EmitEntryCall(target, CallLocation(check, previous));
            }
        }

        private static (Atom Tag, Expr.Call Call)? ReadPairJoinReply(Expr expression, string[] bound)
        {
            if (expression is Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Call inline] })
            {
                return (tag, inline);
            }

            return expression is Expr.Sequence
            {
                Items:
                [
                    Expr.Match { Pattern: Pattern.Variable result, Value: Expr.Call call },
                    Expr.Tuple { Items: [Expr.Literal { Value: Atom reply }, Expr.Variable value] }
                ]
            } && result.Name == value.Name && result.Name != VariableScopeNames.Wildcard && !bound.Contains(result.Name)
                ? (reply, call)
                : null;
        }

        private EntryCall? ReadPairJoinCall(Expr.Call call, string input) =>
            call.Arguments is [Expr.Variable value] && value.Name == input ? ReadEntryCall(call, [input]) : null;

        private static JoinedOption[]? ReadPairJoinOptions(
            Expr.List list,
            string[] names,
            string first,
            string second,
            int selector
        )
        {
            var rows = new List<JoinedOption>(list.Items.Count);
            int firstUses = 0;
            int secondUses = 0;
            var retained = new HashSet<int> { 0, selector };
            foreach (Expr item in list.Items)
            {
                if (AttributeLiteral.TryNormalizeConstant(item, out var ground))
                {
                    rows.Add(new(
                        ground,
                        null,
                        0,
                        false
                    ));
                }
                else if (item is Expr.Tuple { Items: [Expr.Literal { Value: Atom tag }, Expr.Variable value] })
                {
                    if (value.Name == first)
                    {
                        firstUses++;
                        rows.Add(new(
                            null,
                            tag,
                            0,
                            true
                        ));
                    }
                    else if (value.Name == second)
                    {
                        secondUses++;
                        rows.Add(new(
                            null,
                            tag,
                            1,
                            true
                        ));
                    }
                    else if (Array.IndexOf(names, value.Name) is int input && input >= 0 && input != selector)
                    {
                        retained.Add(input);
                        rows.Add(new(
                            null,
                            tag,
                            names.Length - 1 - input,
                            false
                        ));
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }

            return firstUses == 1 && secondUses == 1 && retained.Count == names.Length ? rows.ToArray() : null;
        }

        private void EmitJoinedOptions(JoinedOption[] items)
        {
            int heap = items.Length * ConsHeapWords + items.Count(option => option.Key is not null) * 3;
            owner.Op(TestHeap, BeamWire.UnsignedTag, heap);
            owner.Operand(BeamWire.UnsignedTag, 2);
            bool[] occupied = [true, true, false, false];
            int tail = -1;
            for (int index = items.Length - 1; index >= 0; index--)
            {
                var option = items[index];
                int head = -1;
                if (option.Key is { } key)
                {
                    if (option.FromJoin)
                    {
                        occupied[option.Register] = false;
                    }
                    head = Array.FindIndex(occupied, used => !used);
                    owner.EmitTupleHeader(XRegisterTag, 2, head);
                    owner.LiteralOperand(key);
                    owner.Operand(option.FromJoin ? XRegisterTag : YRegisterTag, option.Register);
                }

                owner.instructions.WriteByte(PutList);
                if (option.Literal is { } literal)
                {
                    owner.LiteralOperand(literal);
                }
                else
                {
                    owner.Operand(XRegisterTag, head);
                }

                if (tail < 0)
                {
                    owner.LiteralOperand(Nil.Value);
                }
                else
                {
                    owner.Operand(XRegisterTag, tail);
                    occupied[tail] = false;
                }

                tail = index == 0 ? 1 : Array.FindIndex(occupied, used => !used);
                owner.Operand(XRegisterTag, tail);
                occupied[tail] = true;
            }
        }
    }
}
