using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitExactListMapChildren(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns:
                        [
                            Pattern.List
                        {
                            Items: [Pattern.Variable first, Pattern.Variable second, Pattern.Variable third, Pattern.Variable fourth],
                            Tail: null
                        }
                        ],
                        Body: Expr.Sequence
                        {
                            Items:
                            [
                                Expr.Call
                            {
                                Arguments:
                                    [
                                        Expr.Variable ownerInput,
                                        Expr.Tuple { Items: [Expr.Literal { Value: Atom labelTag }, Expr.Variable fieldInput] }
                                    ]
                            } label,
                                Expr.Match { Pattern: Pattern.Variable selfBinding, Value: Expr.Call { Arguments.Count: 0 } self },
                                Expr.Match { Pattern: Pattern.Variable flagsBinding, Value: var flags },
                                Expr.Match { Pattern: Pattern.Variable childrenBinding, Value: Expr.List { Tail: null, Items: var children } },
                                Expr.Tuple
                            {
                                Items:
                                    [
                                        Expr.Literal { Value: Atom resultTag },
                                        Expr.Tuple { Items: [Expr.Variable flagsResult, Expr.Variable childrenResult] }
                                    ]
                            }
                            ]
                        }
                    }
                ]
                || !DistinctBindings(
                    first,
                    second,
                    third,
                    fourth,
                    selfBinding,
                    flagsBinding,
                    childrenBinding
                )
                || ownerInput.Name != first.Name || fieldInput.Name != third.Name
                || flagsBinding.Name != flagsResult.Name || childrenBinding.Name != childrenResult.Name
                || !IsReceiverSelf(self)
                || !TryResolveOrdinaryCall(label, out string? labelModule, preparedArgument: 1) || labelModule is null
                || !AttributeLiteral.TryNormalizeConstant(flags, out Term flagValue)
                || children.Count < 3
                || ReadMapChildDescriptor(children[0]) is not { Arguments: [Expr.Variable a, Expr.Variable b, Expr.Variable c, Expr.Call store] } worker
                || ReadMapChildDescriptor(children[1]) is not { Arguments: [Expr.Variable forwarded] } manager
                || a.Name != first.Name || b.Name != second.Name || c.Name != third.Name || forwarded.Name != fourth.Name
                || !AttributeLiteral.TryNormalizeConstant(new Expr.List(children.Skip(2).ToArray()), out Term constantChildren))
            {
                return false;
            }

            if (store.Arguments is not
                [
                    var storeKey,
                    Expr.List
                {
                    Tail: null,
                    Items: [Expr.Tuple { Items: [Expr.Literal { Value: Atom selfTag }, Expr.Variable storedSelf] }]
                },
                    Expr.Variable options,
                    var provenance,
                    var sourceLine
                ]
                || storedSelf.Name != selfBinding.Name || options.Name != fourth.Name
                || !TryResolveOrdinaryCall(store, out string? storeModule, preparedArgument: 1) || storeModule is null
                || !AttributeLiteral.TryNormalizeConstant(storeKey, out Term optionKey)
                || !AttributeLiteral.TryNormalizeConstant(provenance, out Term ownerValue)
                || !AttributeLiteral.TryNormalizeConstant(sourceLine, out Term locationValue))
            {
                return false;
            }

            const int entryFrame = 5;
            const int resultFrame = entryFrame - 1;
            const int listTail = 2;
            const int finalTail = 6;
            int failure = owner.nextLabel - 2;
            int input = 0;
            for (int item = 0; item < 4; item++)
            {
                owner.EmitNonemptyList(input, failure);
                owner.EmitListParts(
                    input,
                    XRegisterTag,
                    item == 0 ? 1 : item + 2,
                    XRegisterTag,
                    item == 3 ? finalTail : listTail
                );
                input = listTail;
            }

            owner.EmitNilTest(XRegisterTag, finalTail, failure);
            EmitFrameAllocation(6, entryFrame, heapWords: 3);
            owner.MoveRegister(
                XRegisterTag,
                5,
                YRegisterTag,
                0
            );
            owner.MoveRegister(
                XRegisterTag,
                4,
                YRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                2
            );
            owner.MoveRegister(
                XRegisterTag,
                3,
                YRegisterTag,
                3
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                4
            );
            owner.EmitTagMatchTuple(1, (AtomTag, owner.Intern(labelTag.Name)), (XRegisterTag, 4));
            owner.MoveRegister(
                YRegisterTag,
                4,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(label, location);
            Call(labelModule, label.Function, 2);

            Heap(3 + ConsHeapWords, 0);
            EmitReceiverSelf(0);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(selfTag.Name)), (XRegisterTag, 0));
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 1);
            owner.EmitConstant(ownerValue, 3);
            owner.MoveRegister(
                YRegisterTag,
                0,
                XRegisterTag,
                2
            );
            owner.EmitConstant(locationValue, 4);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, resultFrame);
            owner.EmitConstant(optionKey, 0);
            owner.EmitCallLine(store, CallLocation(label, location));
            Call(storeModule, store.Function, 5);

            Heap(4 * ConsHeapWords + 4, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.LiteralOperand(Nil.Value);
            owner.Operand(XRegisterTag, 0);
            foreach (int slot in new[] { 0, 2, 3 })
            {
                owner.Op(PutList, YRegisterTag, slot);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(XRegisterTag, 0);
            }
            owner.EmitTagMatchTuple(
                0,
                (AtomTag, owner.Intern(worker.Module.Name)),
                (AtomTag, owner.Intern(worker.Member.Name)),
                (XRegisterTag, 0)
            );
            EmitDescriptor(worker, 0);
            Heap(4, 1);
            owner.EmitTagMatchTuple(
                1,
                (AtomTag, owner.Intern(manager.Module.Name)),
                (AtomTag, owner.Intern(manager.Member.Name)),
                (YRegisterTag, 1)
            );
            EmitDescriptor(manager, 1);

            Heap(2 * ConsHeapWords + 6, 2);
            owner.Op(PutList, XRegisterTag, 1);
            owner.LiteralOperand(constantChildren);
            owner.Operand(XRegisterTag, 1);
            owner.Op(PutList, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.LiteralOperand(flagValue);
            owner.Operand(XRegisterTag, 0);
            owner.EmitTagMatchTuple(0, (AtomTag, owner.Intern(resultTag.Name)), (XRegisterTag, 0));
            owner.EmitFrameReturn(resultFrame);

            return true;

            void Heap(int words, int live)
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, words);
                owner.Operand(BeamWire.UnsignedTag, live);
            }

            void EmitDescriptor(MapChildDescriptor spec, int register)
            {
                owner.Op(PutMapAssoc, LabelTag, 0);
                owner.LiteralOperand(new MapTerm([]));
                owner.Operand(XRegisterTag, register);
                owner.Operand(BeamWire.UnsignedTag, register + 1);
                owner.instructions.WriteByte(ExtendedListOperand);
                owner.Operand(BeamWire.UnsignedTag, 2 * spec.Fields.Count);
                foreach (var (key, value) in spec.Fields)
                {
                    owner.LiteralOperand(key);
                    if (value is null)
                    {
                        owner.Operand(XRegisterTag, register);
                    }
                    else
                    {
                        owner.LiteralOperand(value);
                    }
                }
            }
        }

        private static MapChildDescriptor? ReadMapChildDescriptor(Expr source)
        {
            if (source is not Expr.Map { Base: null } map || map.Fields.Any(field => field.Exact))
            {
                return null;
            }

            var fields = new Dictionary<Atom, Term?>();
            (Atom Module, Atom Member, IReadOnlyList<Expr> Arguments)? dynamic = null;
            foreach (var field in map.Fields)
            {
                if (field.Key is not Expr.Literal { Value: Atom key } || fields.ContainsKey(key))
                {
                    return null;
                }
                if (field.Value is Expr.Tuple
                    {
                        Items:
                        [
                            Expr.Literal { Value: Atom module },
                            Expr.Literal { Value: Atom member },
                            Expr.List { Tail: null, Items: var args }
                        ]
                    } && dynamic is null && args.Count != 0 && args.Any(arg => arg is not Expr.Literal))
                {
                    dynamic = (module, member, args);
                    fields.Add(key, null);
                }
                else if (AttributeLiteral.TryNormalizeConstant(field.Value, out Term value))
                {
                    fields.Add(key, value);
                }
                else
                {
                    return null;
                }
            }

            return dynamic is { } call
                ? new(
                    call.Module,
                    call.Member,
                    call.Arguments,
                    fields.OrderBy(pair => pair.Key, Comparer<Atom>.Create((left, right) => TermOrder.Compare(left, right, exact: true))).ToArray()
                )
                : null;
        }

        private sealed record MapChildDescriptor(
            Atom Module,
            Atom Member,
            IReadOnlyList<Expr> Arguments,
            IReadOnlyList<KeyValuePair<Atom, Term?>> Fields
        );
    }
}
