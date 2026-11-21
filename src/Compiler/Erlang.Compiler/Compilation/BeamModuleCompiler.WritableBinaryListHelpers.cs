using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private const int OneBitSegmentWidth = 1;
    private const int ByteCapacityShift = 3;
    private const long ListLengthTypeMaximum = (1L << 58) - 1;
    private const int WritableBinaryListLabelCount = 4;
    private const int WritableBinaryTupleOwnerLabelCount = 2;
    private const int WritableBinaryTupleHelperLabelCount = 5;
    private const int WritableBinaryListHelperArity = 2;

    private sealed record WritableBinaryListPlan(
        string HelperName,
        Integer? Subtract,
        bool TuplePair,
        WritableBinaryListMapper? Mapper
    );

    private readonly Dictionary<(string Name, int Arity), WritableBinaryListPlan> writableBinaryListOwners = [];
    private readonly Dictionary<(string Name, int Arity), WritableBinaryListPlan> writableBinaryListHelpers = [];

    private (FunctionDefinition Function, BeamSourceLocation Location)? ExtractWritableBinaryListBuilder(
        FunctionDefinition function,
        BeamSourceLocation location,
        HashSet<(string Name, int Arity)> declared,
        IReadOnlyDictionary<(string Name, int Arity), FunctionDefinition> localFunctions
    )
    {
        if (!SsaTypeInferenceEnabled || ModuleOptimizationDisabled || function.Arity != 1
            || IsDeclaredNative((function.Name, function.Arity))
            || function.Clauses is not
            [
                {
                    Guard: null, Patterns: [Pattern.Variable parameter],
                    Body: Expr.BinaryComprehension
                    {
                        Body: Expr.Bits bits,
                        Qualifiers: [
                            ComprehensionQualifier.Generator { Strict: false, Source: Expr.Variable source } generator
                        ]
                    }
                }
            ] || parameter.Name != source.Name || parameter.Name == VariableScopeNames.Wildcard)
        {
            return null;
        }

        int line = generator.GeneratorLine != 0 ? generator.GeneratorLine : location.Line;
        string? file = generator.GeneratorFile ?? location.File;
        if (bits.SourceLine != 0 && bits.SourceLine != line)
        {
            return null;
        }

        bool tuplePair = IsWritableBinaryBytePair(generator.Pattern, bits.Segments);
        Integer? subtract = null;
        WritableBinaryListMapper? mapper = null;
        bool singleBit = !tuplePair && ReadWritableSingleBit(
            generator.Pattern,
            bits.Segments,
            line,
            out subtract
        );
        if (!tuplePair && !singleBit)
        {
            mapper = ReadWritableBinaryListMapper(
                generator.Pattern,
                bits.Segments,
                line,
                localFunctions
            );
        }

        if (!tuplePair && mapper is null && !singleBit)
        {
            return null;
        }

        // A standalone, single generator gives v3_core and SSA one letrec each.
        string helper = GeneratedComprehensionNames.Function(
            function.Name,
            function.Arity,
            GeneratedComprehensionNames.Binary,
            0,
            WritableBinaryListHelperArity,
            0
        );
        var helperKey = (helper, WritableBinaryListHelperArity);
        if (!declared.Add(helperKey))
        {
            return null;
        }

        var plan = new WritableBinaryListPlan(
            helper,
            subtract,
            tuplePair,
            mapper
        );
        writableBinaryListOwners.Add((function.Name, function.Arity), plan);
        writableBinaryListHelpers.Add(helperKey, plan);

        const string tail = "$writable_binary_list_tail";
        const string accumulator = "$writable_binary_list_accumulator";
        const string invalid = "$writable_binary_list_invalid";
        var prefix = new BitSegment(
            new Expr.Variable(accumulator),
            null,
            BitSegmentTypes.Binary,
            BitSyntaxDefaults.BitStringUnit
        );
        var clauses = new List<Clause>
        {
            new(
                [
                    new Pattern.List([generator.Pattern], new Pattern.Variable(tail)),
                    new Pattern.Variable(accumulator)
                ],
                null,
                new Expr.Call(null, helper, [new Expr.Variable(tail), bits with { Segments = [prefix, .. bits.Segments] }])
            )
        };
        if (tuplePair)
        {
            clauses.Add(new Clause(
                [
                    new Pattern.List([new Pattern.Variable(VariableScopeNames.Wildcard)], new Pattern.Variable(tail)),
                    new Pattern.Variable(accumulator)
                ],
                null,
                new Expr.Call(null, helper, [new Expr.Variable(tail), new Expr.Variable(accumulator)])
            ));
        }

        clauses.Add(new Clause(
            [new Pattern.Literal(Nil.Value), new Pattern.Variable(accumulator)],
            null,
            new Expr.Variable(accumulator)
        ));
        clauses.Add(new Clause(
            [new Pattern.Variable(invalid), new Pattern.Variable(VariableScopeNames.Wildcard)],
            null,
            new Expr.Call(
                ErlangModuleNames.Module,
                AutoImportNames.Error,
                [new Expr.Tuple([new Expr.Literal(Term.A(ComprehensionErrorReasons.BadGenerator)), new Expr.Variable(invalid)])]
            )
        ));

        return (new FunctionDefinition(helper, WritableBinaryListHelperArity, clauses), new(file, line));
    }

    private static bool ReadWritableSingleBit(
        Pattern pattern,
        IReadOnlyList<BitSegment> segments,
        int line,
        out Integer? subtract
    )
    {
        subtract = null;
        if (pattern is not Pattern.Variable { Name: not VariableScopeNames.Wildcard } item
            || segments is not [{ Size: Expr.Literal { Value: Integer width } } segment]
            || width.Value != OneBitSegmentWidth || !IsWritableIntegerSegment(segment))
        {
            return false;
        }

        if (segment.Value is Expr.Binary
            {
                Operator: ErlangOperators.Minus, Left: Expr.Variable left,
                Right: Expr.Literal { Value: Integer offset }
            } arithmetic && left.Name == item.Name && (arithmetic.SourceLine == 0 || arithmetic.SourceLine == line))
        {
            subtract = offset;

            return true;
        }

        return segment.Value is Expr.Variable variable && variable.Name == item.Name;
    }

    private static bool IsWritableBinaryBytePair(Pattern pattern, IReadOnlyList<BitSegment> segments)
    {
        return pattern is Pattern.Tuple
        {
            Items: [
                    Pattern.Variable { Name: not VariableScopeNames.Wildcard } first,
                    Pattern.Variable { Name: not VariableScopeNames.Wildcard } second
                ]
        }
            && first.Name != second.Name && segments is [var high, var low]
            && IsByteOf(high, first.Name) && IsByteOf(low, second.Name);

        static bool IsByteOf(BitSegment segment, string variable) =>
            IsWritableIntegerSegment(segment) && segment.Value is Expr.Variable value && value.Name == variable
            && (segment.Size is null || segment.Size is Expr.Literal { Value: Integer width }
                && width.Value == BitSyntaxDefaults.IntegerWidth);
    }

    private static bool IsWritableIntegerSegment(BitSegment segment) => segment is
    {
        Type: BitSegmentTypes.Integer, Unit: BitSyntaxDefaults.NumericUnit,
        Endian: BitByteOrders.Big, Signed: false, IsStringLiteral: false
    };

    private void EmitWritableBinaryListOwner(int entry, WritableBinaryListPlan plan)
    {
        Op(Allocate, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, 1);
        MoveRegister(
            XRegisterTag,
            0,
            YRegisterTag,
            0
        );
        if (plan.TuplePair)
        {
            // A nonmatching item is skipped, so length does not determine output size.
            EmitConstant(Term.I(BinaryComprehensionInitialCapacity), 0);
        }
        else
        {
            EmitWritableFixedWidthCapacity(plan.Mapper?.Width ?? OneBitSegmentWidth);
        }

        instructions.WriteByte(InitializeWritableBinary);
        MoveRegister(
            XRegisterTag,
            0,
            XRegisterTag,
            1
        );
        MoveRegister(
            YRegisterTag,
            0,
            XRegisterTag,
            0
        );
        Op(TailLocalCallWithFrame, BeamWire.UnsignedTag, WritableBinaryListHelperArity);
        Operand(LabelTag, entry);
        Operand(BeamWire.UnsignedTag, 1);
    }

    private void EmitWritableFixedWidthCapacity(int width)
    {
        int failedSize = nextLabel++;
        int initialize = nextLabel++;
        int bitRounding = BitSyntaxDefaults.BinaryUnit - 1;
        Op(GcBifOne, LabelTag, failedSize);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, AutoImportNames.Length, 1));
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        if (width != BitSyntaxDefaults.IntegerWidth)
        {
            EmitLine(default);
            bool nibble = width == NibbleSegmentWidth;
            if (nibble)
            {
                EmitWritableCapacityOperation(
                    ErlangOperators.Multiply,
                    width,
                    0,
                    ListLengthTypeMaximum
                );
            }

            EmitWritableCapacityOperation(
                ErlangOperators.Plus,
                bitRounding,
                0,
                nibble ? null : ListLengthTypeMaximum,
                failure: nibble ? failedSize : 0
            );
            EmitWritableCapacityOperation(
                ErlangOperators.ShiftRight,
                ByteCapacityShift,
                bitRounding,
                nibble ? null : ListLengthTypeMaximum + bitRounding
            );
        }

        Op(Jump, LabelTag, initialize);
        Op(Label, BeamWire.UnsignedTag, failedSize);
        EmitConstant(Term.I(BinaryComprehensionInitialCapacity), 0);
        Op(Label, BeamWire.UnsignedTag, initialize);
    }

    private void EmitWritableCapacityOperation(
        string operation,
        int argument,
        long lower,
        long? upper,
        int failure = 0
    )
    {
        Op(GcBifTwo, LabelTag, failure);
        Operand(BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, operation, 2));
        EmitTypedRegister(
            0,
            IntegerTypeBits,
            lower,
            upper
        );
        LiteralOperand(Term.I(argument));
        Operand(XRegisterTag, 0);
    }

    private void EmitWritableBinaryAccumulator(int tag)
    {
        EmitBinarySegmentPrefix(
            BinaryPrivateAppendSegment,
            1,
            BitSyntaxDefaults.BitStringUnit,
            BitByteOrders.Big
        );
        EmitTypedRegister(
            1,
            MatchableTypeBits,
            registerTag: tag,
            unit: BitSyntaxDefaults.BitStringUnit
        );
        LiteralOperand(Term.A(BitSizeAtoms.All));
    }

    private void EmitWritableBinaryListHelper(
        int entry,
        WritableBinaryListPlan plan,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        int skipped = plan.TuplePair ? nextLabel++ : 0;
        int empty = nextLabel++;
        int invalid = nextLabel++;
        Op(IsNonemptyList, LabelTag, empty);
        Operand(XRegisterTag, 0);
        if (plan.Mapper is { } mapper)
        {
            EmitWritableMappedBinaryIteration(entry, entries[(mapper.Name, 1)], mapper);
            EmitMappingEnd(empty, invalid, nilResultRegister: 1);

            return;
        }

        Op(GetList, XRegisterTag, 0);
        Operand(XRegisterTag, 2);
        Operand(XRegisterTag, 0);
        if (plan.TuplePair)
        {
            EmitTupleCheck(2, 2, skipped);
            EmitTuplePart(2, 0, 3);
            EmitTuplePart(2, 1, 2);
        }
        else if (plan.Subtract is { } subtract)
        {
            Op(GcBifTwo, LabelTag, 0);
            Operand(BeamWire.UnsignedTag, 3);
            Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, ErlangOperators.Minus, 2));
            Operand(XRegisterTag, 2);
            LiteralOperand(subtract);
            Operand(XRegisterTag, 2);
        }

        // Only the initial writable buffer or the immediately preceding private
        // append reaches X1. Both the remaining list and that buffer are roots.
        BeginBinaryConstruction(
            failure: 0,
            live: plan.TuplePair ? 4 : 3,
            unit: plan.TuplePair ? BitSyntaxDefaults.IntegerWidth * 2 : BitSyntaxDefaults.BitStringUnit,
            destination: 1,
            segmentCount: plan.TuplePair ? 3 : 2
        );
        EmitWritableBinaryAccumulator(XRegisterTag);
        if (plan.TuplePair)
        {
            EmitOutputSegment(2, 3, BitSyntaxDefaults.IntegerWidth);
            EmitOutputSegment(3, 2, BitSyntaxDefaults.IntegerWidth);
        }
        else
        {
            EmitOutputSegment(2, 2, OneBitSegmentWidth);
        }

        Tail();
        if (plan.TuplePair)
        {
            Op(Label, BeamWire.UnsignedTag, skipped);
            Tail();
        }

        EmitMappingEnd(empty, invalid, nilResultRegister: 1);

        void Tail()
        {
            Op(TailLocalCall, BeamWire.UnsignedTag, WritableBinaryListHelperArity);
            Operand(LabelTag, entry);
        }

        void EmitOutputSegment(int ordinal, int register, int width)
        {
            EmitBinarySegmentPrefix(
                BitSegmentTypes.Integer,
                ordinal,
                BitSyntaxDefaults.NumericUnit,
                BitByteOrders.Big
            );
            if (plan.Subtract is null)
            {
                Operand(XRegisterTag, register);
            }
            else
            {
                EmitTypedRegister(register, NumberTypeBits);
            }

            LiteralOperand(Term.I(width));
        }
    }
}
