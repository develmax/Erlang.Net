using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private bool TryEmitBoundedSelectedPairDecoder(
            IReadOnlyList<Expr> items,
            Pattern.Variable input,
            Pattern.Variable tags,
            Pattern.Variable parsed,
            Expr.Call match
        )
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || items is not
                [
                    _, var firstSplit, var firstRead, var secondSplit,
                    Expr.Match { Pattern: Pattern.Variable raw },
                    Expr.Match
                {
                    Pattern: Pattern.Variable callback,
                    Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable key] } select
                },
                    Expr.Match { Pattern: Pattern.Variable value, Value: Expr.Try invoke },
                    Expr.Case tailCheck,
                    Expr.Match
                {
                    Pattern: Pattern.Variable output,
                    Value: Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom tag }, Expr.Variable firstResult, Expr.Variable secondResult]
                    }
                },
                    Expr.Variable returned
                ]
                || returned.Name != output.Name || value.Name != secondResult.Name || key.Name != firstResult.Name
                || !DistinctBindings(
                    input,
                    tags,
                    parsed,
                    output,
                    callback,
                    value
                )
                || !owner.IsLocalExportInvocation(select)
                || !entries.TryGetValue((match.Function, 2), out int matcher))
            {
                return false;
            }

            HashSet<string> bound = new(StringComparer.Ordinal)
            {
                input.Name, tags.Name, parsed.Name, output.Name, callback.Name, value.Name
            };
            if (ReadRequiredPart(
                firstSplit,
                firstRead,
                parsed.Name,
                firstResult,
                bound
            ) is not { } first
                || ReadRequiredPart(
                    secondSplit,
                    items[4],
                    first.Rest.Name,
                    new Expr.Variable(raw.Name),
                    bound
                ) is not { } second
                || ReadSequenceCall(second.Read, second.Payload.Name) is not { } opened
                || ReadSequenceBounds(first.Read, first.Payload.Name, bound) is not { } bounded
                || !IsDecoderIntrinsic(bounded.Bounds.Exit)
                || first.Read is not Expr.Block
                {
                    Body: Expr.Sequence { Items: [Expr.Match { Pattern: Pattern.Variable boundedValue }, _] }
                }
                || !bound.Add(boundedValue.Name)
                || ReadTableDecodeTry(
                    invoke,
                    raw.Name,
                    callback.Name,
                    bound
                ) is not { } caught
                || ReadTableUnexpectedFields(tailCheck, second.Rest.Name) is not { } unexpected)
            {
                return false;
            }

            const int frame = 4;
            const int openValue = 0;
            const int finalTail = 1;
            const int decodedKey = 2;
            const int firstTail = 3;
            int extra = Label();
            int handler = Label();
            int report = Label();
            int reraise = Label();
            int rangeFailure = Label();
            int badSecond = Label();
            int badFirst = Label();
            EmitGeneralFrame(2, slots: frame);
            BeamSourceLocation previous = location;
            EmitLocal(match, matcher);
            Split(
                XRegisterTag,
                0,
                firstTail,
                badFirst
            );
            owner.EmitConstant(bounded.Read.Options, 1);
            EmitLocal(bounded.Read.Call, bounded.Read.Entry);
            Save(decodedKey);
            EmitRangeTests(
                bounded.Read.Call,
                bounded.Bounds,
                rangeFailure,
                provenInteger: true
            );
            Split(
                YRegisterTag,
                firstTail,
                finalTail,
                badSecond
            );
            owner.EmitConstant(opened.Options, 1);
            owner.InitStackSlot(firstTail);
            EmitLocal(opened.Call, opened.Entry);
            Save(openValue);
            owner.MoveToX0(YRegisterTag, decodedKey);
            owner.EmitCallLine(select, previous);
            Call(select.Module, select.Function, 1);
            previous = CallLocation(select, previous);

            owner.Op(TryEnter, YRegisterTag, firstTail);
            owner.Operand(LabelTag, handler);
            owner.EmitConstant(caught.Options, 2);
            owner.MoveRegister(
                YRegisterTag,
                openValue,
                XRegisterTag,
                1
            );
            owner.MoveRegister(
                XRegisterTag,
                0,
                XRegisterTag,
                3
            );
            owner.InitStackSlot(openValue);
            owner.EmitConstant(caught.Field, 0);
            owner.EmitArithmeticLine(caught.Apply.SourceLine, ref previous);
            owner.Op(CallFun, BeamWire.UnsignedTag, 3);
            owner.Op(TryEnd, YRegisterTag, firstTail);
            owner.EmitNilTest(YRegisterTag, finalTail, extra);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 4);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 3);
            owner.LiteralOperand(tag);
            owner.Operand(YRegisterTag, decodedKey);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            Mark(extra);
            EmitCheckedDecoderTailExit(
                unexpected.Exit,
                [unexpected.ErrorTag, unexpected.DetailsTag, unexpected.UnexpectedTag],
                YRegisterTag,
                finalTail,
                frame
            );
            EmitTableCallbackCatch(
                caught,
                firstTail,
                frame,
                handler,
                report,
                reraise,
                previous
            );
            Mark(rangeFailure);
            EmitRangeExit(bounded.Bounds, CallLocation(bounded.Bounds.Exit, location), frame);
            EmitDecoderFailures([(badFirst, null, first.Split), (badSecond, firstTail, second.Split)]);

            return true;

            void EmitLocal(Expr.Call call, int entry)
            {
                owner.EmitCallLine(call, previous);
                owner.EmitFieldCall(2, entry);
                previous = CallLocation(call, previous);
            }

            void Save(int slot) => owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                slot
            );

            void Split(
                int register,
                int list,
                int rest,
                int failure
            )
            {
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(register, list);
                owner.Op(GetList, register, list);
                owner.Operand(XRegisterTag, 0);
                owner.Operand(YRegisterTag, rest);
            }
        }

        private bool TryEmitSelectedRecordSequenceDecoder(FunctionDefinition function)
        {
            if (owner.ModuleOptimizationDisabled || !owner.SsaTypeInferenceEnabled || recursiveClosure is not null
                || function.Clauses is not
                [
                    {
                        Guard: null, Patterns: [Pattern.Variable input, Pattern.Variable tags],
                        Body: Expr.Sequence
                        {
                            Items:
                            [
                                Expr.Match
                            {
                                Pattern: Pattern.Variable parsed,
                                Value: Expr.Call
                                {
                                    Module: null, DynamicRemote: false,
                                    Arguments: [Expr.Variable source, Expr.Variable parameters]
                                } match
                            },
                                var firstSplit, var firstRead, var secondSplit, var secondRead,
                                ..,
                                Expr.Case tailCheck,
                                Expr.Match
                            {
                                Pattern: Pattern.Variable output,
                                Value: Expr.Tuple
                                {
                                    Items: [Expr.Literal { Value: Atom tag }, var firstResult,
                                            var secondResult, Expr.Variable thirdResult]
                                }
                            },
                                Expr.Variable returned
                            ] and var sequence
                        }
                    }
                ]
                || sequence.Count is not (11 or 12)
                || input.Name != source.Name || tags.Name != parameters.Name || returned.Name != output.Name
                || !DistinctBindings(
                    input,
                    tags,
                    parsed,
                    output
                )
                || !entries.TryGetValue((match.Function, 2), out int matcher))
            {
                return false;
            }

            bool protectedCallback = sequence.Count == 12;
            int selectorIndex = protectedCallback ? 7 : 5;
            int thirdIndex = protectedCallback ? 5 : 6;
            if (sequence[selectorIndex] is not Expr.Match
                {
                    Pattern: Pattern.Variable callback,
                    Value: Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable key] } select
                }
                || !DistinctBindings(callback) || !owner.IsLocalExportInvocation(select))
            {
                return false;
            }

            var bindings = new HashSet<string>(StringComparer.Ordinal) { input.Name, tags.Name, parsed.Name, output.Name };
            if (!bindings.Add(callback.Name)
                || ReadRequiredPart(
                    firstSplit,
                    firstRead,
                    parsed.Name,
                    firstResult,
                    bindings
                ) is not { } first
                || ReadRequiredPart(
                    secondSplit,
                    secondRead,
                    first.Rest.Name,
                    secondResult,
                    bindings
                ) is not { } second
                || ReadSequenceCall(second.Read, second.Payload.Name) is not { } identifier
                || key.Name != second.Output.Name
                || sequence[thirdIndex + 1] is not Expr.Match { Pattern: Pattern.Variable thirdBinding }
                || ReadRequiredPart(
                    sequence[thirdIndex],
                    sequence[thirdIndex + 1],
                    second.Rest.Name,
                    new Expr.Variable(thirdBinding.Name),
                    bindings
                ) is not { } third
                || ReadTableUnexpectedFields(tailCheck, third.Rest.Name) is not { } unexpected)
            {
                return false;
            }

            Expr.Call? consumer = null;
            Term? options = null;
            int consumerEntry = 0;
            SequenceRead? opened = null;
            Expr.Try? invocation = null;
            if (protectedCallback)
            {
                if (sequence[8] is not Expr.Match { Pattern: Pattern.Variable field, Value: Expr.Try attempt }
                    || field.Name != thirdResult.Name || !DistinctBindings(field) || !bindings.Add(field.Name)
                    || ReadSequenceCall(third.Read, third.Payload.Name) is not { } openType)
                {
                    return false;
                }

                invocation = attempt;
                opened = openType;
            }
            else if (thirdBinding.Name != thirdResult.Name || third.Read is not Expr.Call
            {
                Module: null, DynamicRemote: false, Arguments: [Expr.Variable raw, var extra, Expr.Variable target]
            } local || raw.Name != third.Payload.Name || target.Name != callback.Name
                || !AttributeLiteral.TryNormalizeConstant(extra, out options)
                || !entries.TryGetValue((local.Function, 3), out consumerEntry))
            {
                return false;
            }
            else
            {
                consumer = local;
            }

            if (ReadSequenceBounds(
                first.Read,
                first.Payload.Name,
                bindings
            ) is not { } bounded || !owner.ResolvesErlangBif(bounded.Bounds.Exit)
                || first.Read is not Expr.Block
                {
                    Body: Expr.Sequence { Items: [Expr.Match { Pattern: Pattern.Variable boundedValue }, _] }
                })
            {
                return false;
            }

            bindings.Add(boundedValue.Name);
            TableDecodeCatch? caught = invocation is null
                ? null
                : ReadTableDecodeTry(
                    invocation,
                    thirdBinding.Name,
                    callback.Name,
                    bindings
                );
            if (protectedCallback && caught is null)
            {
                return false;
            }

            int frame = protectedCallback ? 5 : 4;
            int firstRest = frame - 1;
            int firstValue = frame - 2;
            int secondRest = frame - 3;
            int secondValue = protectedCallback ? 1 : 3;
            int extraFields = Label();
            int handler = protectedCallback ? Label() : 0;
            int report = protectedCallback ? Label() : 0;
            int reraise = protectedCallback ? Label() : 0;
            int rangeFailure = Label();
            int badThird = Label();
            int badSecond = Label();
            int badFirst = Label();
            EmitFrameAllocation(2, frame);
            EmitFrameInitialization(0, frame);
            BeamSourceLocation previous = location;
            EmitLocal(match, matcher, 2);
            Split(
                XRegisterTag,
                0,
                firstRest,
                badFirst
            );
            owner.EmitConstant(bounded.Read.Options, 1);
            EmitLocal(bounded.Read.Call, bounded.Read.Entry, 2);
            Save(firstValue);
            EmitRangeTests(
                bounded.Read.Call,
                bounded.Bounds,
                rangeFailure,
                provenInteger: true
            );
            Split(
                YRegisterTag,
                firstRest,
                secondRest,
                badSecond
            );
            owner.EmitConstant(identifier.Options, 1);
            owner.InitStackSlot(firstRest);
            EmitLocal(identifier.Call, identifier.Entry, 2);
            Save(secondValue);

            if (caught is not null)
            {
                Split(
                    YRegisterTag,
                    secondRest,
                    0,
                    badThird
                );
                owner.EmitConstant(opened!.Options, 1);
                owner.InitStackSlot(secondRest);
                EmitLocal(opened.Call, opened.Entry, 2);
                Save(secondRest);
                owner.MoveToX0(YRegisterTag, secondValue);
                EmitSelector();

                // The first list tail is dead, and its slot can now hold the catch token.
                owner.Op(TryEnter, YRegisterTag, firstRest);
                owner.Operand(LabelTag, handler);
                owner.EmitConstant(caught.Options, 2);
                owner.MoveRegister(
                    YRegisterTag,
                    secondRest,
                    XRegisterTag,
                    1
                );
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    3
                );
                owner.InitStackSlot(secondRest);
                owner.EmitConstant(caught.Field, 0);
                owner.EmitArithmeticLine(caught.Apply.SourceLine, ref previous);
                owner.Op(CallFun, BeamWire.UnsignedTag, 3);
                owner.Op(TryEnd, YRegisterTag, firstRest);
            }
            else
            {
                EmitSelector();
                Split(
                    YRegisterTag,
                    secondRest,
                    0,
                    badThird,
                    head: 1
                );
                owner.MoveRegister(
                    XRegisterTag,
                    0,
                    XRegisterTag,
                    2
                );
                owner.MoveToX0(XRegisterTag, 1);
                owner.EmitConstant(options!, 1);
                owner.InitStackSlot(secondRest);
                EmitLocal(consumer!, consumerEntry, 3);
            }

            owner.EmitNilTest(YRegisterTag, 0, extraFields);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 5);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 4);
            owner.LiteralOperand(tag);
            owner.Operand(YRegisterTag, firstValue);
            owner.Operand(YRegisterTag, secondValue);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(frame);

            Mark(extraFields);
            EmitCheckedDecoderTailExit(
                unexpected.Exit,
                [unexpected.ErrorTag, unexpected.DetailsTag, unexpected.UnexpectedTag],
                YRegisterTag,
                0,
                frame
            );
            if (caught is not null)
            {
                EmitTableCallbackCatch(
                    caught,
                    firstRest,
                    frame,
                    handler,
                    report,
                    reraise,
                    previous
                );
            }

            Mark(rangeFailure);
            EmitRangeExit(bounded.Bounds, CallLocation(bounded.Bounds.Exit, location), frame);
            Fail(
                badThird,
                third,
                YRegisterTag,
                secondRest
            );
            Fail(
                badSecond,
                second,
                YRegisterTag,
                firstRest
            );
            Fail(
                badFirst,
                first,
                XRegisterTag,
                0
            );

            return true;

            void EmitLocal(Expr.Call call, int entry, int arity)
            {
                owner.EmitCallLine(call, previous);
                owner.EmitFieldCall(arity, entry);
                previous = CallLocation(call, previous);
            }

            void EmitSelector()
            {
                owner.EmitCallLine(select, previous);
                Call(select.Module, select.Function, 1);
                previous = CallLocation(select, previous);
            }

            void Save(int slot) => owner.MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                slot
            );

            void Split(
                int register,
                int list,
                int rest,
                int failure,
                int head = 0
            )
            {
                owner.Op(IsNonemptyList, LabelTag, failure);
                owner.Operand(register, list);
                owner.Op(GetList, register, list);
                owner.Operand(XRegisterTag, head);
                owner.Operand(YRegisterTag, rest);
            }

            void Fail(
                int label,
                SequenceRequired field,
                int register,
                int list
            )
            {
                Mark(label);
                var failureLocation = location;
                owner.EmitArithmeticLine(field.Split.SourceLine, ref failureLocation);
                owner.Op(BadMatch, register, list);
            }
        }
    }
}
