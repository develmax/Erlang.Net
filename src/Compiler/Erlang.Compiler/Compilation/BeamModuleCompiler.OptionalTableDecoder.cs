using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record TableOptionalDecode(
            Pattern.Variable Payload,
            Pattern.Variable Remaining,
            Expr.Call Decode,
            Term Tags,
            Atom Absent
        );

        private sealed record TableDecodeCatch(
            Expr.Apply Apply,
            Atom Field,
            Term Options,
            Atom ErrorTag,
            Expr.Call Exit
        );

        private sealed record TableUnexpectedFields(
            Atom ErrorTag,
            Atom DetailsTag,
            Atom UnexpectedTag,
            Expr.Call Exit
        );

        private bool TryEmitOptionalTableDecoder(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled
                || function.Clauses is not
                [
                    {
                        Guard: null,
                        Patterns: [Pattern.Variable input, Pattern.Variable tags],
                        Body: Expr.Sequence
                        {
                            Items:
                                [
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable decoded,
                                    Value: Expr.Call
                                    {
                                        Module: null, DynamicRemote: false, Arguments: [Expr.Variable source, Expr.Variable options]
                                    } matchTags
                                },
                                    Expr.Match
                                {
                                    Pattern: Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable tail },
                                    Value: Expr.Variable components
                                } split,
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable required,
                                    Value: Expr.Call
                                    {
                                        Module: null, DynamicRemote: false, Arguments: [Expr.Variable field, var requiredTags]
                                    } readRequired
                                },
                                    Expr.Match
                                {
                                    Pattern: Pattern.Tuple { Items: [Pattern.Variable optional, Pattern.Variable rest] },
                                    Value: Expr.Case open
                                },
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable callback,
                                    Value: Expr.Call
                                    {
                                        Module: not null, DynamicRemote: false, Arguments: [Expr.Variable key]
                                    } select
                                },
                                    Expr.Match { Pattern: Pattern.Variable fieldValue, Value: Expr.Case choose },
                                    Expr.Case remaining,
                                    Expr.Match
                                {
                                    Pattern: Pattern.Variable result,
                                    Value: Expr.Tuple
                                    {
                                        Items: [Expr.Literal { Value: Atom tag }, Expr.Variable first, Expr.Variable second]
                                    }
                                },
                                    Expr.Variable returned
                                ]
                        }
                    }
                ]
                || !DistinctBindings(
                    input,
                    tags,
                    decoded,
                    head,
                    tail,
                    required,
                    optional,
                    rest,
                    callback,
                    fieldValue,
                    result
                )
                || source.Name != input.Name || options.Name != tags.Name || components.Name != decoded.Name
                || field.Name != head.Name || key.Name != required.Name || first.Name != required.Name
                || second.Name != fieldValue.Name || returned.Name != result.Name
                || ReadTableOptionalDecode(open, tail.Name) is not { } read
                || !DistinctBindings(
                    input,
                    tags,
                    decoded,
                    head,
                    tail,
                    required,
                    optional,
                    rest,
                    callback,
                    fieldValue,
                    result,
                    read.Payload,
                    read.Remaining
                )
                || ReadTableDecodeCatch(
                    choose,
                    optional.Name,
                    callback.Name,
                    read.Absent
                ) is not { } caught
                || ReadTableUnexpectedFields(remaining, rest.Name) is not { } unexpected
                || !AttributeLiteral.TryNormalizeConstant(requiredTags, out Term neededTags)
                || !entries.TryGetValue((matchTags.Function, 2), out int matcher)
                || !entries.TryGetValue((readRequired.Function, 2), out int requiredDecoder)
                || !entries.TryGetValue((read.Decode.Function, 2), out int optionalDecoder))
            {
                return false;
            }

            int noOptional = Label();
            int selected = Label();
            int invoke = Label();
            int resultReady = Label();
            int extra = Label();
            int handler = Label();
            int reported = Label();
            int reraised = Label();
            int malformed = Label();
            EmitFrameAllocation(arity: 2, size: 4);
            EmitFrameInitialization(0, 4);
            BeamSourceLocation previous = location;
            EmitLocal(matchTags, matcher);
            CheckList(XRegisterTag, 0, malformed);
            Unpack(XRegisterTag, 0, 3);
            owner.EmitConstant(neededTags, 1);
            EmitLocal(readRequired, requiredDecoder);

            Move(
                XRegisterTag,
                0,
                YRegisterTag,
                2
            );
            CheckList(YRegisterTag, 3, noOptional);
            Unpack(YRegisterTag, 3, 1);
            owner.EmitConstant(read.Tags, 1);
            EmitFrameInitialization(3, 4);
            EmitLocal(read.Decode, optionalDecoder);
            Move(
                YRegisterTag,
                1,
                YRegisterTag,
                0
            );
            Move(
                XRegisterTag,
                0,
                YRegisterTag,
                1
            );
            JumpTo(selected);

            Mark(noOptional);
            Move(
                YRegisterTag,
                3,
                YRegisterTag,
                0
            );
            owner.Op(BeamEmissionFormat.Move, AtomTag, owner.Intern(read.Absent.Name));
            owner.Operand(YRegisterTag, 1);

            Mark(selected);
            owner.MoveToX0(YRegisterTag, 2);
            owner.EmitCallLine(select, previous);
            Call(select.Module, select.Function, 1);
            previous = CallLocation(select, previous);
            owner.Op(ExactEqualTest, LabelTag, invoke);
            owner.Operand(YRegisterTag, 1);
            owner.LiteralOperand(read.Absent);
            owner.EmitConstant(read.Absent, 0);
            JumpTo(resultReady);

            Mark(invoke);
            owner.Op(TryEnter, YRegisterTag, 3);
            owner.Operand(LabelTag, handler);
            owner.EmitConstant(caught.Options, 2);
            Move(
                YRegisterTag,
                1,
                XRegisterTag,
                1
            );
            Move(
                XRegisterTag,
                0,
                XRegisterTag,
                3
            );
            EmitFrameInitialization(1, 2);
            owner.EmitConstant(caught.Field, 0);
            owner.EmitArithmeticLine(caught.Apply.SourceLine, ref previous);
            owner.Op(CallFun, BeamWire.UnsignedTag, 3);
            owner.Op(TryEnd, YRegisterTag, 3);

            Mark(resultReady);
            owner.EmitNilTest(YRegisterTag, 0, extra);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 3);
            owner.LiteralOperand(tag);
            owner.Operand(YRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(4);

            Mark(extra);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 9);
            owner.Operand(BeamWire.UnsignedTag, 0);
            TaggedPair(unexpected.UnexpectedTag, YRegisterTag, 0);
            TaggedPair(unexpected.DetailsTag, XRegisterTag, 0);
            TaggedPair(unexpected.ErrorTag, XRegisterTag, 0);
            EmitExit(unexpected.Exit);

            EmitTableCallbackCatch(
                caught,
                3,
                4,
                handler,
                reported,
                reraised,
                previous
            );

            Mark(malformed);
            previous = location;
            owner.EmitArithmeticLine(split.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            void EmitLocal(Expr.Call call, int entry)
            {
                owner.EmitCallLine(call, previous);
                previous = CallLocation(call, previous);
                owner.Op(LocalCall, BeamWire.UnsignedTag, 2);
                owner.Operand(LabelTag, entry);
            }

            void CheckList(int registerTag, int register, int failure)
            {
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(registerTag, register);
            }

            void Unpack(int registerTag, int register, int remainingSlot)
            {
                owner.Op(GetList, registerTag, register);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(YRegisterTag, remainingSlot);
            }

            void Move(
                int fromTag,
                int from,
                int toTag,
                int to
            ) =>
                owner.MoveRegister(
                    fromTag,
                    from,
                    toTag,
                    to
                );

            void TaggedPair(Atom atom, int kind, int register)
            {
                owner.EmitTupleHeader(XRegisterTag, 2);
                owner.LiteralOperand(atom);
                owner.Operand(kind, register);
            }

            void EmitExit(Expr.Call call)
            {
                owner.EmitCallLine(call, location);
                owner.Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 1);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, call.Function, 1));
                owner.Operand(BeamWire.UnsignedTag, 4);
            }
        }

        private static TableOptionalDecode? ReadTableOptionalDecode(Expr.Case choice, string tail)
        {
            if (choice is not
                {
                    Value: Expr.Variable tested,
                    Clauses:
                        [
                        {
                            Guard: null, Patterns: [Pattern.List { Items: [Pattern.Variable head], Tail: Pattern.Variable rest }],
                            Body: Expr.Tuple
                            {
                                Items:
                                        [
                                            Expr.Call
                                        {
                                            Module: null, DynamicRemote: false, Arguments: [Expr.Variable input, var tags]
                                        } decode,
                                            Expr.Variable remaining
                                        ]
                            }
                        },
                        {
                            Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                            Body: Expr.Tuple { Items: [Expr.Literal { Value: Atom absent }, Expr.Variable unchanged] }
                        }
                        ]
                }
                || tested.Name != tail || unchanged.Name != tail || input.Name != head.Name || remaining.Name != rest.Name
                || !AttributeLiteral.TryNormalizeConstant(tags, out Term constant))
            {
                return null;
            }

            return new(
                head,
                rest,
                decode,
                constant,
                absent
            );
        }

        private TableDecodeCatch? ReadTableDecodeCatch(
            Expr.Case branch,
            string optional,
            string callback,
            Atom absent
        )
        {
            if (branch is not
                {
                    Value: Expr.Variable tested,
                    Clauses:
                        [
                        {
                            Guard: null, Patterns: [Pattern.Literal { Value: Atom sentinel }],
                            Body: Expr.Literal { Value: Atom unchanged }
                        },
                        {
                            Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                            Body: Expr.Try invoke
                        }
                        ]
                }
                || tested.Name != optional || !sentinel.Equals(absent) || !unchanged.Equals(absent))
            {
                return null;
            }

            return ReadTableDecodeTry(invoke, optional, callback);
        }

        private TableDecodeCatch? ReadTableDecodeTry(
            Expr.Try invoke,
            string optional,
            string callback,
            IReadOnlySet<string>? reserved = null
        )
        {
            if (invoke is not
                {
                    Body: Expr.Apply
                    {
                        Function: Expr.Variable target,
                        Arguments: [Expr.Literal { Value: Atom field }, Expr.Variable input, var options]
                    } apply,
                    Clauses.Count: 0, After: null,
                    Catches:
                    [
                        {
                            Patterns:
                                [
                                    Pattern.Tuple
                                {
                                    Items: [Pattern.Variable kind, Pattern.Variable reason,
                                            Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                                }
                                ],
                            Guard: Expr.GuardAlternatives { Items: [var left, var right] },
                            Body: Expr.Call
                            {
                                Function: AutoImportNames.Exit, DynamicRemote: false,
                                Arguments:
                                    [
                                        Expr.Tuple
                                    {
                                        Items: [Expr.Literal { Value: Atom errorTag }, Expr.Variable error]
                                    }
                                    ]
                            } exit
                        }
                    ]
                }
                || input.Name != optional || target.Name != callback || !DistinctBindings(kind, reason)
                || kind.Name == optional || kind.Name == callback || reason.Name == optional || reason.Name == callback
                || (reserved?.Contains(kind.Name) ?? false) || (reserved?.Contains(reason.Name) ?? false)
                || error.Name != reason.Name || !owner.ResolvesErlangBif(exit)
                || !AttributeLiteral.TryNormalizeConstant(options, out Term constant))
            {
                return null;
            }

            var classes = new[] { ReadClass(left), ReadClass(right) };
            if (!classes.Contains(ErlangExceptionClasses.Error, StringComparer.Ordinal)
                || !classes.Contains(ErlangExceptionClasses.Exit, StringComparer.Ordinal))
            {
                return null;
            }

            return new(
                apply,
                field,
                constant,
                errorTag,
                exit
            );

            string? ReadClass(Expr test) => test is Expr.Binary
            {
                Operator: ErlangOperators.ExactEqual,
                Left: Expr.Variable variable,
                Right: Expr.Literal { Value: Atom value }
            } && variable.Name == kind.Name ? value.Name : null;
        }

        private TableUnexpectedFields? ReadTableUnexpectedFields(Expr.Case branch, string remaining)
        {
            if (branch is not
                {
                    Value: Expr.Variable tested,
                    Clauses:
                        [
                        { Guard: null, Patterns: [var empty], Body: Expr.Literal { Value: Atom { Name: "true" } } },
                        {
                            Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                            Body: Expr.Call
                            {
                                Function: AutoImportNames.Exit, DynamicRemote: false,
                                Arguments:
                                        [
                                            Expr.Tuple
                                        {
                                            Items:
                                                    [
                                                        Expr.Literal { Value: Atom error },
                                                        Expr.Tuple
                                                    {
                                                        Items:
                                                                [
                                                                    Expr.Literal { Value: Atom details },
                                                                    Expr.Tuple
                                                                {
                                                                    Items: [Expr.Literal { Value: Atom tag }, Expr.Variable value]
                                                                }
                                                                ]
                                                    }
                                                    ]
                                        }
                                        ]
                            } exit
                        }
                        ]
                }
                || !IsEmptyListPattern(empty) || tested.Name != remaining || value.Name != remaining
                || !owner.ResolvesErlangBif(exit))
            {
                return null;
            }

            return new(
                error,
                details,
                tag,
                exit
            );
        }
    }
}
