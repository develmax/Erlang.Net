using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private sealed record PrimitiveBinaryProjection(Expr.Call Header, Expr.Call Definite, Expr.Call Indefinite);

        private bool TryEmitPrimitiveBinaryProjection(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || owner.OnLoadFunction is not null
                || recursiveClosure is not null || ReadPrimitiveBinaryProjection(function) is not { } arm)
            {
                return false;
            }

            const int form = 1;
            const int payload = 2;
            const int tag = 1;
            const int tail = 0;
            int constructed = Label();
            int primitive = Label();
            int indefinite = Label();
            EmitFrameAllocation(1, 2);
            EmitFrameInitialization(0, 2);
            owner.EmitCallLine(arm.Header, location);
            owner.EmitFieldCall(1, entries[(arm.Header.Function, 1)]);
            var previous = CallLocation(arm.Header, location);
            owner.EmitTupleElement(0, form);
            EmitPrimitiveStackProjection(1, tag);
            owner.EmitTupleElement(2, payload);
            EmitPrimitiveStackProjection(3, tail);
            owner.instructions.WriteByte(SelectValue);
            owner.EmitTypedRegister(
                form,
                IntegerTypeBits,
                0,
                2
            );
            owner.Operand(LabelTag, indefinite);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.LiteralOperand(Term.I(0));
            owner.Operand(LabelTag, primitive);
            owner.LiteralOperand(Term.I(1));
            owner.Operand(LabelTag, constructed);

            Mark(constructed);
            owner.MoveToX0(XRegisterTag, payload);
            owner.EmitCallLine(arm.Definite, previous);
            owner.EmitFieldCall(1, entries[(arm.Definite.Function, 1)]);
            EmitTaggedPrimitivePair(
                tag,
                value: 0,
                tail,
                live: 1
            );

            Mark(primitive);
            EmitTaggedPrimitivePair(
                tag,
                value: payload,
                tail,
                live: 3
            );

            Mark(indefinite);
            owner.EmitConstant(Nil.Value, 1);
            owner.Op(TrimFrame, BeamWire.UnsignedTag, 1);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.MoveToX0(XRegisterTag, payload);
            owner.EmitCallLine(arm.Indefinite, previous);
            owner.EmitFieldCall(2, entries[(arm.Indefinite.Function, 2)]);
            owner.Op(TestHeap, BeamWire.UnsignedTag, 6);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.EmitTupleElement(0, 1);
            owner.EmitTupleElement(1, 0);
            owner.Op(PutTuple, XRegisterTag, 1);
            owner.instructions.WriteByte(ExtendedListOperand);
            owner.Operand(BeamWire.UnsignedTag, 2);
            owner.Operand(YRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.Operand(XRegisterTag, 1);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);

            return true;
        }

        private void EmitTaggedPrimitivePair(
            int tag,
            int value,
            int tail,
            int live
        )
        {
            owner.Op(TestHeap, BeamWire.UnsignedTag, 6);
            owner.Operand(BeamWire.UnsignedTag, live);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.Operand(YRegisterTag, tag);
            owner.Operand(XRegisterTag, value);
            owner.EmitTupleHeader(XRegisterTag, 2);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(YRegisterTag, tail);
            owner.EmitFrameReturn(2);
        }

        private void EmitPrimitiveStackProjection(int index, int slot)
        {
            owner.Op(GetTupleElement, XRegisterTag, 0);
            owner.Operand(BeamWire.UnsignedTag, index);
            owner.Operand(YRegisterTag, slot);
        }

        private PrimitiveBinaryProjection? ReadPrimitiveBinaryProjection(FunctionDefinition function)
        {
            if (function.Arity != 1 || owner.IsDeclaredNative((function.Name, 1)) || function.Clauses is not
                [
                {
                    Guard: null, Patterns: [Pattern.Variable input],
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match
                            {
                                Pattern: Pattern.Tuple
                                {
                                    Items: [Pattern.Variable form, Pattern.Variable tag, Pattern.Variable bytes, Pattern.Variable tail]
                                },
                                Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable original] } header
                            },
                                Expr.Case
                            {
                                Value: Expr.Variable selected,
                                Clauses:
                                    [
                                    { Guard: null, Patterns: [Pattern.Literal { Value: Integer one }], Body: var constructed },
                                    { Guard: null, Patterns: [Pattern.Literal { Value: Integer zero }], Body: var primitive },
                                    {
                                        Guard: null,
                                        Patterns: [Pattern.Literal { Value: Integer two }],
                                        Body: Expr.Sequence
                                        {
                                            Items:
                                                [
                                                    Expr.Match
                                                {
                                                    Pattern: Pattern.Tuple { Items: [Pattern.Variable value, Pattern.Variable remainder] },
                                                    Value: Expr.Call
                                                    {
                                                        Module: null, DynamicRemote: false,
                                                        Arguments: [Expr.Variable raw, Expr.List { Items.Count: 0, Tail: null } or Expr.Literal { Value: Nil }]
                                                    } indefinite
                                                },
                                                    var indefiniteResult
                                                ]
                                        }
                                    }
                                    ]
                            }
                            ]
                    }
                }
                ]
                || original.Name != input.Name || selected.Name != form.Name
                || !one.Value.IsOne || !zero.Value.IsZero || two.Value != 2
                || !DistinctBindings(
                    input,
                    form,
                    tag,
                    bytes,
                    tail,
                    value,
                    remainder
                )
                || ReadResult(constructed, tail.Name) is not Expr.Call
                { Module: null, DynamicRemote: false, Arguments: [Expr.Variable contents] } definite
                || contents.Name != bytes.Name
                || ReadResult(primitive, tail.Name) is not Expr.Variable bare || bare.Name != bytes.Name
                || raw.Name != bytes.Name || ReadResult(indefiniteResult, remainder.Name) is not Expr.Variable produced
                || produced.Name != value.Name || !IsOrdinaryLocal(header) || !IsOrdinaryLocal(definite)
                || !IsOrdinaryLocal(indefinite) || !ProvesPrimitiveHeaderShape(localFunctions[(header.Function, 1)])
                || !ProvesIndefiniteReturn(localFunctions[(indefinite.Function, 2)]))
            {
                return null;
            }

            return new(header, definite, indefinite);

            Expr? ReadResult(Expr expression, string remainder) => expression is Expr.Tuple
            {
                Items: [Expr.Tuple { Items: [Expr.Variable key, var payload] }, Expr.Variable rest]
            } && key.Name == tag.Name && rest.Name == remainder ? payload : null;

            bool IsOrdinaryLocal(Expr.Call call)
            {
                var signature = (call.Function, call.Arguments.Count);

                return entries.ContainsKey(signature) && localFunctions.ContainsKey(signature) && !owner.IsDeclaredNative(signature);
            }
        }

        private static bool ProvesPrimitiveHeaderShape(FunctionDefinition provider)
        {
            foreach (var clause in provider.Clauses)
            {
                if (clause.Patterns is not [BitPattern input])
                {
                    return false;
                }

                var binaries = new HashSet<string>(StringComparer.Ordinal);
                var formBits = new HashSet<string>(StringComparer.Ordinal);
                ReadBindings(input);
                Expr result = clause.Body;
                if (result is Expr.Sequence { Items: [.., var last] } sequence)
                {
                    foreach (var item in sequence.Items.Take(sequence.Items.Count - 1))
                    {
                        if (item is Expr.Match { Pattern: BitPattern binary })
                        {
                            ReadBindings(binary);
                        }
                    }

                    result = last;
                }

                if (result is not Expr.Tuple { Items: [var form, _, var payload, var tail] }
                    || !(form is Expr.Literal { Value: Integer kind } && kind.Value >= 0 && kind.Value <= 2
                        || form is Expr.Variable flag && formBits.Contains(flag.Name))
                    || !IsProvenBinary(payload) || !IsProvenBinary(tail))
                {
                    return false;
                }

                void ReadBindings(BitPattern pattern)
                {
                    foreach (var segment in pattern.Segments)
                    {
                        if (segment.Value is not Pattern.Variable { Name: not VariableScopeNames.Wildcard } binding)
                        {
                            continue;
                        }

                        if (segment.Specification is { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BinaryUnit })
                        {
                            binaries.Add(binding.Name);
                        }
                        else if (segment.Specification is
                        { Type: BitSegmentTypes.Integer, Signed: false, Unit: BitSyntaxDefaults.NumericUnit }
                            && FixedSize(segment.Specification) == 1)
                        {
                            formBits.Add(binding.Name);
                        }
                    }
                }

                bool IsProvenBinary(Expr value) => value is Expr.Variable binary && binaries.Contains(binary.Name)
                    || value is Expr.Literal { Value: BitString { IsBinary: true } }
                    || value is Expr.Bits { Segments.Count: 0 };
            }

            return true;
        }

        private static bool ProvesIndefiniteReturn(FunctionDefinition function) => function.Clauses is
        [
        {
            Guard: null, Patterns: [BitPattern end, Pattern.Variable accumulator],
            Body: Expr.Tuple { Items: [_, Expr.Variable tail] }
        },
        {
            Guard: null, Patterns: [Pattern.Variable { }, Pattern.Variable { }],
            Body: Expr.Sequence
            {
                Items: [_, Expr.Call { Module: null, DynamicRemote: false, Arguments.Count: 2 } recur]
            }
        }
        ] && recur.Function == function.Name && ReadIndefiniteBinaryEnd(end, tail) is not null
            && !end.Segments.Any(segment => segment.Value is Pattern.Variable value && value.Name == accumulator.Name);

        private bool TryEmitDefiniteBinaryList(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || owner.OnLoadFunction is not null
                || recursiveClosure is not null || owner.IsDeclaredNative((function.Name, 1)) || function.Clauses is not
                [
                {
                    Patterns: [Pattern.Variable emptyInput],
                    Guard: Expr.Binary
                    {
                        Operator: ErlangOperators.ExactEqual,
                        Left: Expr.Call { Function: AutoImportNames.ByteSize, DynamicRemote: false, Arguments: [Expr.Variable tested] } size,
                        Right: Expr.Literal { Value: Integer zero }
                    },
                    Body: Expr.List { Items.Count: 0, Tail: null } or Expr.Literal { Value: Nil }
                },
                {
                    Guard: null, Patterns: [Pattern.Variable input],
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match
                            {
                                Pattern: Pattern.Tuple { Items: [Pattern.Variable value, Pattern.Variable tail] },
                                Value: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable bytes] } decode
                            },
                                Expr.List
                            {
                                Items: [Expr.Variable head],
                                Tail: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable rest] } recur
                            }
                            ]
                    }
                }
                ]
                || tested.Name != emptyInput.Name || !zero.Value.IsZero || !owner.ResolvesErlangBif(size)
                || !DistinctBindings(input, value, tail) || bytes.Name != input.Name || head.Name != value.Name
                || rest.Name != tail.Name || recur.Function != function.Name
                || !entries.TryGetValue((decode.Function, 1), out int decoder)
                || !entries.TryGetValue((function.Name, 1), out int next)
                || !localFunctions.TryGetValue((decode.Function, 1), out var provider)
                || ReadPrimitiveBinaryProjection(provider) is not { } read || read.Definite.Function != function.Name
                || !HasOnlyLocalCalls(
                    function,
                    1,
                    (caller, call) =>
                    caller.Name == provider.Name && ReferenceEquals(call, read.Definite)
                    || caller.Name == function.Name && ReferenceEquals(call, recur)
                ))
            {
                return false;
            }

            int nonempty = Label();
            EmitSizeInstruction(size.Function, failure: 0, live: 1);
            owner.EmitTypedRegister(0, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
            owner.Operand(XRegisterTag, 1);
            owner.Op(ExactEqualTest, LabelTag, nonempty);
            owner.EmitTypedRegister(
                1,
                IntegerTypeBits,
                0,
                MaximumBitSize
            );
            owner.LiteralOperand(Term.I(0));
            owner.EmitConstant(Nil.Value, 0);
            owner.instructions.WriteByte(Return);

            Mark(nonempty);
            EmitFrameAllocation(1, 1);
            EmitFrameInitialization(0, 1);
            owner.EmitCallLine(decode, location);
            owner.EmitFieldCall(1, decoder);
            EmitPrimitiveStackProjection(0, 0);
            owner.EmitTupleElement(1, 0);
            owner.EmitCallLine(recur, CallLocation(decode, location));
            owner.EmitFieldCall(1, next);
            owner.Op(TestHeap, BeamWire.UnsignedTag, ConsHeapWords);
            owner.Operand(BeamWire.UnsignedTag, 1);
            owner.Op(PutList, YRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            owner.EmitFrameReturn(1);

            return true;
        }
    }
}
