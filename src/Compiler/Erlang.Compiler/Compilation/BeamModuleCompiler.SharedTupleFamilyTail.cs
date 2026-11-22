using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSharedTupleFamilyTail(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Arity != 3 || function.Clauses.Count < 4
                || function.Clauses[^2] is not
                {
                    Guard: null,
                    Patterns:
                    [
                        global::Erlang.Pattern.Tuple
                    {
                        Items:
                        [
                            global::Erlang.Pattern.Literal { Value: Atom generalTag },
                            global::Erlang.Pattern.Variable family, global::Erlang.Pattern.Variable member,
                            global::Erlang.Pattern.Variable payload
                        ]
                    },
                        global::Erlang.Pattern.Variable sender, global::Erlang.Pattern.Variable state
                    ],
                    Body: Expr.Sequence
                    {
                        Items:
                    [
                        Expr.Match
                    {
                        Pattern: global::Erlang.Pattern.Variable result,
                        Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: var actual } convert
                    },
                        Expr.Match
                    {
                        Pattern: global::Erlang.Pattern.Variable ignored,
                        Value: Expr.Call { Arguments: [Expr.Variable effectInput] } effect
                    },
                        Expr.Tuple { Items: [Expr.Literal { Value: Atom nextTag }, Expr.Variable nextState] }
                    ]
                    }
                }
                || !DistinctBindings(
                    family,
                    member,
                    payload,
                    sender,
                    state,
                    result
                ) || new[] { family.Name, member.Name, payload.Name, sender.Name, state.Name, result.Name }.Contains(ignored.Name) || !MatchArguments(
                    actual,
                    family,
                    member,
                    payload,
                    sender
                )
                || effectInput.Name != result.Name || nextState.Name != state.Name
                || !entries.TryGetValue((convert.Function, actual.Count), out int conversion)
                || !TryResolveOrdinaryCallTarget(effect, out string? effectModule)
                || function.Clauses[^1] is not
                {
                    Guard: null,
                    Patterns: [global::Erlang.Pattern.Variable message, global::Erlang.Pattern.Variable unused,
                        global::Erlang.Pattern.Variable fallbackState],
                    Body: Expr.Sequence
                    {
                        Items:
                    [
                        Expr.Call
                    {
                        Arguments: [var format, Expr.List
                        { Items: [Expr.Literal { Value: Atom category }, Expr.Variable input], Tail: null }]
                    } report,
                        Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom replyTag }, Expr.Literal { Value: Atom reply }, Expr.Variable saved]
                    }
                    ]
                    }
                }
                || !DistinctBindings(
                    message,
                    fallbackState
                ) || unused.Name == message.Name || unused.Name == fallbackState.Name
                || input.Name != message.Name || saved.Name != fallbackState.Name
                || !AttributeLiteral.TryNormalizeConstant(format, out Term template)
                || !TryResolveOrdinaryCallTarget(report, out string? reportModule))
            {
                return false;
            }

            var keys = new SortedSet<Atom>(Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true)));
            Atom? commonFamily = null;
            int tailEntry = 0;
            foreach (Clause clause in function.Clauses.SkipLast(2))
            {
                if (clause is not
                    {
                        Guard: null,
                        Patterns:
                        [
                            global::Erlang.Pattern.Tuple
                        {
                            Items:
                            [
                                global::Erlang.Pattern.Literal { Value: Atom tag },
                                global::Erlang.Pattern.Literal { Value: Atom familyName },
                                AliasPattern alias, global::Erlang.Pattern.Variable value
                            ]
                        },
                            global::Erlang.Pattern.Variable from, global::Erlang.Pattern.Variable carry
                        ],
                        Body: Expr.Call { Module: null, DynamicRemote: false } tail
                    }
                    || !tag.Equals(generalTag) || commonFamily is not null && !familyName.Equals(commonFamily)
                    || !TryReadPatternAlias(alias, out var binding, out var shape)
                    || shape is not global::Erlang.Pattern.Literal { Value: Atom key } || !keys.Add(key)
                    || !DistinctBindings(
                        binding,
                        value,
                        from,
                        carry
                    ) || !MatchArguments(
                        tail.Arguments,
                        binding,
                        value,
                        from,
                        carry
                    )
                    || !entries.TryGetValue((tail.Function, tail.Arguments.Count), out int target)
                    || tailEntry != 0 && tailEntry != target)
                {
                    return false;
                }

                commonFamily = familyName;
                tailEntry = target;
            }

            if (commonFamily is null || tailEntry == 0)
            {
                return false;
            }

            int sharedTail = Label();
            int general = Label();
            int fallback = Label();
            owner.Op(IsTaggedTuple, LabelTag, fallback);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.LiteralOperand(generalTag);
            owner.EmitTuplePart(0, 1, 3);
            owner.EmitTuplePart(0, 2, 4);
            owner.EmitTuplePart(0, 3, 0);
            owner.EmitAtomTest(3, commonFamily.Name, general);
            EmitAritySelect(4, keys.Count, general);
            foreach (Atom key in keys)
            {
                owner.LiteralOperand(key);
                owner.Operand(LabelTag, sharedTail);
            }

            Mark(sharedTail);
            MoveX(2, 3);
            MoveX(1, 2);
            MoveX(0, 1);
            MoveX(4, 0);
            EmitTailLocal(tailEntry, 4, frame: null);

            Mark(general);
            EmitFrameAllocation(5, 1);
            SaveState();
            MoveX(0, 2);
            MoveX(3, 0);
            MoveX(1, 3);
            MoveX(4, 1);
            owner.EmitCallLine(convert, location);
            owner.EmitFieldCall(4, conversion);
            owner.EmitCallLine(effect, CallLocation(convert, location));
            Call(effectModule, effect.Function, 1);
            EmitHeap(3);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(nextTag.Name)), (YRegisterTag, 0));
            owner.EmitFrameReturn(1);

            Mark(fallback);
            EmitFrameAllocation(3, 1, 2 * ConsHeapWords);
            SaveState();
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 0);
            owner.Op(PutList, AtomTag, owner.Intern(category.Name));
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.instructions.WriteByte(Move);
            owner.LiteralOperand(template);
            owner.Operand(XRegisterTag, 0);
            owner.EmitCallLine(report, location);
            Call(reportModule, report.Function, 2);
            EmitHeap(4);
            owner.EmitTagMatchTuple(
                0,
                (AtomTag, owner.Intern(replyTag.Name)),
                (AtomTag, owner.Intern(reply.Name)),
                (YRegisterTag, 0)
            );
            owner.EmitFrameReturn(1);

            return true;

            static bool MatchArguments(IReadOnlyList<Expr> arguments, params global::Erlang.Pattern.Variable[] bindings) =>
                arguments.Count == bindings.Length && arguments.Select((argument, index) =>
                    argument is Expr.Variable variable && variable.Name == bindings[index].Name).All(same => same);

            void MoveX(int source, int destination) => owner.MoveRegister(
                XRegisterTag,
                source,
                XRegisterTag,
                destination
            );

            void SaveState() => owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                0
            );

            void EmitHeap(int size)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, size);
                owner.Operand(BeamWire.UnsignedTag, 0);
            }
        }
    }
}
