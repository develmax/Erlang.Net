using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private bool IsLocalExportInvocation(Expr.Call invocation) =>
        invocation.Module == name && exportedFunctions.Contains((invocation.Function, invocation.Arguments.Count));

    private sealed partial class GeneralEmitter
    {
        private bool TryEmitSelectedRecordSequenceEncoder(FunctionDefinition function)
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
                                Pattern: Pattern.Tuple
                                {
                                    Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard },
                                            Pattern.Variable first, Pattern.Variable second, Pattern.Variable third]
                                },
                                Value: Expr.Variable record
                            } unpack,
                                Expr.Match
                            {
                                Pattern: Pattern.Variable callback,
                                Value: Expr.Call
                                {
                                    Module: not null, DynamicRemote: false, Arguments: [Expr.Variable lookup]
                                } select
                            },
                                var firstPart,
                                var secondPart,
                                ..,
                                Expr.Match
                            {
                                Pattern: Pattern.Variable data,
                                Value: Expr.List { Items: var parts, Tail: null }
                            },
                                Expr.Match { Pattern: Pattern.Variable length, Value: Expr.Binary sum },
                                Expr.Call
                            {
                                Module: null, DynamicRemote: false,
                                Arguments: [Expr.Variable usingTags, Expr.Variable usingData, Expr.Variable usingLength]
                            } finish
                            ] and var sequence
                        }
                    }
                ]
                || input.Name != record.Name || lookup.Name != second.Name
                || tags.Name != usingTags.Name || data.Name != usingData.Name || length.Name != usingLength.Name
                || !owner.IsLocalExportInvocation(select)
                || !entries.TryGetValue((finish.Function, 3), out int finishEntry)
                || !DistinctBindings(
                    input,
                    tags,
                    first,
                    second,
                    third,
                    callback,
                    data,
                    length
                )
                || parts.Count != 3 || sequence.Count is not (8 or 9))
            {
                return false;
            }

            var bindings = new HashSet<string>(StringComparer.Ordinal)
                { input.Name, tags.Name, first.Name, second.Name, third.Name, callback.Name, data.Name, length.Name };
            if (!ReadPart(
                firstPart,
                parts[0],
                out Expr.Call firstCall,
                out string firstLength
            )
                || !ReadPart(
                    secondPart,
                    parts[1],
                    out Expr.Call secondCall,
                    out string secondLength
                )
                || !ReadPart(
                    sequence[^4],
                    parts[2],
                    out Expr.Call finalCall,
                    out string finalLength
                )
                || ReadRequiredOptionalSequenceField(firstCall, first.Name) is not { Constants: [var firstOptions] } firstPlan
                || ReadRequiredOptionalSequenceField(secondCall, second.Name) is not { Constants: [var secondOptions] } secondPlan
                || sum is not
                {
                    Operator: ErlangOperators.Plus, Right: Expr.Variable finalSize,
                    Left: Expr.Binary
                    {
                        Operator: ErlangOperators.Plus, Left: Expr.Variable firstSize, Right: Expr.Variable secondSize
                    } initial
                }
                || firstSize.Name != firstLength || secondSize.Name != secondLength || finalSize.Name != finalLength
                || !ProducesNumericPair(finalCall)
                || !entries.TryGetValue((finalCall.Function, finalCall.Arguments.Count), out int finalEntry))
            {
                return false;
            }

            Expr.Apply? apply = null;
            Term finalOptions;
            Term? selector = null;
            if (sequence.Count == 9)
            {
                if (sequence[4] is not Expr.Match
                    {
                        Pattern: Pattern.Tuple
                        {
                            Items: [Pattern.Variable payload, Pattern.Variable { Name: VariableScopeNames.Wildcard }]
                        },
                        Value: Expr.Apply
                        {
                            Function: Expr.Variable usingCallback,
                            Arguments: [Expr.Literal { Value: Atom field }, Expr.Variable argument, var context]
                        } invocation
                    }
                    || usingCallback.Name != callback.Name || argument.Name != third.Name
                    || !DistinctBindings(payload) || !bindings.Add(payload.Name)
                    || !AttributeLiteral.TryNormalizeConstant(context, out Term callbackOptions) || callbackOptions is not Nil
                    || finalCall.Arguments is not [Expr.Variable usingPayload, var options] || usingPayload.Name != payload.Name
                    || !AttributeLiteral.TryNormalizeConstant(options, out finalOptions))
                {
                    return false;
                }

                apply = invocation;
                selector = field;
            }
            else if (finalCall.Arguments is not [Expr.Variable last, var options, Expr.Variable usingCallback]
                || last.Name != third.Name || usingCallback.Name != callback.Name
                || !AttributeLiteral.TryNormalizeConstant(options, out finalOptions))
            {
                return false;
            }

            int malformedPair = apply is null ? 0 : Label();
            int malformedRecord = Label();
            EmitTupleShapeTest(
                XRegisterTag,
                0,
                4,
                malformedRecord
            );
            EmitFrameAllocation(2, 4);
            EmitFrameInitialization(0, 1);
            Save(1, 2);
            Save(0, 3);
            Project(
                XRegisterTag,
                0,
                2,
                YRegisterTag,
                1
            );
            Restore(1, 0);
            owner.EmitCallLine(select, location);
            Call(select.Module, select.Function, 1);
            BeamSourceLocation previous = CallLocation(select, location);
            Project(
                YRegisterTag,
                3,
                1,
                XRegisterTag,
                1
            );
            Save(0, 0);
            owner.MoveToX0(XRegisterTag, 1);
            EmitField(firstCall, firstOptions, firstPlan.Entry);

            // The second input dies as its saved slot receives the first result.
            owner.Op(Swap, YRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            EmitField(secondCall, secondOptions, secondPlan.Entry);
            Project(
                YRegisterTag,
                3,
                3,
                XRegisterTag,
                1
            );
            Save(0, 3);
            if (apply is null)
            {
                Restore(0, 2);
                owner.MoveToX0(XRegisterTag, 1);
            }
            else
            {
                owner.EmitConstant(Nil.Value, 2);
                Restore(0, 3);
            }

            if (apply is null)
            {
                owner.EmitConstant(finalOptions, 1);
            }

            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 3);
            if (apply is not null)
            {
                owner.EmitConstant(selector!, 0);
                owner.EmitArithmeticLine(apply.SourceLine, ref previous);
                owner.Op(CallFun, BeamWire.UnsignedTag, 3);
                EmitTupleShapeTest(
                    XRegisterTag,
                    0,
                    2,
                    malformedPair
                );
                owner.EmitTuplePart(0, 0, 0);
                owner.EmitConstant(finalOptions, 1);
            }

            owner.EmitCallLine(finalCall, previous);
            previous = CallLocation(finalCall, previous);
            owner.EmitFieldCall(finalCall.Arguments.Count, finalEntry);
            EmitSequencePairResult(
                3,
                3,
                finishEntry,
                [initial, sum],
                previous,
                pairSlots: [0, 2],
                outerTagSlot: 1,
                joinedLengthTypes: [0, 0, 0]
            );

            if (apply is not null)
            {
                Mark(malformedPair);
                previous = location;
                owner.EmitArithmeticLine(apply.SourceLine, ref previous);
                owner.Op(BadMatch, XRegisterTag, 0);
            }

            Mark(malformedRecord);
            previous = location;
            owner.EmitArithmeticLine(unpack.SourceLine, ref previous);
            owner.Op(BadMatch, XRegisterTag, 0);

            return true;

            bool ReadPart(
                Expr expression,
                Expr output,
                out Expr.Call call,
                out string size
            )
            {
                if (expression is Expr.Match
                    {
                        Pattern: Pattern.Tuple { Items: [Pattern.Variable bytes, Pattern.Variable count] },
                        Value: Expr.Call { Module: null, DynamicRemote: false } fieldCall
                    }
                    && output is Expr.Variable usingBytes && usingBytes.Name == bytes.Name
                    && DistinctBindings(bytes, count) && bindings.Add(bytes.Name) && bindings.Add(count.Name))
                {
                    call = fieldCall;
                    size = count.Name;

                    return true;
                }

                call = null!;
                size = string.Empty;

                return false;
            }

            void EmitField(Expr.Call field, Term options, int entry)
            {
                owner.EmitConstant(options, 1);
                owner.EmitCallLine(field, previous);
                previous = CallLocation(field, previous);
                owner.EmitFieldCall(2, entry);
            }

            void Save(int from, int to) => owner.MoveRegister(
                XRegisterTag,
                from,
                YRegisterTag,
                to
            );

            void Restore(int from, int to) => owner.MoveRegister(
                YRegisterTag,
                from,
                XRegisterTag,
                to
            );

            void Project(
                int fromTag,
                int from,
                int field,
                int toTag,
                int to
            )
            {
                owner.Op(GetTupleElement, fromTag, from);
                owner.Operand(BeamWire.UnsignedTag, field);
                owner.Operand(toTag, to);
            }
        }
    }
}
