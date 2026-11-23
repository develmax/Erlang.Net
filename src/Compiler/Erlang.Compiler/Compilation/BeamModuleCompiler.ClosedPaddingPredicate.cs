using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private sealed partial class GeneralEmitter
    {
        private const int MaximumSerializedTypeUnit = byte.MaxValue + 1;

        private bool TryEmitClosedPaddingPredicate(FunctionDefinition function)
        {
            if (!owner.SsaTypeInferenceEnabled || owner.ModuleOptimizationDisabled || owner.OnLoadFunction is not null
                || owner.IsDeclaredNative((function.Name, function.Arity))
                || function.Clauses is not
                [
                {
                    Guard: null,
                    Patterns: [AliasPattern alias, Pattern.Variable names, Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Case
                    {
                        Value: Expr.Call { Module: "lists", Function: "sort", DynamicRemote: false, Arguments: [Expr.Variable sorted] } sort,
                        Clauses:
                            [
                            { Guard: null, Patterns: [Pattern.Variable compared], Body: Expr.Literal { Value: Atom acceptedList } },
                            { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: Expr.Call listThrow }
                            ]
                    }
                },
                {
                    Guard: null,
                    Patterns: [Pattern.Variable equal, Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable repeated, Pattern.Variable { Name: VariableScopeNames.Wildcard }],
                    Body: Expr.Literal { Value: Atom acceptedEqual }
                },
                {
                    Guard: null,
                    Patterns: [Pattern.Variable value, Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable prefix, Pattern.Variable width],
                    Body: Expr.Sequence
                    {
                        Items:
                            [
                                Expr.Match
                            {
                                Pattern: Pattern.Variable rest,
                                Value: Expr.Binary
                                {
                                    Operator: ErlangOperators.Minus,
                                    Left: Expr.Call { Function: AutoImportNames.BitSize, Arguments: [Expr.Variable measured] } size,
                                    Right: Expr.Variable deducted
                                } subtraction
                            },
                                Expr.Case
                            {
                                Value: Expr.Variable matched,
                                Clauses:
                                    [
                                    {
                                        Guard: null,
                                        Patterns: [BitPattern { Segments: [var prefixBits, var paddingBits] }],
                                        Body: Expr.Literal { Value: Atom acceptedBits }
                                    },
                                    { Guard: null, Patterns: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Body: Expr.Call bitsThrow }
                                    ]
                            }
                            ]
                    }
                }
                ]
                || !TryReadPatternAlias(alias, out var head, out var list)
                || list is not Pattern.List { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }], Tail: Pattern.Variable { Name: VariableScopeNames.Wildcard } }
                || !DistinctBindings(head, names) || head.Name != sorted.Name || names.Name != compared.Name
                || equal.Name == VariableScopeNames.Wildcard || equal.Name != repeated.Name
                || !DistinctBindings(
                    value,
                    prefix,
                    width,
                    rest
                )
                || value.Name != measured.Name || width.Name != deducted.Name || value.Name != matched.Name
                || acceptedList.Name != ErlangBooleanAtoms.True || !acceptedList.Equals(acceptedEqual) || !acceptedList.Equals(acceptedBits)
                || !owner.ResolvesErlangBif(size)
                || prefixBits is not
                {
                    Value: Pattern.Variable expected,
                    Specification: { Type: BitSegmentTypes.Binary, Unit: BitSyntaxDefaults.BitStringUnit, Size: Expr.Variable prefixSize }
                }
                || paddingBits is not
                {
                    Value: Pattern.Literal { Value: Integer zero },
                    Specification:
                    {
                        Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.NumericUnit,
                        Endian: BitByteOrders.Big, Signed: false, Size: Expr.Variable suffixSize
                    }
                }
                || expected.Name != prefix.Name || prefixSize.Name != width.Name || suffixSize.Name != rest.Name || !zero.Value.IsZero
                || ReadStackFreeThrow(listThrow) is not { Arguments: [_, var listReason, _] } rejectList
                || ReadStackFreeThrow(bitsThrow) is not { Arguments: [_, var bitsReason, _] } rejectBits
                || !AttributeLiteral.TryNormalizeConstant(listReason, out Term listError)
                || !AttributeLiteral.TryNormalizeConstant(bitsReason, out Term bitsError))
            {
                return false;
            }

            int prefixWidth = 0;
            if (!HasOnlyLocalCalls(
                function,
                4,
                (_, call) =>
                {
                    if (!AttributeLiteral.TryNormalizeConstant(call.Arguments[1], out Term listArgument) || listArgument is not Cons
                        || !AttributeLiteral.TryNormalizeConstant(call.Arguments[2], out Term prefixArgument)
                        || prefixArgument is not BitString { BitLength: > 0 } fixedBits
                        || !AttributeLiteral.TryNormalizeConstant(call.Arguments[3], out Term widthArgument)
                        || widthArgument is not Integer fixedWidth || fixedWidth.Value != fixedBits.BitLength
                        || prefixWidth != 0 && prefixWidth != fixedBits.BitLength)
                    {
                        return false;
                    }

                    prefixWidth = fixedBits.BitLength;

                    return true;
                }
            ))
            {
                return false;
            }

            int prefixUnit = (int)System.Numerics.BigInteger.GreatestCommonDivisor(prefixWidth, MaximumSerializedTypeUnit);
            int rejectNames = Label();
            int bits = Label();
            int accepted = Label();
            int rejectPadding = Label();
            owner.Op(IsNonemptyList, LabelTag, bits);
            owner.Operand(XRegisterTag, 0);
            EmitFrameAllocation(2, 1);
            owner.MoveRegister(
                XRegisterTag,
                1,
                YRegisterTag,
                0
            );
            owner.EmitCallLine(sort, location);
            Call(sort.Module, sort.Function, 1);
            owner.Op(ExactEqualTest, LabelTag, rejectNames);
            owner.EmitTypedRegister(0, ConsTypeBits);
            owner.EmitTypedRegister(0, ConsTypeBits, registerTag: YRegisterTag);
            owner.EmitConstant(acceptedList, 0);
            owner.EmitFrameReturn(1);

            Mark(rejectNames);
            EmitRejection(rejectList, listError, frame: 1);

            Mark(bits);
            owner.Op(ExactNotEqualTest, LabelTag, accepted);
            owner.EmitTypedRegister(2, MatchableTypeBits, unit: prefixUnit);
            owner.Operand(XRegisterTag, 0);
            owner.EmitCallLine(size, location);
            EmitSizeInstruction(size.Function, 0, 3);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 1);

            var previous = CallLocation(size, location);
            owner.EmitArithmeticLine(subtraction.SourceLine, ref previous);
            owner.Op(GcBifTwo, LabelTag, 0);
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, ErlangOperators.Minus, 2));
            owner.EmitTypedRegister(
                1,
                IntegerTypeBits,
                0,
                MaximumBitSize
            );
            owner.LiteralOperand(Term.I(prefixWidth));
            owner.Operand(XRegisterTag, 1);
            owner.Op(StartBinaryMatchKnown, AtomTag, owner.Intern(BeamBinaryNames.NoFail));
            owner.Operand(BeamWire.UnsignedTag, 3);
            owner.Operand(XRegisterTag, 0);
            owner.Operand(XRegisterTag, 0);
            EmitBitMatchHeader(
                0,
                rejectPadding,
                9,
                prefixWidth,
                BitSyntaxDefaults.BitStringUnit
            );
            EmitBitSegment(
                prefixBits.Specification,
                prefixWidth,
                3,
                XRegisterTag,
                3
            );
            owner.Op(GetBinaryInteger, LabelTag, rejectPadding);
            owner.EmitTypedRegister(0, MatchableTypeBits);
            owner.Operand(BeamWire.UnsignedTag, 4);
            owner.EmitTypedRegister(
                1,
                IntegerTypeBits,
                -prefixWidth,
                MaximumBitSize - prefixWidth
            );
            owner.Operand(BeamWire.UnsignedTag, BitSyntaxDefaults.NumericUnit);
            owner.Operand(BeamWire.UnsignedTag, 0);
            owner.Operand(XRegisterTag, 1);
            owner.Op(ExactEqualTest, LabelTag, rejectPadding);
            owner.EmitTypedRegister(1, IntegerTypeBits, 0);
            owner.LiteralOperand(zero);
            EmitBitMatchHeader(
                0,
                rejectPadding,
                2,
                0,
                tailUnit: null
            );
            owner.Op(ExactEqualTest, LabelTag, rejectPadding);
            owner.EmitTypedRegister(3, MatchableTypeBits, unit: BitSyntaxDefaults.BitStringUnit);
            owner.EmitTypedRegister(2, MatchableTypeBits, unit: prefixUnit);

            Mark(accepted);
            owner.EmitConstant(acceptedList, 0);
            owner.instructions.WriteByte(Return);

            Mark(rejectPadding);
            EmitRejection(rejectBits, bitsError, frame: null);

            return true;

            void EmitRejection(Expr.Call raise, Term reason, int? frame)
            {
                owner.EmitConstant(reason, 1);
                owner.EmitConstant(Nil.Value, 2);
                owner.EmitConstant(Term.A(ErlangExceptionClasses.Throw), 0);
                owner.EmitCallLine(raise, location);
                owner.Op(frame.HasValue ? TailExternalCallWithFrame : TailExternalCall, BeamWire.UnsignedTag, 3);
                owner.Operand(BeamWire.UnsignedTag, owner.Import(ErlangModuleNames.Module, raise.Function, 3));
                if (frame is int slots)
                {
                    owner.Operand(BeamWire.UnsignedTag, slots);
                }
            }
        }
    }
}
