using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitStringKeyTupleResultLoop(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled
                || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [var empty, Pattern.Variable emptyAcc, Pattern.Variable emptyValue, Pattern.Variable emptyFlag],
                    Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom resultTag }, Expr.Variable emptyFirst, Expr.Variable emptySecond] }
                },
                {
                    Guard: null,
                    Patterns:
                        [
                            Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail },
                            Pattern.Variable savedAcc,
                            Pattern.Variable savedValue,
                            Pattern.Variable savedFlag
                        ],
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match
                            {
                                Pattern: Pattern.Tuple { Items: [Pattern.Variable field, Pattern.Variable payload] },
                                Value: Expr.Call { Arguments: [Expr.Variable consumed, var secondArg] } produce
                            },
                                Expr.Case { Value: Expr.Variable selected, Clauses: [var guarded, var decoded, var generic] }
                            ]
                    }
                }
                ]
                || !IsEmptyListPattern(empty) || emptyFlag.Name != VariableScopeNames.Wildcard
                || !DistinctBindings(emptyAcc, emptyValue) || emptyFirst.Name != emptyAcc.Name || emptySecond.Name != emptyValue.Name
                || !DistinctBindings(
                    head,
                    tail,
                    savedAcc,
                    savedValue,
                    savedFlag,
                    field,
                    payload
                )
                || consumed.Name != head.Name || selected.Name != field.Name
                || !AttributeLiteral.TryNormalizeConstant(secondArg, out Term secondary)
                || !TryResolveOrdinaryCall(produce, out string? producerModule) || producerModule is null
                || !entries.TryGetValue((function.Name, 4), out int entry))
            {
                return false;
            }

            if (guarded is not
                {
                    Patterns: [Pattern.Literal { Value: Cons guardedKey }],
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.NumericEqual or ErlangOperators.ExactEqual,
                        Left: Expr.Variable checkedFlag,
                        Right: Expr.Literal { Value: Atom requiredFlag }
                    },
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable guardedTail, Expr.List guardedList, Expr.Literal { Value: Integer guardedCode }, Expr.Variable passedFlag]
                    } guardedRecur
                }
                || guardedRecur.Function != function.Name || guardedTail.Name != tail.Name
                || checkedFlag.Name != savedFlag.Name || passedFlag.Name != savedFlag.Name
                || !IsPairPrepend(guardedList)
                || ReadStringKey(guardedKey) is not { } guardKey
                || generic is not
                {
                    Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Call
                    {
                        Module: null, DynamicRemote: false,
                        Arguments: [Expr.Variable genericTail, Expr.List genericList, Expr.Variable genericValue, Expr.Variable genericFlag]
                    } genericRecur
                }
                || genericRecur.Function != function.Name || genericTail.Name != tail.Name
                || genericValue.Name != savedValue.Name || genericFlag.Name != savedFlag.Name
                || !IsPairPrepend(genericList))
            {
                return false;
            }

            if (decoded is not
                {
                    Guard: null, Patterns: [Pattern.Literal { Value: Cons decodedKey }],
                    Body: Expr.Sequence
                    {
                        Items:
                        [
                            Expr.Match
                        {
                            Pattern: Pattern.Variable result,
                            Value: Expr.Case
                            {
                                Value: Expr.Call { Arguments: [Expr.Variable tokenizeValue, var separator, var length] } tokenize,
                                Clauses:
                                    [
                                    {
                                        Guard: null,
                                        Patterns:
                                            [
                                                Pattern.Tuple
                                            {
                                                Items:
                                                    [
                                                        Pattern.Literal { Value: Atom success },
                                                        Pattern.List
                                                    {
                                                        Items: [Pattern.Variable token, Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                                                        Tail: null
                                                    }
                                                    ]
                                            }
                                            ],
                                        Body: Expr.Call { Arguments: [Expr.Variable convertValue] } convert
                                    },
                                    { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: var defaultValue }
                                    ]
                            }
                        },
                            Expr.Call
                        {
                            Module: null, DynamicRemote: false,
                            Arguments: [Expr.Variable decodedTail, Expr.Variable decodedAcc, Expr.Variable decodedValue, Expr.Literal { Value: Atom resetFlag }]
                        } decodedRecur
                        ]
                    }
                }
                || decodedRecur.Function != function.Name || decodedTail.Name != tail.Name
                || decodedAcc.Name != savedAcc.Name || decodedValue.Name != result.Name
                || tokenizeValue.Name != payload.Name || convertValue.Name != token.Name
                || !DistinctBindings(
                    head,
                    tail,
                    savedAcc,
                    savedValue,
                    savedFlag,
                    field,
                    payload,
                    result,
                    token
                )
                || ReadStringKey(decodedKey) is not { } decodeKey || guardKey.Head.Equals(decodeKey.Head)
                || !AttributeLiteral.TryNormalizeConstant(separator, out Term tokenSeparator)
                || !AttributeLiteral.TryNormalizeConstant(length, out Term tokenLength)
                || !AttributeLiteral.TryNormalizeConstant(defaultValue, out Term defaultResult)
                || !TryResolveOrdinaryCall(tokenize, out string? tokenizerModule) || tokenizerModule is null
                || ResolveStringKeyConverter(convert) is not { } converterModule)
            {
                return false;
            }

            int functionInfo = owner.nextLabel - 2;
            int decodedBranch = Label();
            int defaultBranch = Label();
            int decodedTailBranch = Label();
            int guardedBranch = Label();
            int genericBranch = Label();
            int terminalBranch = Label();
            int badMatch = Label();

            const int frame = 4;
            const int tailSlot = 0;
            const int flagSlot = 1;
            const int valueSlot = 2;
            const int accSlot = 3;
            owner.EmitNonemptyList(0, terminalBranch);
            EmitFrameAllocation(4, frame);
            owner.MoveRegister(
                XRegisterTag,
                3,
                YRegisterTag,
                flagSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                2,
                YRegisterTag,
                valueSlot
            );
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                accSlot
            );
            owner.EmitListParts(
                0,
                XRegisterTag,
                0,
                YRegisterTag,
                tailSlot
            );
            owner.EmitConstant(secondary, 1);
            owner.EmitCallLine(produce, location);
            Call(producerModule, produce.Function, 2);
            owner.EmitTupleCheck(0, 2, badMatch);
            owner.EmitTuplePart(0, 0, 1);
            owner.EmitNonemptyList(1, genericBranch);
            owner.EmitListParts(
                1,
                XRegisterTag,
                2,
                XRegisterTag,
                1
            );

            owner.Op(SelectValue, XRegisterTag, 2);
            owner.Operand(LabelTag, genericBranch);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            var keys = new[] { (guardKey.Head, guardedBranch), (decodeKey.Head, decodedBranch) };
            foreach (var (key, branch) in keys.OrderBy(pair => pair.Head.Value))
            {
                owner.LiteralOperand(key);
                owner.Operand(LabelTag, branch);
            }

            Mark(decodedBranch);
            TestExact(
                XRegisterTag,
                1,
                decodeKey.Tail,
                genericBranch
            );
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitOrderedTailMoves([], [(1, tokenSeparator), (2, tokenLength)]);
            owner.EmitSlotKills([flagSlot, valueSlot]);
            owner.EmitCallLine(tokenize, location);
            Call(tokenizerModule, tokenize.Function, 3);
            owner.Op(IsTaggedTuple, LabelTag, defaultBranch);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.LiteralOperand(success);
            owner.EmitTuplePart(0, 1, 0);
            owner.EmitNonemptyList(0, defaultBranch);
            owner.EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitNonemptyList(0, defaultBranch);
            owner.Op(GetTail, XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitNilTest(XRegisterTag, 0, defaultBranch);
            owner.MoveRegister(
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            owner.EmitCallLine(convert, CallLocation(tokenize, location));
            Call(converterModule, convert.Function, 1);
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                2
            );
            JumpTo(decodedTailBranch);

            Mark(defaultBranch);
            owner.EmitConstant(defaultResult, 2);
            Mark(decodedTailBranch);
            owner.MoveRegister(
                YRegisterTag,
                accSlot,
                XRegisterTag,
                1
            );
            owner.EmitConstant(resetFlag, 3);
            Recur();

            Mark(guardedBranch);
            TestExact(
                XRegisterTag,
                1,
                guardKey.Tail,
                genericBranch
            );
            TestExact(
                YRegisterTag,
                flagSlot,
                requiredFlag,
                genericBranch
            );
            Prepend();
            owner.EmitConstant(guardedCode, 2);
            owner.EmitConstant(requiredFlag, 3);
            Recur();

            Mark(genericBranch);
            Prepend();
            owner.MoveRegister(
                YRegisterTag,
                flagSlot,
                XRegisterTag,
                3
            );
            owner.MoveRegister(
                YRegisterTag,
                valueSlot,
                XRegisterTag,
                2
            );
            Recur();

            Mark(terminalBranch);
            owner.EmitNilTest(XRegisterTag, 0, functionInfo);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.EmitTupleHeader(XRegisterTag, 3);
            owner.LiteralOperand(resultTag);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 2);
            owner.instructions.WriteByte(Return);

            Mark(badMatch);
            owner.EmitLine(CallLocation(produce, location));
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            bool IsPairPrepend(Expr.List list) => list is
            {
                Items: [Expr.Tuple { Items: [Expr.Variable key, Expr.Variable value] }],
                Tail: Expr.Variable prior
            } && key.Name == field.Name && value.Name == payload.Name && prior.Name == savedAcc.Name;

            void Recur()
            {
                owner.MoveRegister(
                    YRegisterTag,
                    tailSlot,
                    XRegisterTag,
                    0
                );
                owner.Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, 4);
                owner.Operand(LabelTag, entry);
                owner.Operand(BeamWire.UnsignedTag, frame);
            }

            void Prepend()
            {
                owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
                owner.Operand(BeamWire.UnsignedTag, 1);
                owner.Op(PutList, XRegisterTag, 0);
                owner.Operand(YRegisterTag, accSlot);
                owner.Operand(XRegisterTag, 1);
            }

            void TestExact(
                int tag,
                int register,
                Term expected,
                int failure
            )
            {
                owner.Op(ExactEqualTest, LabelTag, failure);
                owner.Operand(tag, register);
                owner.LiteralOperand(expected);
            }
        }

        private string? ResolveStringKeyConverter(Expr.Call call)
        {
            if (call.Function == AutoImportNames.ListToInteger && owner.ResolvesErlangBif(call))
            {
                return ErlangModuleNames.Module;
            }

            return TryResolveOrdinaryCall(call, out string? module) ? module : null;
        }

        private static bool ReadProperStringTail(Term tail)
        {
            while (tail is Cons { Head: Integer } cell)
            {
                tail = cell.Tail;
            }

            return tail is Nil;
        }

        private static (Integer Head, Term Tail)? ReadStringKey(Cons key) =>
            key is { Head: Integer head, Tail: Cons tail } && ReadProperStringTail(tail)
                ? (head, tail) : null;
    }
}
